using Amazon.DynamoDBv2.DataModel;

namespace InvoiceApi.Models;

public class Customer : DataEntity
{
    public Customer()
    {
    }

    public Customer(string customerId)
    {
        // 基础建
        PartitionKey = $"CUST#{customerId}";
        SortKey = "METADATA"; // 客户元数据固定 SK
        EntityType = "Customer";

        // GSI1: 用于全部检索 (List All)
        // 将所有客户放在同一个虚拟分区 "CUST#ALL" 下
        Gsi1Pk = $"CUST#ALL";
        Gsi1Sk = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
    }

    // --- 辅助属性 ---
    [DynamoDBIgnore] public string Id => PartitionKey?.Split('#').LastOrDefault() ?? string.Empty;

    // 将常用搜索字段提升为顶层属性，方便在 DynamoDB 控制台直接查看
    [DynamoDBProperty("Name")] public string Name { get; set; }

    [DynamoDBProperty("Contact")] public string? Contact { get; set; }

    // --- GSI2 定义 (需要在 AWS 创建表时配置) ---
    [DynamoDBGlobalSecondaryIndexHashKey("GSI2PK")]
    public string Gsi2Pk { get; set; }

    [DynamoDBGlobalSecondaryIndexRangeKey("GSI2SK")]
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