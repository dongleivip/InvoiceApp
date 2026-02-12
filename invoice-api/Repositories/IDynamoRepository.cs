using InvoiceApi.Models;

namespace InvoiceApi.Repositories;

public interface IDynamoRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task CreateAsync(T item);
    Task UpdateAsync(T item);
    Task DeleteAsync(string id);
}

// Additional interface for invoice-specific queries
public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetByCustomerIdAsync(string customerId);
}