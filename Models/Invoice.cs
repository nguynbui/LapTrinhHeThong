using System;
using System.Text.Json.Serialization;

namespace Models
{
    public class Invoice
    {
        [JsonPropertyName("InvoiceId")]
        public string InvoiceId { get; set; }

        [JsonPropertyName("ProductId")]
        public string ProductId { get; set; }

        [JsonPropertyName("QuantitySold")]
        public int QuantitySold { get; set; }

        [JsonPropertyName("UnitSellingPrice")]
        public decimal UnitSellingPrice { get; set; }

        [JsonPropertyName("InvoiceDate")]
        public DateTime InvoiceDate { get; set; }
    }
}