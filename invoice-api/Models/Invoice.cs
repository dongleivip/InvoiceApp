namespace InvoiceApi.Models;

using Amazon.DynamoDBv2.DataModel;

public class Invoice : DataEntity
{
    // 必须保留无参构造函数供 DynamoDB SDK 反序列化使用
    public Invoice()
    {
    }

    public Invoice(string customerId, string invoiceId, DateTime date)
    {
        var dateString = date.ToString("yyyy-MM-dd");

        // Basic Key
        PartitionKey = $"CUST#{customerId}";
        SortKey = $"INV#{dateString}#{invoiceId}";
        EntityType = "Invoice";

        // GSI1 索引用来按日期范围来查询 Invoice
        Gsi1Pk = "INV#ALL";
        Gsi1Sk = $"{dateString}#{invoiceId}";

        // GSI2 索引用来实现仅用 InvoiceId 来查询 Invoice
        Gsi2Pk = $"INV#{invoiceId}";
        Gsi2Sk = "METADATA";

        var now = DateTime.Now;
        CreatedAt = now;
        UpdatedAt = now;
    }

    [DynamoDBGlobalSecondaryIndexHashKey("GSI2", AttributeName = "GSI2PK")]
    public string Gsi2Pk { get; set; }

    [DynamoDBGlobalSecondaryIndexRangeKey("GSI2", AttributeName = "GSI2SK")]
    public string Gsi2Sk { get; set; }

    [DynamoDBProperty("Amount")]
    public decimal Amount { get; set; }

    [DynamoDBProperty("IssueDate")]
    public string IssueDate { get; set; }

    [DynamoDBIgnore]
    public string? CustomerName
    {
        get => ExtractFromData("customerName");
        set => Data["customerName"] = value;
    }

    [DynamoDBIgnore]
    public string? DeliveryAddress
    {
        get => ExtractFromData("deliveryAddress");
        set => Data["deliveryAddress"] = value;
    }

    [DynamoDBIgnore]
    public string Id => SortKey?.Split('#').LastOrDefault() ?? string.Empty;

    [DynamoDBIgnore]
    public string CustomerId => PartitionKey?.Split('#').LastOrDefault() ?? string.Empty;
}