using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Shipping.ShipGrid.Domain;
using Nop.Plugin.Shipping.ShipGrid.Services;
using Nop.Plugin.Shipping.ShipGrid.Services.Dto;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;
using Nop.Services.Cms;
using Nop.Web.Framework.Infrastructure;
using ILogger = Nop.Services.Logging.ILogger;

namespace Nop.Plugin.Shipping.ShipGrid.ShippingRateComputationMethod
{
    /// <summary>
    /// ShipGrid (atoship) 运费计算插件主类
    /// </summary>
    public class ShipGridProcessor : BasePlugin, IShippingRateComputationMethod, IWidgetPlugin
    {
        private readonly ShipGridHttpClient _httpClient;
        private readonly ShipGridSettings _settings;
        private readonly ShipGridShipmentTracker _shipmentTracker;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;

        public ShipGridProcessor(ShipGridHttpClient httpClient,
            ShipGridSettings settings,
            ShipGridShipmentTracker shipmentTracker,
            ISettingService settingService,
            ILocalizationService localizationService,
            ILogger logger,
            IWebHelper webHelper,
            ICountryService countryService,
            IStateProvinceService stateProvinceService)
        {
            _httpClient = httpClient;
            _settings = settings;
            _shipmentTracker = shipmentTracker;
            _settingService = settingService;
            _localizationService = localizationService;
            _logger = logger;
            _webHelper = webHelper;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
        }

        /// <summary>
        /// nopCommerce 结账流程会调用这个方法来展示可选的物流方式
        /// </summary>
        public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(
            GetShippingOptionRequest getShippingOptionRequest)
        {
            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest?.Items == null || !getShippingOptionRequest.Items.Any())
            {
                response.AddError("购物车为空，无法计算运费");
                return response;
            }

            if (getShippingOptionRequest.ShippingAddress?.CountryId == null)
            {
                response.AddError("收货地址不完整");
                return response;
            }

            if (string.IsNullOrWhiteSpace(_settings.ShipperZip))
            {
                response.AddError("插件尚未配置发货仓库地址，请联系管理员");
                return response;
            }

            try
            {
                var requestDto = await BuildRatesRequestAsync(getShippingOptionRequest);
                var rates = await _httpClient.GetRatesAsync(requestDto);
                

                if (rates == null || rates.Count == 0)
                {
                    response.AddError("暂时无法获取该地址的物流报价");
                    return response;
                }

                foreach (var rate in rates.OrderBy(r => r.Rate))
                {
                    response.ShippingOptions.Add(new ShippingOption
                    {
                        Name = FormatOptionName(rate),
                        Description = rate.DeliveryDays.HasValue
                            ? $"预计 {rate.DeliveryDays} 个工作日送达"
                            : string.Empty,
                        Rate = rate.Rate,
                        ShippingRateComputationMethodSystemName = ShipGridDefaults.SystemName
                    });
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("ShipGrid: 获取运费报价异常", ex);

                // FailSilently = true 时，即使物流接口挂了也不阻断结账（不推荐用于生产环境）
                if (!_settings.FailSilently)
                    response.AddError("物流服务暂时不可用，请稍后重试");
            }

            return response;
        }

