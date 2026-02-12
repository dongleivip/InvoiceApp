using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using InvoiceApi.Models;
using Microsoft.Extensions.Configuration;

namespace InvoiceApi.Repositories;

public class DynamoRepository<T> : IDynamoRepository<T> where T : class
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly DynamoDBContext _context;
    private readonly string _tableName;
    private readonly string _partitionKeyField;
    private readonly string _sortKeyField;
    private readonly string _entityTypeField;
    private readonly string _createdAtField;
    private readonly string _gsiPartitionKeyField;
    private readonly string _gsiSortKeyField;

    public DynamoRepository(IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
    {
        _dynamoDbClient = dynamoDbClient;
        var contextConfig = new DynamoDBContextConfig
        {
            Conversion = DynamoDBEntryConversion.V2
        };
        _context = new DynamoDBContext(_dynamoDbClient, contextConfig);
        _tableName = configuration["DynamoDB:TableName"] ?? "InvoiceApp";
        _partitionKeyField = configuration["DynamoDB:PartitionKeyField"] ?? "pk";
        _sortKeyField = configuration["DynamoDB:SortKeyField"] ?? "sk";
        _entityTypeField = configuration["DynamoDB:EntityTypeField"] ?? "entity_type";
        _createdAtField = configuration["DynamoDB:CreatedAtField"] ?? "created_at";
        _gsiPartitionKeyField = configuration["DynamoDB:GsiPartitionKeyField"] ?? "gsi_pk";
        _gsiSortKeyField = configuration["DynamoDB:GsiSortKeyField"] ?? "gsi_sk";
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        try
        {
            var table = Table.LoadTable(_dynamoDbClient, _tableName);

            string entityType = typeof(T).Name.ToUpper();
            string partitionKeyValue = $"{entityType}#{id}";
            string sortKeyValue = $"{entityType}#{id}";

            var document = await table.GetItemAsync(partitionKeyValue, sortKeyValue);

            if (document == null)
                return null;

            var obj = _context.FromDocument<T>(document);
            return obj;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var table = Table.LoadTable(_dynamoDbClient, _tableName);

        string entityType = typeof(T).Name.ToUpper();

        var search = table.Query(new QueryFilter(_partitionKeyField, QueryOperator.BeginsWith, $"{entityType}#"));
        var documentList = await search.GetRemainingAsync();

        var results = new List<T>();

        foreach (var document in documentList)
        {
            if (document[_entityTypeField].AsString() == entityType)
            {
                var obj = _context.FromDocument<T>(document);
                results.Add(obj);
            }
        }

        return results;
    }

    public async Task CreateAsync(T item)
    {
        var table = Table.LoadTable(_dynamoDbClient, _tableName);

        var document = _context.ToDocument(item);

        string entityType = typeof(T).Name.ToUpper();
        string id = GetIdFromItem(item);

        document[_partitionKeyField] = $"{entityType}#{id}";
        document[_sortKeyField] = $"{entityType}#{id}";
        document[_entityTypeField] = entityType;
        document[_createdAtField] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        if (item is Invoice invoice)
        {
            document[_gsiPartitionKeyField] = $"INVOICE#{invoice.CustomerId}";
            document[_gsiSortKeyField] = $"#INVOICES#{invoice.Id}";
        }
        else if (item is Customer customer)
        {
            document[_gsiPartitionKeyField] = $"CUSTOMER#{customer.Id}";
            document[_gsiSortKeyField] = "METADATA";
        }

        await table.PutItemAsync(document);
    }

    public async Task UpdateAsync(T item)
    {
        await CreateAsync(item);
    }

    public async Task DeleteAsync(string id)
    {
        var table = Table.LoadTable(_dynamoDbClient, _tableName);

        string entityType = typeof(T).Name.ToUpper();
        string partitionKeyValue = $"{entityType}#{id}";
        string sortKeyValue = $"{entityType}#{id}";

        await table.DeleteItemAsync(partitionKeyValue, sortKeyValue);
    }

    private static string GetIdFromItem(T item)
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            var idValue = idProperty.GetValue(item)?.ToString();
            return idValue ?? Guid.NewGuid().ToString();
        }
        return Guid.NewGuid().ToString();
    }
}
