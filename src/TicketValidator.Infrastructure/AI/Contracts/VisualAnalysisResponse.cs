using System.Globalization;
using System.Text.Json;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Enums;

namespace TicketValidator.Infrastructure.AI.Contracts;

internal sealed class VisualAnalysisResponse
{
    public string? DocumentType { get; init; }

    public string? VisualDate { get; init; }

    public decimal? VisualTotal { get; init; }

    public bool? ManipulationDetected { get; init; }

    public string? Details { get; init; }
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
