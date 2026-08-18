using System.Text.Json;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;

namespace TicketValidator.Infrastructure.AI.Contracts;

internal sealed class ProductClassificationResponse
{
    public IReadOnlyList<ProductClassificationItemResponse> Classifications { get; init; } = [];
}

internal sealed class ProductClassificationItemResponse
{
    public int Index { get; init; }

    public string? Category { get; init; }

    public bool? IsAlcohol { get; init; }
}

internal static class ProductClassificationMapper
{
    internal static IReadOnlyList<ProductData> Map(
        IReadOnlyList<ProductData> products,
        ProductClassificationResponse response)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(response);

        var expectedIndices = products
            .Select((product, index) => new { product.Concept, Index = index })
            .Where(product => !string.IsNullOrWhiteSpace(product.Concept))
            .Select(product => product.Index)
            .ToHashSet();
        var classifications = response.Classifications
            ?? throw new JsonException("OpenAI returned classifications without items.");

        if (classifications.Count != expectedIndices.Count)
        {
            throw new InvalidOperationException("OpenAI returned an incompatible number of product classifications.");
        }

        var classificationsByIndex = new Dictionary<int, ProductClassificationItemResponse>();
        foreach (var classification in classifications)
        {
            if (!expectedIndices.Contains(classification.Index))
            {
                throw new InvalidOperationException("OpenAI returned a classification for an unexpected product index.");
            }

            if (!classificationsByIndex.TryAdd(classification.Index, classification))
            {
                throw new InvalidOperationException("OpenAI returned duplicate product classification indices.");
            }
        }

        if (classificationsByIndex.Count != expectedIndices.Count)
        {
            throw new InvalidOperationException("OpenAI returned incomplete product classifications.");
        }

        return products.Select((product, index) =>
        {
            if (string.IsNullOrWhiteSpace(product.Concept))
            {
                return CreateProduct(product, ProductCategory.Unknown, null);
            }

            var classification = classificationsByIndex[index];
            return CreateProduct(product, MapCategory(classification.Category), classification.IsAlcohol);
        }).ToArray();
    }

    private static ProductData CreateProduct(ProductData product, ProductCategory category, bool? isAlcohol) => new()
    {
            Concept = product.Concept,
        NormalizedText = product.NormalizedText,
        Amount = product.Amount,
        Category = category,
        IsAlcohol = isAlcohol
    };

    private static ProductCategory MapCategory(string? category) => category switch
    {
        "food" => ProductCategory.Food,
        "nonAlcoholicBeverage" => ProductCategory.NonAlcoholicBeverage,
        "alcoholicBeverage" => ProductCategory.AlcoholicBeverage,
        "other" => ProductCategory.Other,
        "unknown" or null => ProductCategory.Unknown,
        _ => throw new JsonException("OpenAI returned an invalid product category.")
    };
}
