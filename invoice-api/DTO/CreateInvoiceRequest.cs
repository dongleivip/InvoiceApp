namespace InvoiceApi.DTO;

public record CreateInvoiceRequest(
    string CustomerId,
    string? CustomerName,
    DateTime IssueDate,
    decimal Amount,
    string? DeliveryAddress);

public record UpdateInvoiceRequest(
    string InvoiceId,
    string CustomerId,
    string? CustomerName,
    decimal Amount,
    string? DeliveryAddress);
