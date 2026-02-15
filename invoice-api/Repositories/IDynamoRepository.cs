namespace InvoiceApi.Repositories;

using Amazon.DynamoDBv2.DocumentModel;
using Models;

public interface IDynamoRepository<T>
    where T : DataEntity
{
    Task<T?> GetAsync(string pk, string sk);

    Task CreateAsync(T entity);

    Task DeleteAsync(string pk, string sk);

    Task<IEnumerable<T>> QueryAsync(string pk, QueryOperationConfig? config = null);
}