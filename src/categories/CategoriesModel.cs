namespace com.appix.ai.design {

    public class CategoriesPOCO {
        public required string id { get; set; }
        public string? type { get; set; }
        public string? name { get; set; }
        public int sort { get; set; }
        public bool isActive { get; set; }
        public string? url { get; set; }
    }
}