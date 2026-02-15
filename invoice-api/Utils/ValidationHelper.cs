namespace InvoiceApi.Utils;

using DTO;

public static class ValidationHelper
{
    public static bool IsValidCustomer(CreateCustomerRequest customer)
    {
        return !string.IsNullOrWhiteSpace(customer.name);
    }

    public static bool IsValidInvoice(CreateInvoiceRequest invoice)
    {
        return !string.IsNullOrWhiteSpace(invoice.CustomerId) &&
               !string.IsNullOrWhiteSpace(invoice.CustomerName) &&
               invoice.Amount >= 0 &&
               invoice.IssueDate != default;
    }

    public static bool IsValidUpdateInvoice(UpdateInvoiceRequest invoice)
    {
        return !string.IsNullOrWhiteSpace(invoice.CustomerId) &&
               !string.IsNullOrWhiteSpace(invoice.CustomerName) &&
               invoice.Amount >= 0;
    }
}