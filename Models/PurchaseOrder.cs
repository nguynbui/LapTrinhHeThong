namespace Final.Models;

public class PurchaseOrder
{
    public string PurchaseOrderID { get; set; } = string.Empty;
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
}