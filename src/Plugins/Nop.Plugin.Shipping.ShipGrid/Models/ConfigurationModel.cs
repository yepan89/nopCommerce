using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.ShipGrid.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.ApiKey")]
        public string ApiKey { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.ShipperName")]
        public string ShipperName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.ShipperStreet1")]
        public string ShipperStreet1 { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.ShipperCity")]
        public string ShipperCity { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.ShipperState")]
        public string ShipperState { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.ShipperZip")]
        public string ShipperZip { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.ShipperCountry")]
        public string ShipperCountry { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.DefaultLabelFormat")]
        public string DefaultLabelFormat { get; set; }
        public List<SelectListItem> LabelFormats { get; set; } = new();

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.DefaultParcelLength")]
        public decimal DefaultParcelLength { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.DefaultParcelWidth")]
        public decimal DefaultParcelWidth { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.DefaultParcelHeight")]
        public decimal DefaultParcelHeight { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ShipGrid.Fields.FailSilently")]
        public bool FailSilently { get; set; }
    }
}
