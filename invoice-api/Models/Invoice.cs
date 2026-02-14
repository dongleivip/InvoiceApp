using System.Text.Json.Serialization;

namespace InvoiceApi.Models;

public class Invoice : DataEntity
{
    // 必须保留无参构造函数供 DynamoDB SDK 反序列化使用
    public Invoice()
    {
    }

    public Invoice(string invoiceId)
    {
        PartitionKey = $"INV#{invoiceId}";
        SortKey = "";
        EntityType = "INVOICE";
    }

    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime IssueDate { get; set; }
}