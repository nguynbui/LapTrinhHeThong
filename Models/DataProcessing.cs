using System;
using System.Text.Json.Serialization;

namespace Models
{
    public class XeroRoot
    {
        [JsonPropertyName("Invoices")]
        public List<XeroInvoice> Invoices { get; set; }
    }

    public class XeroInvoice
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } // ACCPAY (Mua) hoặc ACCREC (Bán)

        [JsonPropertyName("DateString")]
        public string DateString { get; set; }

        [JsonPropertyName("LineItems")]
        public List<XeroLineItem> LineItems { get; set; }
    }

    public class XeroLineItem
    {
        [JsonPropertyName("ItemCode")]
        public string ItemCode { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("Quantity")]
        public decimal Quantity { get; set; } 

        [JsonPropertyName("UnitAmount")]
        public decimal UnitAmount { get; set; }
    }
}