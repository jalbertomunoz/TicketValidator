using System.Globalization;
using System.Text.Json;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;

namespace TicketValidator.Infrastructure.AI.Contracts;

internal sealed class TicketExtractionResponse
{
    public string? DocumentType { get; init; }

    public string? EstablishmentName { get; init; }

    public string? TaxId { get; init; }

    public string? InvoiceNumber { get; init; }

    public string? Date { get; init; }

    public string? Time { get; init; }

    public decimal? Total { get; init; }

    public TicketAddressResponse? Address { get; init; }

    public IReadOnlyList<TicketVatResponse> VatDetails { get; init; } = [];

    public IReadOnlyList<TicketProductResponse> Products { get; init; } = [];
}

internal sealed class TicketAddressResponse
{
    public string? Street { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    public string? Country { get; init; }
}

internal sealed class TicketVatResponse
{
    public decimal? Rate { get; init; }

    public decimal? TaxableAmount { get; init; }

    public decimal? Amount { get; init; }
}

internal sealed class TicketProductResponse
{
    public string? OcrText { get; init; }

    public string? NormalizedText { get; init; }

    public decimal? Amount { get; init; }
}

internal static class TicketExtractionMapper
{
    internal static AiTicketExtraction Map(TicketExtractionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new AiTicketExtraction
        {
            Ticket = new TicketData
            {
                DocumentType = MapDocumentType(response.DocumentType),
                EstablishmentName = response.EstablishmentName,
                TaxId = response.TaxId,
                InvoiceNumber = response.InvoiceNumber,
                Date = ParseDate(response.Date),
                Time = response.Time,
                Total = response.Total,
                Address = MapAddress(response.Address),
                VatDetails = response.VatDetails.Select(vat => new VatData
                {
                    Rate = vat.Rate,
                    TaxableAmount = vat.TaxableAmount,
                    Amount = vat.Amount
                }).ToArray(),
                Products = response.Products.Select(product => new ProductData
                {
                    OcrText = product.OcrText,
                    NormalizedText = product.NormalizedText,
                    Amount = product.Amount,
                    Category = null,
                    IsAlcohol = null
                }).ToArray()
            }
        };
    }

    private static DocumentType MapDocumentType(string? documentType) => documentType?.ToLowerInvariant() switch
    {
        "ticket" or "receipt" => DocumentType.Receipt,
        "factura" or "invoice" => DocumentType.Invoice,
        _ => DocumentType.Unknown
    };

    private static DateOnly? ParseDate(string? date)
    {
        if (date is null)
        {
            return null;
        }

        if (DateOnly.TryParseExact(
            date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate))
        {
            return parsedDate;
        }

        throw new JsonException("The structured response contains an invalid date.");
    }

    private static AddressData? MapAddress(TicketAddressResponse? address) => address is null
        ? null
        : new AddressData
        {
            Street = address.Street,
            City = address.City,
            PostalCode = address.PostalCode,
            Country = address.Country
        };
}
