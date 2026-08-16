namespace TicketValidator.Infrastructure.AI.Contracts;

internal static class VisualAnalysisSchema
{
    internal const string Name = "visual_ticket_analysis";

    internal const string Json = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "documentType": { "type": "string", "enum": ["TICKET", "FACTURA", "NO_DOCUMENTO", "UNKNOWN"] },
            "visualDate": { "type": ["string", "null"], "pattern": "^\\d{4}-\\d{2}-\\d{2}$" },
            "visualTotal": { "type": ["number", "null"] },
            "manipulationDetected": { "type": ["boolean", "null"] },
            "details": { "type": ["string", "null"] }
          },
          "required": ["documentType", "visualDate", "visualTotal", "manipulationDetected", "details"]
        }
        """;
}
