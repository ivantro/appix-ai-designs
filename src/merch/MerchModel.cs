namespace com.appix.ai.design {

    public class MerchPOCO {
        public required string id { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string linkURL { get; set; }
        public string imageURL { get; set; }
        public int productType { get; set; }
        public int productCategory { get; set; }
        public bool isTopCategory { get; set; }
        public bool isNewCategory { get; set; }
    }
}