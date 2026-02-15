namespace InvoiceApi.Models;

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

[DynamoDBTable("InvoiceApp")]
public class DataEntity
{
    // 分区键（必须）
    [DynamoDBHashKey("PK")]
    public string PartitionKey { get; set; }

    // 排序键（可选）
    [DynamoDBRangeKey("SK")]
    public string SortKey { get; protected set; }

    // GSI 索引键
    // 必须明确标记这是 GSI1 索引的分区键和排序键
    [DynamoDBGlobalSecondaryIndexHashKey("GSI1", AttributeName = "GSI1PK")]
    public string Gsi1Pk { get; set; }

    [DynamoDBGlobalSecondaryIndexRangeKey("GSI1", AttributeName = "GSI1SK")]
    public string Gsi1Sk { get; set; }

    // 实体类型标识
    [DynamoDBProperty("EntityType")]
    public string EntityType { get; protected set; }

    // 通用属性
    [DynamoDBProperty("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [DynamoDBProperty("UpdatedAt")]
    public DateTime UpdatedAt { get; set; }

    // 实体特定数据（JSON 序列化）
    [DynamoDBProperty("Data", typeof(DocumentPropertyConverter))]
    public Document Data { get; set; } = new ();

    protected string ExtractFromData(string key)
    {
        return this.Data != null && this.Data.ContainsKey(key) ? this.Data[key].AsString() : string.Empty;
    }
}