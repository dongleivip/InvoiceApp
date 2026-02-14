using InvoiceApi.DTO;
using InvoiceApi.Models;

namespace InvoiceApi.Utils;

public static class ValidationHelper
{
    public static bool IsValidCustomer(CreateCustomerRequest customer)
    {
        return !string.IsNullOrWhiteSpace(customer.Name);
    }

    public static bool IsValidInvoice(Invoice invoice)
    {
        return !string.IsNullOrWhiteSpace(invoice.CustomerId) &&
               !string.IsNullOrWhiteSpace(invoice.CustomerName) &&
               invoice.Amount > 0 &&
               invoice.IssueDate != default;
    }
}