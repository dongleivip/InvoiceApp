namespace InvoiceApi.DTO;

public record CreateCustomerRequest(
    string Name,
    string? Id,
    string? Contact,
    string? Address,
    string? TaxId
);
