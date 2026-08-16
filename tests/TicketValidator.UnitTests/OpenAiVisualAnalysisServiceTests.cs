using TicketValidator.Infrastructure.AI;
using TicketValidator.Infrastructure.AI.Contracts;

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

    [Fact]
    public void Map_PreservesNullVisualDate()
    {
        var result = VisualAnalysisMapper.Map(new VisualAnalysisResponse
        {
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
            ManipulationDetected = false,
            Details = "No se observan indicios."
        });

        Assert.Null(result.Details);
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
