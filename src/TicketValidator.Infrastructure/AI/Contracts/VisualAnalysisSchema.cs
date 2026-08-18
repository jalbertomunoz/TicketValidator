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
            "establishmentName": { "type": ["string", "null"] },
            "establishmentType": { "type": ["string", "null"], "enum": ["RESTAURANT", "HOTEL", "TRANSPORT", "OTHER", "UNKNOWN", null] },
            "address": {
              "type": ["object", "null"],
              "additionalProperties": false,
              "properties": {
                "street": { "type": ["string", "null"] },
                "city": { "type": ["string", "null"] },
                "postalCode": { "type": ["string", "null"] },
                "country": { "type": ["string", "null"] }
              },
              "required": ["street", "city", "postalCode", "country"]
            },
            "taxId": { "type": ["string", "null"] },
            "invoiceNumber": { "type": ["string", "null"] },
            "time": { "type": ["string", "null"] },
            "products": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "concept": { "type": ["string", "null"] },
                  "normalizedText": { "type": ["string", "null"] },
                  "amount": { "type": ["number", "null"] }
                },
                "required": ["concept", "normalizedText", "amount"]
              }
            },
            "vatDetails": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "rate": { "type": ["number", "null"] },
                  "taxableAmount": { "type": ["number", "null"] },
                  "amount": { "type": ["number", "null"] }
                },
                "required": ["rate", "taxableAmount", "amount"]
              }
            },
            "manipulationDetected": { "type": ["boolean", "null"] },
            "details": { "type": ["string", "null"] }
          },
          "required": ["documentType", "visualDate", "visualTotal", "establishmentName", "establishmentType", "address", "taxId", "invoiceNumber", "time", "products", "vatDetails", "manipulationDetected", "details"]
        }
        """;
}
