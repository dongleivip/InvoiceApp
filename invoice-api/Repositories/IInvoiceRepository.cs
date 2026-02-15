namespace InvoiceApi.Repositories;

using Models;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(string invoiceId);

    Task<IEnumerable<Invoice>> GetCustomerInvoicesAsync(string customerId);

    Task CreateAsync(Invoice invoice);

    Task UpdateAsync(Invoice invoice);

    Task DeleteAsync(string pk, string sk);
}