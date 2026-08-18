using System.Text.Json;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Models;

namespace TicketValidator.Infrastructure.AI.Contracts;

internal sealed class ExpenseCoherenceResponse
{
    public bool? IsCoherent { get; init; }

    public IReadOnlyList<int> IncompatibleIndexes { get; init; } = [];
}

internal static class ExpenseCoherenceMapper
{
    internal static ExpenseCoherenceResult Map(TicketData ticket, ExpenseCoherenceResponse response)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(response);

        var incompatibleIndexes = response.IncompatibleIndexes
            ?? throw new JsonException("OpenAI returned incompatible concepts without indexes.");
        var uniqueIndexes = new HashSet<int>();
        var concepts = new List<string>();

        foreach (var index in incompatibleIndexes)
        {
            if (index < 0 || index >= ticket.Products.Count)
            {
                throw new InvalidOperationException("OpenAI returned an incompatible product index outside the ticket.");
            }

            if (!uniqueIndexes.Add(index))
            {
                throw new InvalidOperationException("OpenAI returned duplicate incompatible product indexes.");
            }

            var concept = ticket.Products[index].Concept;
            if (string.IsNullOrWhiteSpace(concept))
            {
                throw new InvalidOperationException("OpenAI marked a product without concept evidence as incompatible.");
            }

            concepts.Add(concept);
        }

        return new ExpenseCoherenceResult
        {
            IsCoherent = response.IsCoherent,
            IncompatibleConcepts = concepts
        };
    }
}
