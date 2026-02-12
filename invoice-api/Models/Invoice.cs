using System.Text.Json.Serialization;

namespace InvoiceApi.Models;

public class Invoice
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime IssueDate { get; set; }
}