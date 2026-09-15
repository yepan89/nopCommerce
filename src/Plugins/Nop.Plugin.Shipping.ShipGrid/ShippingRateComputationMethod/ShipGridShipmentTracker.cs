using Nop.Core.Domain.Shipping;
using Nop.Plugin.Shipping.ShipGrid.Services;
using Nop.Services.Shipping.Tracking;
using ILogger = Nop.Services.Logging.ILogger;

namespace Nop.Plugin.Shipping.ShipGrid.ShippingRateComputationMethod
{
    /// <summary>
    /// 让 nopCommerce 订单详情页可以直接显示 ShipGrid 的物流轨迹链接/状态
    /// </summary>
    public class ShipGridShipmentTracker : IShipmentTracker
    {
        private readonly ShipGridHttpClient _httpClient;
        private readonly ILogger _logger;

        public ShipGridShipmentTracker(ShipGridHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool IsMatch(string trackingNumber)
        {
            return !string.IsNullOrWhiteSpace(trackingNumber);
        }

        public Task<string> GetUrlAsync(string trackingNumber, Shipment shipment)
        {
            return Task.FromResult($"https://atoship.com/dashboard/tracking/{trackingNumber}");
        }

        public async Task<IList<ShipmentStatusEvent>> GetShipmentEventsAsync(string trackingNumber, Shipment shipment)
        {
            var events = new List<ShipmentStatusEvent>();

            try
            {
                var tracking = await _httpClient.GetTrackingAsync(trackingNumber);
                if (tracking?.Events == null)
                    return events;

                foreach (var e in tracking.Events)
                {
                    DateTime.TryParse(e.Timestamp, out var date);

                    events.Add(new ShipmentStatusEvent
                    {
                        Date = date,
                        EventName = e.Description,
                        Location = e.Location
                    });
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"ShipGrid: 获取轨迹失败 (单号: {trackingNumber})", ex);
            }

            return events;
        }
    }
}
