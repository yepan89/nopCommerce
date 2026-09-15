using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.ShipGrid.Domain
{
    /// <summary>
    /// ShipGrid 插件配置项，保存在 nopCommerce 的 Setting 表中
    /// </summary>
    public class ShipGridSettings : ISettings
    {
        /// <summary>
        /// API Key（ak_live_xxx 生产 / ak_test_xxx 测试）
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// 是否使用测试模式 Key（仅作后台提示用，实际由 Key 前缀决定）
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// 面单格式：PDF / PNG / ZPL
        /// </summary>
        public string DefaultLabelFormat { get; set; } = "PDF";

        // ------- 发货仓库地址 (from_address) -------
        public string ShipperName { get; set; }
        public string ShipperStreet1 { get; set; }
        public string ShipperCity { get; set; }
        public string ShipperState { get; set; }
        public string ShipperZip { get; set; }
        public string ShipperCountry { get; set; } = "US";

        // ------- 默认包裹尺寸（单位：英寸），当商品未维护尺寸时使用 -------
        public decimal DefaultParcelLength { get; set; } = 12;
        public decimal DefaultParcelWidth { get; set; } = 8;
        public decimal DefaultParcelHeight { get; set; } = 6;

        /// <summary>
        /// 是否在获取报价失败时，仍然放行结账（避免物流接口故障导致无法下单）
        /// </summary>
        public bool FailSilently { get; set; }
    }
}
