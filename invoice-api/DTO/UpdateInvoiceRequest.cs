namespace InvoiceApi.DTO;

public record UpdateInvoiceRequest(
    string InvoiceId,
    string CustomerId,
    string? CustomerName,
    decimal Amount,
    string? DeliveryAddress);
