namespace TicketValidator.Infrastructure.AI.Contracts;

internal static class TicketExtractionSchema
{
    internal const string Name = "ticket_extraction";

    internal const string Json = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "documentType": { "type": ["string", "null"], "enum": ["ticket", "receipt", "factura", "invoice", "unknown", null] },
            "establishmentName": { "type": ["string", "null"] },
            "taxId": { "type": ["string", "null"] },
            "invoiceNumber": { "type": ["string", "null"] },
            "date": { "type": ["string", "null"] },
            "time": { "type": ["string", "null"] },
            "total": { "type": ["number", "null"] },
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
            "products": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "ocrText": { "type": ["string", "null"] },
                  "normalizedText": { "type": ["string", "null"] },
                  "amount": { "type": ["number", "null"] }
                },
                "required": ["ocrText", "normalizedText", "amount"]
              }
            }
          },
          "required": ["documentType", "establishmentName", "taxId", "invoiceNumber", "date", "time", "total", "address", "vatDetails", "products"]
        }
        """;
}
