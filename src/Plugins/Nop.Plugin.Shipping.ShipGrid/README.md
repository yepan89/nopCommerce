# Nop.Plugin.Shipping.ShipGrid

nopCommerce 4.70 物流插件，对接 ShipGrid (品牌域名 shipgrid.ai，真实 API 域名 atoship.com)。

支持：
- 结账时实时获取 USPS / UPS / FedEx 多家运费报价（POST /v1/rates）
- 后台配置 API Key、发货仓库地址、默认包裹尺寸
- 订单详情页展示物流轨迹（GET /v1/tracking/{trackingNumber}）
- 预留了创建面单（POST /v1/labels）和作废面单（POST /v1/labels/{id}/void）的方法，
  可以在发货流程里自行调用 `ShipGridHttpClient.CreateLabelAsync` / `VoidLabelAsync`

## ⚠️ 重要说明（请务必先读）

这是**插件的完整源代码**，不是编译好的 DLL。原因很简单：编译 nopCommerce 插件必须引用
nopCommerce 自己的核心项目（Nop.Core / Nop.Services / Nop.Web.Framework 等），这些项目只存在于
你自己的 nopCommerce 4.70 源码解决方案里，我这边没有你的源码环境，无法真正跑一次编译。

所以拿到代码后，你需要在自己的开发环境里编译一次——大概率会有一些**接口签名的小出入**需要修正
（不同小版本之间 `IShippingRateComputationMethod` / `IShipmentTracker` 的个别方法可能是
同步还是异步、参数顺序等会有细微差异，Visual Studio 会直接报红，照着提示改函数签名即可，
逻辑代码不用动）。

## 安装步骤

1. 下载并解压这个压缩包
2. 把整个 `Nop.Plugin.Shipping.ShipGrid` 文件夹复制到你的 nopCommerce 源码目录：
   ```
   你的NopCommerce源码/src/Plugins/Nop.Plugin.Shipping.ShipGrid/
   ```
3. 用 Visual Studio 打开 `nopCommerce.sln`
4. 右键解决方案 → 添加 → 现有项目 → 选择 `Nop.Plugin.Shipping.ShipGrid.csproj`
5. 右键 `Nop.Web` 项目 → 添加引用 → 勾选 `Nop.Plugin.Shipping.ShipGrid`
6. 生成解决方案（如果有编译报错，多数是接口签名差异，按提示修正即可）
7. F5 启动网站 → 后台 → 配置 → 插件 → 找到 "ShipGrid Shipping (atoship)" → 安装
8. 安装后点"配置"，填写：
   - API Key（先用 `ak_test_` 开头的测试 Key）
   - 发货仓库地址（这是计算运费时的起始地址，必填）
9. 后台 → 配置 → 物流 → 物流费率计算方式，勾选启用 "ShipGrid Shipping (atoship)"
10. 前台加商品到购物车走到运费选择步骤，确认能看到 USPS/UPS/FedEx 的报价选项
11. 确认无误后，把 API Key 换成 `ak_live_` 开头的正式 Key

## 关于面单生成 / 追踪号回写

`ShipGridHttpClient.CreateLabelAsync(rateId, format)` 已经写好，但**没有**自动挂接到
"订单发货"流程上，因为这一步和你店铺的发货习惯强相关（有的人在收到订单后台自动出单，
有的人要人工审核才出单）。建议做法：

- 在后台订单详情页加一个"生成 ShipGrid 面单"按钮的自定义 Controller Action
- 调用成功后，把返回的 `tracking_number` 写入该订单 `Shipment` 实体的 `TrackingNumber` 字段，
  `label_url` 可以存到 Shipment 的一个 GenericAttribute 里，方便后台随时打开 PDF 面单

如果需要，我可以继续把这部分"后台发货页生成面单"的按钮和 Controller 代码也写出来。

## 需要你确认/调整的地方

- `ShipGridSettings` 里的重量换算目前假设你的商品重量单位是 **kg**（`* 35.274` 换算成盎司）。
  如果你后台"度量衡"设置的是磅(lb)，把 `ShipGridProcessor.BuildRatesRequest` 里的换算系数改成 `* 16`。
- 默认包裹长宽高是写死在设置里的（12x8x6 英寸），如果你的商品需要按实际尺寸报价，
  改成从商品的 Length/Width/Height 字段读取即可。
- `rate_id` 有时效性，请在用户下单后尽快调用 `CreateLabelAsync`，不要缓存太久。
