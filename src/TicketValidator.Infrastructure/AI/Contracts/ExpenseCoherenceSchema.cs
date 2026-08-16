namespace TicketValidator.Infrastructure.AI.Contracts;

internal static class ExpenseCoherenceSchema
{
    internal const string Name = "expense_coherence";

    internal const string Json = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "isCoherent": { "type": ["boolean", "null"] },
            "incompatibleIndexes": {
              "type": "array",
              "items": { "type": "integer" }
            }
          },
          "required": ["isCoherent", "incompatibleIndexes"]
        }
        """;
}
