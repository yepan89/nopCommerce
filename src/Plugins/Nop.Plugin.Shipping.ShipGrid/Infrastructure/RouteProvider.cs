using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Shipping.ShipGrid.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                ShipGridDefaults.ConfigurationRouteName,
                "Admin/ShipGrid/Configure",
                new { controller = "ShipGrid", action = "Configure", area = "Admin" });
        }

        // 数字越大越晚匹配，一般插件路由用比核心路由大的数字
        public int Priority => 0;
    }
}
