namespace InvoiceApi.Models;

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

public class DocumentPropertyConverter : IPropertyConverter
{
    // 将 Document 对象转换为 DynamoDB 能够存储的格式 (DynamoDBEntry)
    public DynamoDBEntry ToEntry(object value)
    {
        var document = value as Document;

        // 如果 Data 是 null 或没有内容，存入一个空的 Map 实体
        return document ?? new Document();
    }

    // 从数据库读回数据时，将存储格式转换回 Document 对象
    public object FromEntry(DynamoDBEntry entry)
    {
        var primitive = entry as Document;
        return primitive ?? new Document();
    }
}