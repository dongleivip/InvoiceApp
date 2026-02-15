namespace InvoiceApi.DTO;

public record CreateCustomerRequest(
    string name,
    string? id,
    string? contact,
    string? address,
    string? taxId);
