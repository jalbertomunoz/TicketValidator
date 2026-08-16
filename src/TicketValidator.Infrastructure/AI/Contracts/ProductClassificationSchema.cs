namespace TicketValidator.Infrastructure.AI.Contracts;

internal static class ProductClassificationSchema
{
    internal const string Name = "product_classification";

    internal const string Json = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "classifications": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "index": { "type": "integer" },
                  "category": { "type": ["string", "null"], "enum": ["unknown", "food", "nonAlcoholicBeverage", "alcoholicBeverage", "other", null] },
                  "isAlcohol": { "type": ["boolean", "null"] }
                },
                "required": ["index", "category", "isAlcohol"]
              }
            }
          },
          "required": ["classifications"]
        }
        """;
}
