namespace Final.Models;

public class Invoice
{
    public string InvoiceID { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Total { get; set; } // Sử dụng decimal cho tiền tệ
}