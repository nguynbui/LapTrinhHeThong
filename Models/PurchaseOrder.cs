using System;
using System.Text.Json.Serialization;

namespace Models
{
    public class PurchaseOrder
    {
        [JsonPropertyName("OrderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("ProductId")]
        public string ProductId { get; set; }

        [JsonPropertyName("QuantityPurchased")]
        public int QuantityPurchased { get; set; }

        [JsonPropertyName("UnitCost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("PurchaseDate")]
        public DateTime PurchaseDate { get; set; }
    }
}