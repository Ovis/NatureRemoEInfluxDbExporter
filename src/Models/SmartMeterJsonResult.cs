using System.Text.Json.Serialization;

namespace NatureRemoEInfluxDbExporter.Models
{
    public class SmartMeterJsonResult
    {
        [JsonPropertyName("echonetlite_properties")]
        public List<EchonetliteProperties> Properties { get; set; } = [];
    }

    public class EchonetliteProperties
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("epc")]
        public int Epc { get; set; }

        [JsonPropertyName("val")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
