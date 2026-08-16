using TicketValidator.Domain.Enums;
using TicketValidator.Infrastructure.AI;
using TicketValidator.Infrastructure.AI.Contracts;

namespace TicketValidator.UnitTests;

public sealed class OpenAiTicketExtractorTests
{
    [Fact]
    public void ValidateOcrText_ThrowsArgumentException_WhenTextIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => OpenAiTicketExtractor.ValidateOcrText(" "));
    }

    [Fact]
    public void Map_PreservesNullFields()
    {
        var extraction = TicketExtractionMapper.Map(new TicketExtractionResponse());

        Assert.Equal(DocumentType.Unknown, extraction.Ticket.DocumentType);
        Assert.Null(extraction.Ticket.EstablishmentName);
        Assert.Null(extraction.Ticket.TaxId);
        Assert.Null(extraction.Ticket.InvoiceNumber);
        Assert.Null(extraction.Ticket.Date);
        Assert.Null(extraction.Ticket.Time);
        Assert.Null(extraction.Ticket.Total);
        Assert.Null(extraction.Ticket.Address);
        Assert.Empty(extraction.Ticket.VatDetails);
        Assert.Empty(extraction.Ticket.Products);
    }

    [Fact]
    public void Map_PreservesProductOcrTextWithoutSemanticSubstitution()
    {
        var extraction = TicketExtractionMapper.Map(new TicketExtractionResponse
        {
            Products =
            [
                new TicketProductResponse
                {
                    OcrText = "CEREZAS",
                    NormalizedText = "Cerezas",
                    Amount = 3.50m
                }
            ]
        });

        var product = Assert.Single(extraction.Ticket.Products);
        Assert.Equal("CEREZAS", product.OcrText);
        Assert.Equal("Cerezas", product.NormalizedText);
        Assert.Equal(3.50m, product.Amount);
        Assert.Null(product.Category);
        Assert.Null(product.IsAlcohol);
    }

    [Fact]
    public void Map_MapsStructuredTicketData()
    {
        var extraction = TicketExtractionMapper.Map(new TicketExtractionResponse
        {
            DocumentType = "ticket",
            EstablishmentName = "TICKETVALIDATOR",
            TaxId = "B12345678",
            InvoiceNumber = "T-2026-001",
            Date = "2026-08-16",
            Time = "14:30",
            Total = 12.50m,
            Address = new TicketAddressResponse
            {
                Street = "Calle Ejemplo 1",
                City = "Madrid",
                PostalCode = "28001",
                Country = "ES"
            },
            VatDetails = [new TicketVatResponse { Rate = 10m, TaxableAmount = 11.36m, Amount = 1.14m }]
        });

        Assert.Equal(DocumentType.Receipt, extraction.Ticket.DocumentType);
        Assert.Equal("TICKETVALIDATOR", extraction.Ticket.EstablishmentName);
        Assert.Equal("B12345678", extraction.Ticket.TaxId);
        Assert.Equal("T-2026-001", extraction.Ticket.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 8, 16), extraction.Ticket.Date);
        Assert.Equal("14:30", extraction.Ticket.Time);
        Assert.Equal(12.50m, extraction.Ticket.Total);
        Assert.Equal("Calle Ejemplo 1", extraction.Ticket.Address!.Street);
        var vat = Assert.Single(extraction.Ticket.VatDetails);
        Assert.Equal(10m, vat.Rate);
        Assert.Equal(11.36m, vat.TaxableAmount);
        Assert.Equal(1.14m, vat.Amount);
    }
}
