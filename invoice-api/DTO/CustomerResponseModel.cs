namespace InvoiceApi.DTO;

using Models;

public record CustomerResponseModel(
    string Id,
    string Name,
    string Contact,
    string Address,
    string TaxId);

public static class CustomerMappingExtensions
{
    public static CustomerResponseModel ToResponseModel(this Customer customer) =>
        new CustomerResponseModel(
            customer.Id,
            customer.Name,
            customer.Contact,
            customer.Address,
            customer.TaxId);
}