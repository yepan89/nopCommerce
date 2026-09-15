using System.Text.Json.Serialization;

namespace Nop.Plugin.Shipping.ShipGrid.Services.Dto
{
    public class AddressDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("street1")]
        public string Street1 { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("zip")]
        public string Zip { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }

    public class ParcelDto
    {
        /// <summary>
        /// 重量，单位：盎司 (oz) —— 这是 ShipGrid API 要求的单位
        /// </summary>
        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }
        [JsonPropertyName("weight_unit")]
        public string WeightUnit { get; set; } = "oz";

        [JsonPropertyName("length")]
        public decimal Length { get; set; }

        [JsonPropertyName("width")]
        public decimal Width { get; set; }

        [JsonPropertyName("height")]
        public decimal Height { get; set; }

        [JsonPropertyName("dimension_unit")]
        public string DimensionUnit { get; set; } = "in";
    }

    public class RatesRequestDto
    {
        [JsonPropertyName("from_address")]
        public AddressDto FromAddress { get; set; }

        [JsonPropertyName("to_address")]
        public AddressDto ToAddress { get; set; }

        [JsonPropertyName("parcel")]
        public ParcelDto Parcel { get; set; }
    }

    public class RateDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("carrier")]
        public string Carrier { get; set; }

        [JsonPropertyName("service")]
        public string Service { get; set; }

        [JsonPropertyName("service_code")]
        public string ServiceCode { get; set; }

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("delivery_days")]
        public int? DeliveryDays { get; set; }

        [JsonPropertyName("delivery_date_guaranteed")]
        public bool DeliveryDateGuaranteed { get; set; }
    }

    public class RatesResponseDto
    {
         [JsonPropertyName("data")]
        public List<RateDto> Data { get; set; } = new();
    }

    public class ShipGridErrorDto
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
