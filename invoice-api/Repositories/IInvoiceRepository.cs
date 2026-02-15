namespace InvoiceApi.Repositories;

using Models;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(string customerId, string invoiceId, string issueDate);

    Task<Invoice?> GetByOnlyIdAsync(string invoiceId);

    /// <summary>
    /// Get Invoices for specific customer.
    /// </summary>
    /// <param name="customerId">customer identifier.</param>
    /// <returns>All Invoices belong to the customer.</returns>
    Task<IEnumerable<Invoice>> GetByCustomerAsync(string customerId);

    /// <summary>
    /// Get Invoices for specific customer with a certain period.
    /// </summary>
    /// <param name="customerId">customer identifier.</param>
    /// <param name="start">start date.</param>
    /// <param name="end">end date.</param>
    /// <returns>All Invoices belong to the customer within the date range.</returns>
    Task<IEnumerable<Invoice>> GetByCustomerAndDateAsync(string customerId, DateTime start, DateTime end);

    /// <summary>
    /// Get Invoices for a certain period.
    /// </summary>
    /// <param name="start">start date.</param>
    /// <param name="end">end date.</param>
    /// <returns>All Invoices within the period.</returns>
    Task<IEnumerable<Invoice>> GetAllByDateRangeAsync(DateTime start, DateTime end);

    Task CreateAsync(Invoice invoice);

    Task UpdateAsync(Invoice invoice);

    /// <summary>
    /// Delete invoice by the key composition.
    /// </summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="invoiceId">Invoice identifier.</param>
    /// <param name="issueDate">The issueDate of the invoice, format yyyy-MM-dd.</param>
    /// <returns>Nothing.</returns>
    Task DeleteAsync(string customerId, string invoiceId, string issueDate);

    Task DeleteByKeyAsync(string pk, string sk);

    /// <summary>
    /// Delete invoice by invoice id.
    /// </summary>
    /// <param name="invoiceId">Invoice identifier.</param>
    /// <returns>Nothing.</returns>
    Task DeleteByIdAsync(string invoiceId);
}