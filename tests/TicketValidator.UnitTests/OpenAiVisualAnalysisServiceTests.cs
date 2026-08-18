using TicketValidator.Infrastructure.AI;
using TicketValidator.Infrastructure.AI.Contracts;
using TicketValidator.Infrastructure.AI.Prompts;
using TicketValidator.Domain.Enums;

namespace TicketValidator.UnitTests;

public sealed class OpenAiVisualAnalysisServiceTests
{
    [Fact]
    public void GetImageMediaType_ThrowsArgumentException_WhenImageIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => OpenAiVisualAnalysisService.GetImageMediaType([]));
    }

    [Fact]
    public void GetImageMediaType_ThrowsArgumentException_WhenFormatIsUnsupported()
    {
        Assert.Throws<ArgumentException>(() => OpenAiVisualAnalysisService.GetImageMediaType([0x01, 0x02, 0x03]));
    }

    [Fact]
    public void GetImageMediaType_ReturnsPng_WhenPngHeaderIsValid()
    {
        Assert.Equal("image/png", OpenAiVisualAnalysisService.GetImageMediaType(CreatePng()));
    }

    [Fact]
    public void Map_MapsValidDateTotalAndFalseManipulation()
    {
        var result = VisualAnalysisMapper.Map(new VisualAnalysisResponse
        {
            DocumentType = "TICKET",
            VisualDate = "2026-08-16",
            VisualTotal = 12.50m,
            ManipulationDetected = false,
            Details = null
        });

        Assert.Equal(new DateOnly(2026, 8, 16), result.VisualDate);
        Assert.Equal(12.50m, result.VisualTotal);
        Assert.False(result.ManipulationDetected);
        Assert.Null(result.Details);
    }

    [Theory]
    [InlineData("TICKET", DocumentType.Receipt)]
    [InlineData("FACTURA", DocumentType.Invoice)]
    [InlineData("NO_DOCUMENTO", DocumentType.NotDocument)]
    [InlineData("UNKNOWN", DocumentType.Unknown)]
    public void Map_MapsVisualDocumentType(string documentType, DocumentType expectedDocumentType)
    {
        var result = VisualAnalysisMapper.Map(new VisualAnalysisResponse
        {
            DocumentType = documentType,
            ManipulationDetected = false
        });

        Assert.Equal(expectedDocumentType, result.VisualDocumentType);
    }

    [Fact]
    public void Map_PreservesNullVisualDate()
    {
        var result = VisualAnalysisMapper.Map(new VisualAnalysisResponse
        {
            DocumentType = "UNKNOWN",
            VisualDate = null,
            VisualTotal = 12.50m,
            ManipulationDetected = false
        });

        Assert.Null(result.VisualDate);
    }

    [Fact]
    public void Map_PreservesNullVisualTotal()
    {
        var result = VisualAnalysisMapper.Map(new VisualAnalysisResponse
        {
            DocumentType = "UNKNOWN",
            VisualDate = "2026-08-16",
            VisualTotal = null,
            ManipulationDetected = false
        });

        Assert.Null(result.VisualTotal);
    }

    [Fact]
    public void Map_PreservesDetectedManipulationAndDetails()
    {
        var result = VisualAnalysisMapper.Map(new VisualAnalysisResponse
        {
            DocumentType = "TICKET",
            ManipulationDetected = true,
            Details = "Se observa una sobrescritura en el importe total."
        });

        Assert.True(result.ManipulationDetected);
        Assert.Equal("Se observa una sobrescritura en el importe total.", result.Details);
    }

    [Fact]
    public void Map_ClearsDetails_WhenManipulationIsFalse()
    {
        var result = VisualAnalysisMapper.Map(new VisualAnalysisResponse
        {
            DocumentType = "TICKET",
            ManipulationDetected = false,
            Details = "No se observan indicios."
        });

        Assert.Null(result.Details);
    }

    [Fact]
    public void Map_MapsVisualSemanticTicketData()
    {
        var result = VisualAnalysisMapper.Map(new VisualAnalysisResponse
        {
            DocumentType = "FACTURA",
            EstablishmentName = "Proveedor Ejemplo",
            EstablishmentType = "RESTAURANT",
            Address = new VisualAddressResponse
            {
                Street = "Calle Mayor 1",
                City = "Madrid",
                PostalCode = "28001",
                Country = "ES"
            },
            TaxId = "B12345678",
            InvoiceNumber = "F-2026-1",
            Time = "14:30",
            Products =
            [
                new VisualProductResponse
                {
                    Concept = "MENU DEL DIA",
                    NormalizedText = "Menu del dia",
                    Amount = 12.50m
                }
            ],
            VatDetails = [new VisualVatResponse { Rate = 10m, TaxableAmount = 11.36m, Amount = 1.14m }],
            ManipulationDetected = false
        });

        Assert.Equal("Proveedor Ejemplo", result.EstablishmentName);
        Assert.Equal(EstablishmentType.Restaurant, result.EstablishmentType);
        Assert.Equal("Calle Mayor 1", result.Address!.Street);
        Assert.Equal("B12345678", result.TaxId);
        Assert.Equal("F-2026-1", result.InvoiceNumber);
        Assert.Equal("14:30", result.Time);
        Assert.Equal("MENU DEL DIA", Assert.Single(result.Products).Concept);
        Assert.Equal(10m, Assert.Single(result.VatDetails).Rate);
    }

    [Fact]
    public void Map_PreservesNullEstablishmentType()
    {
        var result = VisualAnalysisMapper.Map(new VisualAnalysisResponse
        {
            DocumentType = "TICKET",
            EstablishmentType = null,
            ManipulationDetected = false
        });

        Assert.Null(result.EstablishmentType);
    }

    [Fact]
    public void VisualAnalysisPrompt_RequiresIssuerDataAndExcludesCustomerData()
    {
        Assert.Contains("emisor", VisualAnalysisPrompt.SystemMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nunca al cliente", VisualAnalysisPrompt.SystemMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] CreatePng()
    {
        var image = new byte[45];
        image[0] = 0x89;
        image[1] = 0x50;
        image[2] = 0x4E;
        image[3] = 0x47;
        image[4] = 0x0D;
        image[5] = 0x0A;
        image[6] = 0x1A;
        image[7] = 0x0A;
        image[12] = 0x49;
        image[13] = 0x48;
        image[14] = 0x44;
        image[15] = 0x52;
        image[^8] = 0x49;
        image[^7] = 0x45;
        image[^6] = 0x4E;
        image[^5] = 0x44;

        return image;
    }
}
