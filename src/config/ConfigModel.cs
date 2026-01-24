using System.Text.Json.Serialization;

namespace com.appix.ai.design {
    public class ConfigPOCO {
        public string? id { get; set; }
        public string? type { get; set; }
        
        // Store any additional JSON properties
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}

