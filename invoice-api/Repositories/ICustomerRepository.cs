namespace InvoiceApi.Repositories;

using Models;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(string customerId);

    Task CreateAsync(Customer customer);

    Task UpdateAsync(Customer customer);

    Task DeleteAsync(string pk, string sk);

    Task<IEnumerable<Customer>> GetAllAsync();
}