        /// <summary>
        /// 组装调用 ShipGrid /rates 接口所需的请求体
        /// </summary>
        private async Task<RatesRequestDto> BuildRatesRequestAsync(GetShippingOptionRequest request)
        {
            // nopCommerce 商品重量的单位取决于系统"度量衡"设置(通常是 kg 或 lb)
            // ShipGrid 要求盎司(oz)，这里按 kg -> oz 换算，如果你的店铺用 lb 计重请自行调整系数(1 lb = 16 oz)
            var totalWeightKg = request.Items.Sum(x =>
                (x.Product?.Weight ?? 0) * x.GetQuantity());
            var weightOz = totalWeightKg > 0 ? totalWeightKg * 35.274m : 1m;
            var country = await _countryService.GetCountryByAddressAsync(request.ShippingAddress);
            var stateProvince = await _stateProvinceService.GetStateProvinceByAddressAsync(request.ShippingAddress);

            return new RatesRequestDto
            {
                FromAddress = new AddressDto
                {
                    Name = _settings.ShipperName,
                    Street1 = _settings.ShipperStreet1,
                    City = _settings.ShipperCity,
                    State = _settings.ShipperState,
                    Zip = _settings.ShipperZip,
                    Country = _settings.ShipperCountry
                },
                ToAddress = new AddressDto
                {
                    Name = $"{request.ShippingAddress.FirstName} {request.ShippingAddress.LastName}".Trim(),
                    Street1 = request.ShippingAddress.Address1,
                    City = request.ShippingAddress.City,
                    State = stateProvince?.Abbreviation,
                    Zip = request.ShippingAddress.ZipPostalCode,
                    Country = country?.TwoLetterIsoCode
                },
                Parcel = new ParcelDto
                {
                    Weight = weightOz,
                    WeightUnit = "oz",
                    Length = _settings.DefaultParcelLength,
                    Width = _settings.DefaultParcelWidth,
                    Height = _settings.DefaultParcelHeight,
                    DimensionUnit = "in"
                }
            };
        }

        private static string FormatOptionName(RateDto rate)
        {
            var carrier = (rate.Carrier ?? string.Empty).ToUpperInvariant();
            var service = (rate.Service ?? string.Empty).Replace("_", " ");
            return $"{carrier} - {service}".Trim(' ', '-');
        }

        public Task<decimal?> GetFixedRateAsync(GetShippingOptionRequest getShippingOptionRequest)
            => Task.FromResult<decimal?>(null);

        public Task<IShipmentTracker> GetShipmentTrackerAsync() => Task.FromResult<IShipmentTracker>(_shipmentTracker);

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/ShipGrid/Configure";
        }

        public override async Task InstallAsync()
        {
            await _settingService.SaveSettingAsync(new ShipGridSettings
            {
                UseSandbox = true,
                DefaultLabelFormat = "PDF",
                ShipperCountry = "US",
                DefaultParcelLength = 12,
                DefaultParcelWidth = 8,
                DefaultParcelHeight = 6,
                FailSilently = false
            });

            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.ShipGrid.Fields.ApiKey"] = "API Key",
                ["Plugins.Shipping.ShipGrid.Fields.ApiKey.Hint"] = "在 ShipGrid/atoship 后台 Dashboard -> Settings -> API Keys 中生成，测试请用 ak_test_ 开头的Key",
                ["Plugins.Shipping.ShipGrid.Fields.ShipperName"] = "发货人/店铺名称",
                ["Plugins.Shipping.ShipGrid.Fields.ShipperStreet1"] = "发货地址",
                ["Plugins.Shipping.ShipGrid.Fields.ShipperCity"] = "发货城市",
                ["Plugins.Shipping.ShipGrid.Fields.ShipperState"] = "发货州/省",
                ["Plugins.Shipping.ShipGrid.Fields.ShipperZip"] = "发货邮编",
                ["Plugins.Shipping.ShipGrid.Fields.ShipperCountry"] = "发货国家(两位代码，如 US)",
                ["Plugins.Shipping.ShipGrid.Fields.DefaultLabelFormat"] = "面单格式",
                ["Plugins.Shipping.ShipGrid.Fields.DefaultParcelLength"] = "默认包裹长(英寸)",
                ["Plugins.Shipping.ShipGrid.Fields.DefaultParcelWidth"] = "默认包裹宽(英寸)",
                ["Plugins.Shipping.ShipGrid.Fields.DefaultParcelHeight"] = "默认包裹高(英寸)",
                ["Plugins.Shipping.ShipGrid.Fields.FailSilently"] = "接口异常时仍允许结账",
                ["Plugins.Shipping.ShipGrid.Configuration.Saved"] = "配置已保存"
            });

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<ShipGridSettings>();
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.ShipGrid");
            await base.UninstallAsync();
        }
        public bool HideInWidgetList => true;

    public Task<IList<string>> GetWidgetZonesAsync()
        => Task.FromResult<IList<string>>(new List<string> { AdminWidgetZones.OrderDetailsButtons  });

    public Type GetWidgetViewComponent(string widgetZone)
        => typeof(Components.ShipGridOrderButtonViewComponent);
    }
}
