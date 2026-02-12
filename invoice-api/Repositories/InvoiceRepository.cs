using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using InvoiceApi.Models;
using Microsoft.Extensions.Configuration;

namespace InvoiceApi.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    private readonly string _partitionKeyField;
    private readonly string _entityTypeField;
    private readonly string _gsiPartitionKeyField;

    public InvoiceRepository(IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = configuration["DynamoDB:TableName"] ?? "InvoiceApp";
        _partitionKeyField = configuration["DynamoDB:PartitionKeyField"] ?? "pk";
        _entityTypeField = configuration["DynamoDB:EntityTypeField"] ?? "entity_type";
        _gsiPartitionKeyField = configuration["DynamoDB:GsiPartitionKeyField"] ?? "gsi_pk";
    }

    public async Task<IEnumerable<Invoice>> GetByCustomerIdAsync(string customerId)
    {
        var table = Table.LoadTable(_dynamoDbClient, _tableName);

        // Query using GSI for customer-specific invoices
        var search = table.Query(new QueryFilter(_gsiPartitionKeyField, QueryOperator.Equal, $"INVOICE#{customerId}"));
        var documentList = await search.GetRemainingAsync();

        var results = new List<Invoice>();

        foreach (var document in documentList)
        {
            if (document[_entityTypeField].AsString() == "INVOICE")
            {
                var invoice = new Invoice
                {
                    Id = ExtractIdFromKey(document[_partitionKeyField].AsString()),
                    CustomerId = customerId,
                    CustomerName = document.ContainsKey("CustomerName") ? document["CustomerName"].AsString() : "",
                    Amount = document.ContainsKey("Amount") ? decimal.Parse(document["Amount"].AsString()) : 0,
                    IssueDate = document.ContainsKey("IssueDate") ? DateTime.Parse(document["IssueDate"].AsString()) : DateTime.MinValue
                };

                results.Add(invoice);
            }
        }

        return results;
    }

    private string ExtractIdFromKey(string key)
    {
        // Assuming the key format is ENTITYTYPE#ID
        if (key.Contains('#'))
        {
            return key.Split('#')[1];
        }
        return key;
    }
}