using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Shipping.ShipGrid.Domain;
using Nop.Plugin.Shipping.ShipGrid.Models;
using Nop.Services.Configuration;
using Nop.Services.Security;
using Nop.Services.Messages;
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


        public ShipGridController(ISettingService settingService,
            IPermissionService permissionService,
            INotificationService notificationService)
        {
            _settingService = settingService;
            _permissionService = permissionService;
            _notificationService = notificationService;
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

            _notificationService.SuccessNotification(await Task.FromResult("配置已保存"));

            return await Configure();
        }
    }
}
