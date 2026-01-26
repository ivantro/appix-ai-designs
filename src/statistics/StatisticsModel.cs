using System.Text.Json.Serialization;

namespace com.appix.ai.design {
    public class ClickPOCO {
        public string? id { get; set; }
        public string? type { get; set; }
        public string? senderId { get; set; }
        public string productId { get; set; }
        public DateTime timestamp { get; set; }
        public string? tag { get; set; }
    }

    public class ClickStatsPOCO {
        public string? id { get; set; }  // Will be the productId as string
        public string? type { get; set; }
        public string productId { get; set; }
        public int count { get; set; }
    }
}

