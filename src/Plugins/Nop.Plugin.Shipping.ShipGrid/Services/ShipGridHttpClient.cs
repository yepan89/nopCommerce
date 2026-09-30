using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Nop.Core;
using Nop.Plugin.Shipping.ShipGrid.Domain;
using Nop.Plugin.Shipping.ShipGrid.Services.Dto;

namespace Nop.Plugin.Shipping.ShipGrid.Services
{
    /// <summary>
    /// 封装对 ShipGrid (atoship) REST API 的调用。
    /// 官方文档: https://shipgrid.ai/docs/api-reference
    /// 真实接口域名: https://atoship.com/api/v1
    /// </summary>
    public class ShipGridHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ShipGridSettings _settings;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ShipGridHttpClient(HttpClient httpClient, ShipGridSettings settings)
        {
            _settings = settings;
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri(ShipGridDefaults.ApiBaseUrl + "/");
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            }
        }

        /// <summary>
        /// 调用 POST /v1/rates 获取多家承运商的实时报价
        /// </summary>
        public async Task<List<RateDto>> GetRatesAsync(RatesRequestDto request)
        {
            EnsureApiKeyConfigured();

            using var response = await _httpClient.PostAsJsonAsync("rates", request, _jsonOptions);
            await EnsureSuccessAsync(response, "获取运费报价失败");

            var result = await response.Content.ReadFromJsonAsync<RatesResponseDto>(_jsonOptions);
            return result?.Data ?? new List<RateDto>();
        }

        /// <summary>
        /// 第一步：创建面单草稿(不扣费，不生成真实面单，status 为 draft)
        /// </summary>
        public async Task<LabelResponseDto> CreateDraftLabelAsync(LabelRequestDto request)
        {
            EnsureApiKeyConfigured();

            using var response = await _httpClient.PostAsJsonAsync("labels", request, _jsonOptions);
            await EnsureSuccessAsync(response, "创建面单草稿失败");

            return await response.Content.ReadFromJsonAsync<LabelResponseDto>(_jsonOptions);
        }

        /// <summary>
        /// 第二步：真正购买面单(扣费，返回运单号和面单文件链接)
        /// 注意: rate_id 有时效性(通常几十分钟)，请在用户下单后尽快调用整个流程
        /// </summary>
        public async Task<LabelResponseDto> PurchaseLabelAsync(string labelId)
        {
            EnsureApiKeyConfigured();

            using var response = await _httpClient.PostAsync($"labels/{Uri.EscapeDataString(labelId)}/purchase", null);
            await EnsureSuccessAsync(response, "购买面单失败");

            return await response.Content.ReadFromJsonAsync<LabelResponseDto>(_jsonOptions);
        }

        /// <summary>
        /// 调用 GET /v1/tracking/{trackingNumber} 查询轨迹
        /// </summary>
        public async Task<TrackingResponseDto> GetTrackingAsync(string trackingNumber)
        {
            EnsureApiKeyConfigured();

            using var response = await _httpClient.GetAsync($"tracking/{Uri.EscapeDataString(trackingNumber)}");
            await EnsureSuccessAsync(response, "查询物流轨迹失败");

            return await response.Content.ReadFromJsonAsync<TrackingResponseDto>(_jsonOptions);
        }

        /// <summary>
        /// 调用 POST /v1/labels/{id}/void 作废面单并申请退款
        /// </summary>
        public async Task<VoidLabelResponseDto> VoidLabelAsync(string labelId)
        {
            EnsureApiKeyConfigured();

            using var response = await _httpClient.PostAsync($"labels/{Uri.EscapeDataString(labelId)}/void", null);
            await EnsureSuccessAsync(response, "作废面单失败");

            return await response.Content.ReadFromJsonAsync<VoidLabelResponseDto>(_jsonOptions);
        }

        private void EnsureApiKeyConfigured()
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new NopException("ShipGrid API Key 尚未配置，请到 后台 -> 配置 -> 物流 -> ShipGrid 中填写");
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string errorPrefix)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync();
            string detail = body;
            try
            {
                var err = JsonSerializer.Deserialize<ShipGridErrorDto>(body, _jsonOptions);
                if (err != null)
                    detail = err.Message ?? err.Error ?? body;
            }
            catch
            {
                // 响应体不是标准错误 JSON，直接用原始内容
            }

            throw new NopException($"{errorPrefix} (HTTP {(int)response.StatusCode}): {detail}");
        }
    }
}