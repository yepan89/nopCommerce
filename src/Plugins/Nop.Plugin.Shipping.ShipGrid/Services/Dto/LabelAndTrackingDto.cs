using System.Text.Json.Serialization;

namespace Nop.Plugin.Shipping.ShipGrid.Services.Dto
{
    public class LabelRequestDto
    {
        [JsonPropertyName("rate_id")]
        public string RateId { get; set; }

        [JsonPropertyName("from_address")]
        public AddressDto FromAddress { get; set; }

        [JsonPropertyName("to_address")]
        public AddressDto ToAddress { get; set; }

        [JsonPropertyName("parcel")]
        public ParcelDto Parcel { get; set; }

        [JsonPropertyName("label_format")]
        public string LabelFormat { get; set; } = "pdf";
    }

    public class LabelResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("tracking_number")]
        public string TrackingNumber { get; set; }

        [JsonPropertyName("label_url")]
        public string LabelUrl { get; set; }

        [JsonPropertyName("carrier")]
        public string Carrier { get; set; }

        [JsonPropertyName("service")]
        public string Service { get; set; }

        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }
    }

    public class TrackingEventDto
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; }
    }

    public class TrackingResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("events")]
        public List<TrackingEventDto> Events { get; set; } = new();
    }

    public class VoidLabelResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("refunded")]
        public bool Refunded { get; set; }
    }
}