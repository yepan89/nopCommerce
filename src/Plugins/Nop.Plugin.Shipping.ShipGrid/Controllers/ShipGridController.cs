using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Shipping.ShipGrid.Domain;
using Nop.Plugin.Shipping.ShipGrid.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Security;
using Nop.Services.Messages;
using Nop.Services.Shipping;
using Nop.Core.Domain.Cms;
using Nop.Plugin.Shipping.ShipGrid.Services;
using Nop.Plugin.Shipping.ShipGrid.Services.Dto;
using Nop.Core.Domain.Shipping;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.ShipGrid.Controllers
{
    [Area(AreaNames.ADMIN)]
    [AuthorizeAdmin]
    [AutoValidateAntiforgeryToken]
    public class ShipGridController : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;
        private readonly INotificationService _notificationService;
        private readonly IOrderService _orderService;
        private readonly IAddressService _addressService;
        private readonly IShipmentService _shipmentService;
        private readonly ShipGridHttpClient _httpClient;
        private readonly ShipGridSettings _shipGridSettings;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly WidgetSettings _widgetSettings;


        public ShipGridController(ISettingService settingService,
            IPermissionService permissionService,
            INotificationService notificationService,
            IOrderService orderService,
            IAddressService addressService,
            IShipmentService shipmentService,
            ShipGridHttpClient httpClient,
             ShipGridSettings shipGridSettings,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            WidgetSettings widgetSettings)
        {
            _settingService = settingService;
            _permissionService = permissionService;
            _notificationService = notificationService;
            _orderService = orderService;
            _addressService = addressService;
            _shipmentService = shipmentService;
            _httpClient = httpClient;
            _shipGridSettings = shipGridSettings;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _widgetSettings = widgetSettings;
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var settings = await _settingService.LoadSettingAsync<ShipGridSettings>();

            var model = new ConfigurationModel
            {
                ApiKey = settings.ApiKey,
                ShipperName = settings.ShipperName,
                ShipperStreet1 = settings.ShipperStreet1,
                ShipperCity = settings.ShipperCity,
                ShipperState = settings.ShipperState,
                ShipperZip = settings.ShipperZip,
                ShipperCountry = settings.ShipperCountry,
                DefaultLabelFormat = settings.DefaultLabelFormat,
                DefaultParcelLength = settings.DefaultParcelLength,
                DefaultParcelWidth = settings.DefaultParcelWidth,
                DefaultParcelHeight = settings.DefaultParcelHeight,
                FailSilently = settings.FailSilently
            };

            model.LabelFormats = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new("PDF", "PDF"),
                new("PNG", "PNG"),
                new("ZPL (热敏打印机)", "ZPL")
            };

            return View("~/Plugins/Shipping.ShipGrid/Views/ShipGrid/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            var settings = await _settingService.LoadSettingAsync<ShipGridSettings>();

            settings.ApiKey = model.ApiKey?.Trim();
            settings.ShipperName = model.ShipperName;
            settings.ShipperStreet1 = model.ShipperStreet1;
            settings.ShipperCity = model.ShipperCity;
            settings.ShipperState = model.ShipperState;
            settings.ShipperZip = model.ShipperZip;
            settings.ShipperCountry = model.ShipperCountry;
            settings.DefaultLabelFormat = model.DefaultLabelFormat;
            settings.DefaultParcelLength = model.DefaultParcelLength;
            settings.DefaultParcelWidth = model.DefaultParcelWidth;
            settings.DefaultParcelHeight = model.DefaultParcelHeight;
            settings.FailSilently = model.FailSilently;
            settings.UseSandbox = settings.ApiKey?.StartsWith("ak_test_") == true;

            await _settingService.SaveSettingAsync(settings);
             if (!_widgetSettings.ActiveWidgetSystemNames.Contains(ShipGridDefaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Add(ShipGridDefaults.SystemName);
                await _settingService.SaveSettingAsync(_widgetSettings);
            }

            _notificationService.SuccessNotification(await Task.FromResult("配置已保存"));

            return await Configure();
        }
        [HttpGet]
        public async Task<IActionResult> CreateLabel(int orderId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
                return RedirectToAction("List", "Order");

            var model = new ShipGridCreateLabelModel { OrderId = orderId };

            if (order.ShippingAddressId == null)
            {
                model.ErrorMessage = "该订单没有收货地址(可能是自提订单)，无法生成面单";
                return View("~/Plugins/Shipping.ShipGrid/Views/ShipGrid/CreateLabel.cshtml", model);
            }

            var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
            var orderItems = await _orderService.GetOrderItemsAsync(orderId);

            var totalWeightKg = orderItems.Sum(i => (i.ItemWeight ?? 0) * i.Quantity);
            var weightOz = totalWeightKg > 0 ? totalWeightKg * 35.274m : 1m;

            var country = await _countryService.GetCountryByAddressAsync(shippingAddress);
            var stateProvince = await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress);

            try
            {
                var request = new RatesRequestDto
                {
                    FromAddress = new AddressDto
                    {
                        Name = _shipGridSettings.ShipperName,
                        Street1 = _shipGridSettings.ShipperStreet1,
                        City = _shipGridSettings.ShipperCity,
                        State = _shipGridSettings.ShipperState,
                        Zip = _shipGridSettings.ShipperZip,
                        Country = _shipGridSettings.ShipperCountry
                    },
                    ToAddress = new AddressDto
                    {
                        Name = $"{shippingAddress.FirstName} {shippingAddress.LastName}".Trim(),
                        Street1 = shippingAddress.Address1,
                        City = shippingAddress.City,
                        State = stateProvince?.Abbreviation,
                        Zip = shippingAddress.ZipPostalCode,
                        Country = country?.TwoLetterIsoCode
                    },
                    Parcel = new ParcelDto
                    {
                        Weight = weightOz,
                        WeightUnit = "oz",
                        Length = _shipGridSettings.DefaultParcelLength,
                        Width = _shipGridSettings.DefaultParcelWidth,
                        Height = _shipGridSettings.DefaultParcelHeight,
                        DimensionUnit = "in"
                    }
                };

                var rates = await _httpClient.GetRatesAsync(request);
                model.Rates = rates.OrderBy(r => r.Rate).ToList();
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"获取报价失败: {ex.Message}";
            }

            return View("~/Plugins/Shipping.ShipGrid/Views/ShipGrid/CreateLabel.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLabel(int orderId, string rateId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            if (string.IsNullOrWhiteSpace(rateId))
            {
                _notificationService.ErrorNotification("请先选择一个报价选项");
                return RedirectToAction("CreateLabel", new { orderId });
            }

            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
                var orderItems = await _orderService.GetOrderItemsAsync(orderId);
                var country = await _countryService.GetCountryByAddressAsync(shippingAddress);
                var stateProvince = await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress);

                var totalWeightKg = orderItems.Sum(i => (i.ItemWeight ?? 0) * i.Quantity);
                var weightOz = totalWeightKg > 0 ? totalWeightKg * 35.274m : 1m;

                var labelRequest = new LabelRequestDto
                {
                    RateId = rateId,
                    LabelFormat = (_shipGridSettings.DefaultLabelFormat ?? "PDF").ToLowerInvariant(),
                    FromAddress = new AddressDto
                    {
                        Name = _shipGridSettings.ShipperName,
                        Street1 = _shipGridSettings.ShipperStreet1,
                        City = _shipGridSettings.ShipperCity,
                        State = _shipGridSettings.ShipperState,
                        Zip = _shipGridSettings.ShipperZip,
                        Country = _shipGridSettings.ShipperCountry
                    },
                    ToAddress = new AddressDto
                    {
                        Name = $"{shippingAddress.FirstName} {shippingAddress.LastName}".Trim(),
                        Street1 = shippingAddress.Address1,
                        City = shippingAddress.City,
                        State = stateProvince?.Abbreviation,
                        Zip = shippingAddress.ZipPostalCode,
                        Country = country?.TwoLetterIsoCode
                    },
                    Parcel = new ParcelDto
                    {
                        Weight = weightOz,
                        WeightUnit = "oz",
                        Length = _shipGridSettings.DefaultParcelLength,
                        Width = _shipGridSettings.DefaultParcelWidth,
                        Height = _shipGridSettings.DefaultParcelHeight,
                        DimensionUnit = "in"
                    }
                };

                var draft = await _httpClient.CreateDraftLabelAsync(labelRequest);
                var label = await _httpClient.PurchaseLabelAsync(draft.Id);

                var shipments = await _shipmentService.GetShipmentsByOrderIdAsync(orderId);
                var shipment = shipments.FirstOrDefault();

                if (shipment == null)
                {
                    shipment = new Shipment
                    {
                        OrderId = orderId,
                        TrackingNumber = label.TrackingNumber,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    await _shipmentService.InsertShipmentAsync(shipment);

                    foreach (var item in orderItems)
                    {
                        await _shipmentService.InsertShipmentItemAsync(new ShipmentItem
                        {
                            ShipmentId = shipment.Id,
                            OrderItemId = item.Id,
                            Quantity = item.Quantity,
                            WarehouseId = 0
                        });
                    }
                }
                else
                {
                    shipment.TrackingNumber = label.TrackingNumber;
                    await _shipmentService.UpdateShipmentAsync(shipment);
                }

                _notificationService.SuccessNotification(
                    $"面单生成成功！运单号: {label.TrackingNumber}，承运商: {label.Carrier}，面单下载: {label.LabelUrl}");
            }
            catch (Exception ex)
            {
                _notificationService.ErrorNotification($"生成面单失败: {ex.Message}");
            }

            return RedirectToAction("Edit", "Order", new { id = orderId, area = "Admin" });
        }
    }
}
