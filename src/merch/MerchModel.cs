using System.Text.Json.Serialization;

namespace com.appix.ai.design {

    public class MerchPOCO {
        public string? id { get; set; }
        public string? type { get; set; }
        public string? title { get; set; }
        public string? linkURL { get; set; }
        public string? imageURL { get; set; }
        public int productId { get; set; }
        public int[] categoryIds { get; set; }
        public bool isTopCategory { get; set; }
        public bool isNewCategory { get; set; }
        public bool isMine{ get; set; }
    }
}