namespace InvoiceApi.Models;

using Amazon.DynamoDBv2.DataModel;

public class Customer : DataEntity
{
    public Customer()
    {
    }

    public Customer(string customerId)
    {
        var now = DateTime.Now;

        // 基础建
        PartitionKey = $"CUST#{customerId}";
        SortKey = "METADATA"; // 客户元数据固定 SK
        EntityType = "Customer";

        // GSI1: 用于全部检索 (List All)
        // 将所有客户放在同一个虚拟分区 "CUST#ALL" 下
        Gsi1Pk = "CUST#ALL";
        Gsi1Sk = now.ToString("yyyy-MM-ddTHH:mm:ss");

        CreatedAt = now;
        UpdatedAt = now;
    }

    // --- 辅助属性 ---
    [DynamoDBIgnore]
    public string Id => PartitionKey?.Split('#').LastOrDefault() ?? string.Empty;

    // 将常用搜索字段提升为顶层属性，方便在 DynamoDB 控制台直接查看
    [DynamoDBProperty("Name")]
    public string Name { get; set; }

    [DynamoDBProperty("Contact")]
    public string? Contact { get; set; }

    // --- GSI2  (目前未给Customer启用)
    [DynamoDBGlobalSecondaryIndexHashKey("GSI2", AttributeName = "GSI2PK")]
    public string Gsi2Pk { get; set; }

    [DynamoDBGlobalSecondaryIndexRangeKey("GSI2", AttributeName = "GSI2SK")]
    public string Gsi2Sk { get; set; }

    // 通过 Data 字段存储用户具体属性
    [DynamoDBIgnore]
    public string? Address
    {
        get => ExtractFromData("address");
        set => Data["address"] = value;
    }

    [DynamoDBIgnore]
    public string? TaxId
    {
        get => ExtractFromData("taxId");
        set => Data["taxId"] = value;
    }
}