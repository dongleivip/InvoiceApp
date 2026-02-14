using InvoiceApi.Models;

namespace InvoiceApi.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(string customerId);
    Task CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(string pk, string sk);
    Task<IEnumerable<Customer>> GetAllAsync();
}