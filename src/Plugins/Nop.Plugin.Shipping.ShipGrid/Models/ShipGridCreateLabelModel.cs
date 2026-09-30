using Nop.Plugin.Shipping.ShipGrid.Services.Dto;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.ShipGrid.Models
{
    public record ShipGridCreateLabelModel : BaseNopModel
    {
        public int OrderId { get; set; }
        public List<RateDto> Rates { get; set; } = new();
        public string ErrorMessage { get; set; }
    }
}