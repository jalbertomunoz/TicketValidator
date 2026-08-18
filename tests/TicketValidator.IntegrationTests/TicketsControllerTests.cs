using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TicketValidator.Api.Configuration;
using TicketValidator.Api.Contracts;
using TicketValidator.Api.Controllers;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Application.UseCases.AnalyzeTicket;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.IntegrationTests;

public sealed class TicketsControllerTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsBadRequest_WhenFileIsMissing()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest { ExpenseType = ExpenseType.Meals },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsBadRequest_WhenFileIsEmpty()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest { File = CreateFile([], "image/jpeg"), ExpenseType = ExpenseType.Meals },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsBadRequest_WhenExpenseTypeIsUnknown()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest { File = CreateFile([0xFF, 0xD8, 0xFF], "image/jpeg"), ExpenseType = ExpenseType.Unknown },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsBadRequest_WhenContentTypeIsUnsupported()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest { File = CreateFile([0xFF, 0xD8, 0xFF], "application/pdf"), ExpenseType = ExpenseType.Meals },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsBadRequest_WhenJpegExtensionHasPngSignature()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest
            {
                File = CreateFile([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "image/jpeg", "ticket.jpg"),
                ExpenseType = ExpenseType.Meals
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsBadRequest_WhenFileExceedsConfiguredLimit()
    {
        var result = await CreateController(maxFileSizeBytes: 3).AnalyzeAsync(
            new AnalyzeTicketRequest { File = CreateFile([0xFF, 0xD8, 0xFF, 0x00], "image/jpeg"), ExpenseType = ExpenseType.Meals },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Theory]
    [InlineData(AnalysisStatus.Approved, ReasonCode.Ok, "APPROVED", "OK")]
    [InlineData(AnalysisStatus.Rejected, ReasonCode.ErrBebidaAlcoholica, "REJECTED", "ERR_BEBIDA_ALCOHOLICA")]
    [InlineData(AnalysisStatus.ReviewRequired, ReasonCode.DateMismatch, "REVIEW_REQUIRED", "DATE_MISMATCH")]
    [InlineData(AnalysisStatus.ReviewRequired, ReasonCode.ErrSinCif, "REVIEW_REQUIRED", "ERR_SIN_CIF")]
    [InlineData(AnalysisStatus.ReviewRequired, ReasonCode.ErrFechaAntigua, "REVIEW_REQUIRED", "ERR_FECHA_ANTIGUA")]
    [InlineData(AnalysisStatus.ReviewRequired, ReasonCode.ErrFechaFutura, "REVIEW_REQUIRED", "ERR_FECHA_FUTURA")]
    [InlineData(AnalysisStatus.Unreadable, ReasonCode.ErrNoLegible, "UNREADABLE", "ERR_NO_LEGIBLE")]
    public async Task AnalyzeAsync_ReturnsOk_ForFunctionalDecisions(
        AnalysisStatus status,
        ReasonCode reasonCode,
        string expectedStatus,
        string expectedReasonCode)
    {
        var result = await CreateController(new AnalysisDecision
        {
            Status = status,
            ReasonCode = reasonCode
        }).AnalyzeAsync(
            new AnalyzeTicketRequest { File = CreateFile([0xFF, 0xD8, 0xFF], "image/jpeg"), ExpenseType = ExpenseType.Meals },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AnalyzeTicketResponse>(okResult.Value);
        Assert.Equal(expectedStatus, response.Status);
        Assert.Equal(expectedReasonCode, response.ReasonCode);
        Assert.Equal("evidence", response.Verification.OcrRawText);
    }

    [Theory]
    [InlineData("ticket.jpg")]
    [InlineData("ticket.jpeg")]
    public async Task AnalyzeAsync_AcceptsJpegExtensionsWithMatchingSignature(string fileName)
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest
            {
                File = CreateFile([0xFF, 0xD8, 0xFF], "image/jpeg", fileName),
                ExpenseType = ExpenseType.Meals
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("image/jpg")]
    public async Task AnalyzeAsync_AcceptsJpegWithGenericOrLegacyContentType(string contentType)
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest
            {
                File = CreateFile([0xFF, 0xD8, 0xFF], contentType, "ticket.jpg"),
                ExpenseType = ExpenseType.Meals
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_AcceptsPngWithMatchingSignature()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest
            {
                File = CreateFile([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "image/png", "ticket.png"),
                ExpenseType = ExpenseType.Meals
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_AcceptsPngWithGenericContentType()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest
            {
                File = CreateFile(
                    [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                    "application/octet-stream",
                    "ticket.png"),
                ExpenseType = ExpenseType.Meals
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsBadRequest_WhenFileExtensionIsUnsupported()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest
            {
                File = CreateFile([0xFF, 0xD8, 0xFF], "image/jpeg", "ticket.gif"),
                ExpenseType = ExpenseType.Meals
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsBadRequest_WhenPngExtensionHasJpegSignature()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest
            {
                File = CreateFile(
                    [0xFF, 0xD8, 0xFF],
                    "image/jpeg",
                    "ticket.png"),
                ExpenseType = ExpenseType.Meals
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsBadRequest_WhenSignatureIsUnknown()
    {
        var result = await CreateController().AnalyzeAsync(
            new AnalyzeTicketRequest
            {
                File = CreateFile([0x00, 0x01, 0x02], "image/jpeg", "ticket.jpg"),
                ExpenseType = ExpenseType.Meals
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static TicketsController CreateController(AnalysisDecision? decision = null, long maxFileSizeBytes = UploadOptions.DefaultMaxFileSizeBytes) => new(
        new AnalyzeTicketHandler(
            new OrientationStub(),
            new OcrStub(),
            new ProductClassifierStub(),
            new ExpenseCoherenceAnalyzerStub(),
            new VisualAnalysisStub(),
            new VerificationStub(),
            new RuleEngineStub(decision ?? new AnalysisDecision { Status = AnalysisStatus.Approved, ReasonCode = ReasonCode.Ok }),
            new AuditLoggerStub()),
        Options.Create(new UploadOptions { MaxFileSizeBytes = maxFileSizeBytes }));

    private static IFormFile CreateFile(byte[] content, string contentType, string fileName = "ticket.jpg")
    {
        var file = new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary()
        };
        file.ContentType = contentType;
        return file;
    }

    private sealed class OrientationStub : IDocumentOrientationService
    {
        public Task<byte[]> OrientAsync(byte[] image, CancellationToken cancellationToken = default) => Task.FromResult(image);
    }

    private sealed class OcrStub : IOcrService
    {
        public Task<OcrResult> ReadAsync(byte[] image, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OcrResult { RawText = "evidence" });
    }

    private sealed class ProductClassifierStub : IProductClassifier
    {
        public Task<IReadOnlyList<ProductData>> ClassifyAsync(
            IReadOnlyList<ProductData> products,
            CancellationToken cancellationToken = default) => Task.FromResult(products);
    }

    private sealed class ExpenseCoherenceAnalyzerStub : IExpenseCoherenceAnalyzer
    {
        public Task<ExpenseCoherenceResult> AnalyzeAsync(
            TicketData ticket,
            ExpenseType expenseType,
            CancellationToken cancellationToken = default) => Task.FromResult(new ExpenseCoherenceResult());
    }

    private sealed class VisualAnalysisStub : IVisualAnalysisService
    {
        public Task<VisualAnalysisResult> AnalyzeAsync(byte[] image, CancellationToken cancellationToken = default) =>
            Task.FromResult(new VisualAnalysisResult());
    }

    private sealed class VerificationStub : ITicketVerificationService
    {
        public VerificationResult Verify(
            OcrResult ocrResult,
            VisualAnalysisResult visualAnalysis) => new();
    }

    private sealed class RuleEngineStub(AnalysisDecision decision) : IExpenseRuleEngine
    {
        public AnalysisDecision Evaluate(
            TicketData ticket,
            VerificationData verification,
            ExpenseType expenseType,
            ExpenseCoherenceResult coherence) => decision;
    }

    private sealed class AuditLoggerStub : IAuditLogger
    {
        public Task LogAsync(
            Guid analysisId,
            ExpenseType expenseType,
            AnalysisDecision? decision,
            TimeSpan duration,
            Exception? error,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
