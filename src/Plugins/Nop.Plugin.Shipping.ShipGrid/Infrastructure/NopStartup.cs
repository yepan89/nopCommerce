using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Shipping.ShipGrid.Services;
using Nop.Plugin.Shipping.ShipGrid.ShippingRateComputationMethod;

namespace Nop.Plugin.Shipping.ShipGrid.Infrastructure
{
    /// <summary>
    /// 插件的依赖注入注册。nopCommerce 启动时会自动扫描并调用实现了 INopStartup 的类。
    /// </summary>
    public class NopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // 注册具名 HttpClient，供 ShipGridHttpClient 使用
            services.AddHttpClient<ShipGridHttpClient>(ShipGridDefaults.HttpClientName);

            services.AddScoped<ShipGridShipmentTracker>();
            services.AddScoped<ShipGridProcessor>();
        }

        public void Configure(IApplicationBuilder application)
        {
            // 无需额外中间件配置
        }

        // 数值越小越先执行，一般插件用较大的数字（如 3000+）避免抢在核心服务前注册
        public int Order => 3000;
    }
}
