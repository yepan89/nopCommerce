using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Shipping.ShipGrid.Components
{
    public class ShipGridOrderButtonViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke(string widgetZone, object additionalData)
        {
            var orderId = 0;
            if (additionalData != null)
            {
                var idProperty = additionalData.GetType().GetProperty("Id");
                if (idProperty?.GetValue(additionalData) is int id)
                    orderId = id;
            }

            return View("~/Plugins/Shipping.ShipGrid/Views/ShipGrid/_OrderButton.cshtml", orderId);
        }
    }
}