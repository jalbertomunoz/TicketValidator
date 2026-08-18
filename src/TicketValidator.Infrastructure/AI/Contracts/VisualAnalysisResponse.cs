using System.Globalization;
using System.Text.Json;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;

namespace TicketValidator.Infrastructure.AI.Contracts;

internal sealed class VisualAnalysisResponse
{
    public string? DocumentType { get; init; }

    public string? VisualDate { get; init; }

    public decimal? VisualTotal { get; init; }

    public string? EstablishmentName { get; init; }

    public string? EstablishmentType { get; init; }

    public VisualAddressResponse? Address { get; init; }

    public string? TaxId { get; init; }

    public string? InvoiceNumber { get; init; }

    public string? Time { get; init; }

    public IReadOnlyList<VisualProductResponse> Products { get; init; } = [];

    public IReadOnlyList<VisualVatResponse> VatDetails { get; init; } = [];

    public bool? ManipulationDetected { get; init; }

    public string? Details { get; init; }
}

internal sealed class VisualAddressResponse
{
    public string? Street { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    public string? Country { get; init; }
}

internal sealed class VisualProductResponse
{
    public string? Concept { get; init; }

    public string? NormalizedText { get; init; }

    public decimal? Amount { get; init; }
}

internal sealed class VisualVatResponse
{
    public decimal? Rate { get; init; }

    public decimal? TaxableAmount { get; init; }

    public decimal? Amount { get; init; }
}

internal static class VisualAnalysisMapper
{
    internal static VisualAnalysisResult Map(VisualAnalysisResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new VisualAnalysisResult
        {
            VisualDocumentType = MapDocumentType(response.DocumentType),
            VisualDate = ParseDate(response.VisualDate),
            VisualTotal = response.VisualTotal,
            EstablishmentName = response.EstablishmentName,
            EstablishmentType = MapEstablishmentType(response.EstablishmentType),
            Address = MapAddress(response.Address),
            TaxId = response.TaxId,
            InvoiceNumber = response.InvoiceNumber,
            Time = response.Time,
            Products = response.Products.Select(product => new ProductData
            {
                Concept = product.Concept,
                NormalizedText = product.NormalizedText,
                Amount = product.Amount
            }).ToArray(),
            VatDetails = response.VatDetails.Select(vat => new VatData
            {
                Rate = vat.Rate,
                TaxableAmount = vat.TaxableAmount,
                Amount = vat.Amount
            }).ToArray(),
            ManipulationDetected = response.ManipulationDetected,
            Details = response.ManipulationDetected is false ? null : response.Details
        };
    }

    private static DocumentType MapDocumentType(string? documentType) => documentType switch
    {
        "TICKET" => DocumentType.Receipt,
        "FACTURA" => DocumentType.Invoice,
        "NO_DOCUMENTO" => DocumentType.NotDocument,
        "UNKNOWN" => DocumentType.Unknown,
        _ => throw new JsonException("The structured visual analysis response contains an invalid document type.")
    };

    private static EstablishmentType? MapEstablishmentType(string? establishmentType) => establishmentType switch
    {
        "RESTAURANT" => EstablishmentType.Restaurant,
        "HOTEL" => EstablishmentType.Hotel,
        "TRANSPORT" => EstablishmentType.Transport,
        "OTHER" => EstablishmentType.Other,
        "UNKNOWN" or null => null,
        _ => throw new JsonException("The structured visual analysis response contains an invalid establishment type.")
    };

    private static AddressData? MapAddress(VisualAddressResponse? address) => address is null
        ? null
        : new AddressData
        {
            Street = address.Street,
            City = address.City,
            PostalCode = address.PostalCode,
            Country = address.Country
        };

    private static DateOnly? ParseDate(string? visualDate)
    {
        if (visualDate is null)
        {
            return null;
        }

        if (DateOnly.TryParseExact(
            visualDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate))
        {
            return parsedDate;
        }

        throw new JsonException("The structured visual analysis response contains an invalid date.");
    }
}
