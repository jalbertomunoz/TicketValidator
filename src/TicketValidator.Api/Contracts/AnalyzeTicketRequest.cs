using System.Text.Json.Serialization;
using TicketValidator.Domain.Enums;

namespace TicketValidator.Api.Contracts;

public sealed class AnalyzeTicketRequest
{
    /// <summary>Imagen JPEG o PNG del ticket o factura.</summary>
    [JsonPropertyName("file")]
    public IFormFile? File { get; init; }

    /// <summary>Tipo de gasto declarado.</summary>
    [JsonPropertyName("expenseType")]
    public ExpenseType ExpenseType { get; init; }
}
