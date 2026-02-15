namespace InvoiceApi.DTO;

using Models;

public record InvoiceResponseModel(
    string Id,
    string CustomerId,
    string? CustomerName,
    string issueDate,
    decimal? Amount,
    string? DeliverAddress);

public static class InvoiceMappingExtensions
{
    public static InvoiceResponseModel ToResponseModel(this Invoice invoice) =>
        new InvoiceResponseModel(
            invoice.Id,
            invoice.CustomerId,
            invoice.CustomerName,
            invoice.IssueDate,
            invoice.Amount,
            invoice.DeliveryAddress);
}