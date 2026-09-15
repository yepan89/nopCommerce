namespace Nop.Plugin.Shipping.ShipGrid
{
    /// <summary>
    /// 插件常量
    /// </summary>
    public static class ShipGridDefaults
    {
        /// <summary>
        /// 插件系统名（必须和 plugin.json 里的 SystemName 一致）
        /// </summary>
        public const string SystemName = "Shipping.ShipGrid";

        /// <summary>
        /// HttpClient 注册名
        /// </summary>
        public const string HttpClientName = "Nop.Plugin.Shipping.ShipGrid";

        /// <summary>
        /// 后台配置页路由名
        /// </summary>
        public const string ConfigurationRouteName = "Plugin.Shipping.ShipGrid.Configure";

        /// <summary>
        /// ShipGrid（品牌名 shipgrid.ai，真实 API host 是 atoship.com）生产环境地址
        /// </summary>
        public const string ApiBaseUrl = "https://shipgrid.ai/api/v1";

        /// <summary>
        /// 用于把 ShipGrid 返回的 rate_id 缓存起来，供后续创建面单使用的会话 Key 前缀
        /// </summary>
        public const string RateIdCacheKeyPrefix = "Nop.Plugin.Shipping.ShipGrid.RateId.";
    }
}
