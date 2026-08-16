using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Infrastructure.AI.Contracts;
using TicketValidator.Infrastructure.AI.Prompts;

namespace TicketValidator.UnitTests;

public sealed class OpenAiExpenseCoherenceAnalyzerTests
{
    [Fact]
    public void Map_RepresentsMealsMenuAndWaterAsCoherent()
    {
        var result = Map(
            ["MENU DEL DIA", "AGUA"],
            new ExpenseCoherenceResponse { IsCoherent = true },
            expenseType: ExpenseType.Meals);

        Assert.True(result.IsCoherent);
        Assert.Empty(result.IncompatibleConcepts);
    }

    [Fact]
    public void Map_RepresentsHamburguesaPlasticaAsCoherent()
    {
        var result = Map(["HAMBURGUESA PLASTICA"], new ExpenseCoherenceResponse { IsCoherent = true });

        Assert.True(result.IsCoherent);
    }

    [Fact]
    public void Map_RepresentsRestaurantChuletonAsCoherent()
    {
        var result = Map(
            ["CHULETON"],
            new ExpenseCoherenceResponse { IsCoherent = true },
            EstablishmentType.Restaurant);

        Assert.True(result.IsCoherent);
    }

    [Fact]
    public void Map_MapsRawFoodMajorityAsIncoherent()
    {
        var result = Map(
            ["CARNE CRUDA", "HARINA", "ACEITE"],
            new ExpenseCoherenceResponse { IsCoherent = false, IncompatibleIndexes = [0, 1, 2] });

        Assert.False(result.IsCoherent);
        Assert.Equal(["CARNE CRUDA", "HARINA", "ACEITE"], result.IncompatibleConcepts);
    }

    [Fact]
    public void Map_MapsNonFoodMajorityAsIncoherent()
    {
        var result = Map(
            ["DETERGENTE", "LEJIA", "AGUA"],
            new ExpenseCoherenceResponse { IsCoherent = false, IncompatibleIndexes = [0, 1] });

        Assert.False(result.IsCoherent);
        Assert.Equal(["DETERGENTE", "LEJIA"], result.IncompatibleConcepts);
    }

    [Fact]
    public void Map_RepresentsAmbiguousProductAsUnknown()
    {
        var result = Map(["CONCEPTO ILEGIBLE"], new ExpenseCoherenceResponse { IsCoherent = null });

        Assert.Null(result.IsCoherent);
        Assert.Empty(result.IncompatibleConcepts);
    }

    [Fact]
    public void Map_RepresentsTaxiAsCoherentTaxiExpense()
    {
        var result = Map(
            ["SERVICIO TAXI"],
            new ExpenseCoherenceResponse { IsCoherent = true },
            expenseType: ExpenseType.Taxi);

        Assert.True(result.IsCoherent);
    }

    [Fact]
    public void Map_RepresentsFuelAsCoherentFuelExpense()
    {
        var result = Map(
            ["GASOLINA 95"],
            new ExpenseCoherenceResponse { IsCoherent = true },
            expenseType: ExpenseType.Fuel);

        Assert.True(result.IsCoherent);
    }

    [Fact]
    public void Map_RepresentsAccommodationAsCoherent()
    {
        var result = Map(
            ["ALOJAMIENTO HABITACION"],
            new ExpenseCoherenceResponse { IsCoherent = true },
            expenseType: ExpenseType.Accommodation);

        Assert.True(result.IsCoherent);
    }

    [Theory]
    [InlineData(ExpenseType.Parking, "APARCAMIENTO")]
    [InlineData(ExpenseType.Highway, "PEAJE")]
    [InlineData(ExpenseType.Material, "CARTUCHO IMPRESORA")]
    public void Map_RepresentsExplicitExpenseTypeAsCoherent(ExpenseType expenseType, string concept)
    {
        var ticket = new TicketData
        {
            Products = [new ProductData { OcrText = concept }]
        };

        var message = ExpenseCoherencePrompt.CreateUserMessage(ticket, expenseType);
        var result = ExpenseCoherenceMapper.Map(ticket, new ExpenseCoherenceResponse { IsCoherent = true });

        Assert.Contains($"ExpenseType: {expenseType}", message);
        Assert.Contains(concept, message);
        Assert.True(result.IsCoherent);
    }

    [Fact]
    public void Map_ThrowsWhenStructuredResponseContainsAnInvalidIndex()
    {
        Assert.Throws<InvalidOperationException>(() => Map(
            ["AGUA"],
            new ExpenseCoherenceResponse { IsCoherent = false, IncompatibleIndexes = [1] }));
    }

    private static TicketValidator.Application.DTOs.ExpenseCoherenceResult Map(
        IReadOnlyList<string> concepts,
        ExpenseCoherenceResponse response,
        EstablishmentType establishmentType = EstablishmentType.Unknown,
        ExpenseType expenseType = ExpenseType.Other)
    {
        var ticket = new TicketData
        {
            EstablishmentType = establishmentType,
            Products = concepts.Select(concept => new ProductData { OcrText = concept }).ToArray()
        };

        Assert.Contains($"ExpenseType: {expenseType}", ExpenseCoherencePrompt.CreateUserMessage(ticket, expenseType));
        return ExpenseCoherenceMapper.Map(ticket, response);
    }
}
