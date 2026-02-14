using Amazon.DynamoDBv2.DocumentModel;
using InvoiceApi.Models;

namespace InvoiceApi.Repositories;

public interface IDynamoRepository<T> where T : DataEntity
{
    Task<T?> GetAsync(string pk, string sk);
    Task CreateAsync(T entity);
    Task DeleteAsync(string pk, string sk);
    Task<IEnumerable<T>> QueryAsync(string pk, QueryOperationConfig?  config = null);
}
