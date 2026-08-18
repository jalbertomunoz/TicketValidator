using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Infrastructure.AI;
using TicketValidator.Infrastructure.AI.Contracts;

namespace TicketValidator.UnitTests;

public sealed class OpenAiProductClassifierTests
{
    [Fact]
    public async Task ClassifyAsync_ReturnsEmptyWithoutCallingOpenAi_WhenProductsAreEmpty()
    {
        var classifier = new OpenAiProductClassifier(
            new ChatClient("gpt-4.1", "not-a-real-api-key"),
            Options.Create(new OpenAiOptions()));

        var result = await classifier.ClassifyAsync([]);

        Assert.Empty(result);
    }

    [Fact]
    public void Map_PreservesEvidenceFields_WhenClassifyingCherries()
    {
        var products = new[]
        {
            new ProductData
            {
                Concept = "CEREZAS",
                NormalizedText = "Cerezas",
                Amount = 3.25m
            }
        };

        var result = ProductClassificationMapper.Map(products, Response(0, "food", false));

        var product = Assert.Single(result);
        Assert.Equal("CEREZAS", product.Concept);
        Assert.Equal("Cerezas", product.NormalizedText);
        Assert.Equal(3.25m, product.Amount);
        Assert.Equal(ProductCategory.Food, product.Category);
        Assert.False(product.IsAlcohol);
    }

    [Theory]
    [InlineData("CERVEZA MAHOU", "alcoholicBeverage", true, ProductCategory.AlcoholicBeverage)]
    [InlineData("CERVEZA SIN ALCOHOL", "nonAlcoholicBeverage", false, ProductCategory.NonAlcoholicBeverage)]
    [InlineData("CERVEZA 0,0", "nonAlcoholicBeverage", false, ProductCategory.NonAlcoholicBeverage)]
    [InlineData("VINO SIN ALCOHOL", "nonAlcoholicBeverage", false, ProductCategory.NonAlcoholicBeverage)]
    [InlineData("LICOR DE CEREZAS", "alcoholicBeverage", true, ProductCategory.AlcoholicBeverage)]
    [InlineData("ZUMO DE CEREZA", "nonAlcoholicBeverage", false, ProductCategory.NonAlcoholicBeverage)]
    public void Map_AppliesStructuredClassification(
        string concept,
        string category,
        bool isAlcohol,
        ProductCategory expectedCategory)
    {
        var result = ProductClassificationMapper.Map(
            [new ProductData { Concept = concept }],
            Response(0, category, isAlcohol));

        var product = Assert.Single(result);
        Assert.Equal(expectedCategory, product.Category);
        Assert.Equal(isAlcohol, product.IsAlcohol);
    }

    [Fact]
    public void Map_Throws_WhenResponseContainsAnUnexpectedIndex()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ProductClassificationMapper.Map(
            [new ProductData { Concept = "AGUA" }],
            Response(1, "nonAlcoholicBeverage", false)));

        Assert.Equal("OpenAI returned a classification for an unexpected product index.", exception.Message);
    }

    [Fact]
    public void Map_UsesUnknownAndNull_WhenClassificationIsUnknown()
    {
        var result = ProductClassificationMapper.Map(
            [new ProductData { Concept = "PRODUCTO ILEGIBLE" }],
            Response(0, null, null));

        var product = Assert.Single(result);
        Assert.Equal(ProductCategory.Unknown, product.Category);
        Assert.Null(product.IsAlcohol);
    }

    [Fact]
    public void Map_UsesUnknownAndNull_WhenConceptIsEmpty()
    {
        var result = ProductClassificationMapper.Map(
            [new ProductData
            {
                Concept = " ",
                NormalizedText = "Sin texto",
                Amount = 1.50m,
                Category = ProductCategory.Food,
                IsAlcohol = false
            }],
            new ProductClassificationResponse());

        var product = Assert.Single(result);
        Assert.Equal(" ", product.Concept);
        Assert.Equal("Sin texto", product.NormalizedText);
        Assert.Equal(1.50m, product.Amount);
        Assert.Equal(ProductCategory.Unknown, product.Category);
        Assert.Null(product.IsAlcohol);
    }

    private static ProductClassificationResponse Response(int index, string? category, bool? isAlcohol) => new()
    {
        Classifications =
        [
            new ProductClassificationItemResponse
            {
                Index = index,
                Category = category,
                IsAlcohol = isAlcohol
            }
        ]
    };
}
