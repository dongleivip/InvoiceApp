using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using InvoiceApi.Models;

namespace InvoiceApi.Repositories;

public class DynamoRepository<T> : IDynamoRepository<T> where T : DataEntity, new()
{
    private readonly IDynamoDBContext _context;
    private readonly string _tableName;

    public DynamoRepository(IDynamoDBContext context, IConfiguration configuration)
    {
        _context = context;
        var tableName = configuration.GetSection("DynamoDB");
        _tableName = configuration["DynamoDB:TableName"] ?? throw new Exception("Table Name not configured");
    }


    public async Task<T?> GetAsync(string pk, string sk)
    {
        return await _context.LoadAsync<T>(pk, sk);
    }

    public async Task CreateAsync(T entity)
    {
        var now = DateTime.Now;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        
        Console.WriteLine(entity.Data.ToJson());
        
        await _context.SaveAsync(entity);
    }

    public async Task DeleteAsync(string pk, string sk)
    {
        await _context.DeleteAsync<T>(pk, sk);
    }

    public async Task<IEnumerable<T>> QueryAsync(string pk, QueryOperationConfig? config = null)
    {
        // 在 DynamoDB 中，Query 必须包含对 Hash Key (PK) 的 "Equal" 条件
        config ??= new QueryOperationConfig
        {
            // 确保 Filter 中包含对分区键的等值过滤
            // 在 DynamoDB 中，Query 必须包含对 Hash Key (PK) 的 "Equal" 条件
            // Partition Key 属性在实体中定义的特性或物理名是 "PK"
            // 注意：这里的 "PK" 需要与 DataEntity 中 [DynamoDBHashKey("PK")] 定义的名字一致
            Filter = new QueryFilter("PK", QueryOperator.Equal, pk)
        };

        var search = _context.FromQueryAsync<T>(config);
        return await search.GetRemainingAsync();
    }
}