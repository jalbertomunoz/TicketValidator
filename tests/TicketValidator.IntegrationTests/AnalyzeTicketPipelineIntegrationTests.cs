using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TicketValidator.Application.Services;
using TicketValidator.Application.UseCases.AnalyzeTicket;
using TicketValidator.Domain.Enums;
using TicketValidator.Infrastructure.Logging;

namespace TicketValidator.IntegrationTests;

public sealed class AnalyzeTicketPipelineIntegrationTests
{
    [Fact]
    public async Task HandleAsync_ApprovedPipeline_WritesAuditLog()
    {
        var directoryPath = CreateTemporaryDirectoryPath();
        try
        {
            var handler = CreateHandler(PipelineScenarios.Approved(), directoryPath);

            var result = await handler.HandleAsync(new AnalyzeTicketCommand([0x89, 0x50, 0x4E, 0x47], ExpenseType.Meals));

            Assert.NotEqual(Guid.Empty, result.AnalysisId);
            Assert.Equal(AnalysisStatus.Approved, result.Decision.Status);
            Assert.Equal(ReasonCode.Ok, result.Decision.ReasonCode);

            var line = Assert.Single(await File.ReadAllLinesAsync(GetLogFilePath(directoryPath)));
            Assert.Contains($"AnalysisId={result.AnalysisId}", line);
            Assert.Contains("ExpenseType=Meals", line);
            Assert.Contains("Status=APPROVED", line);
            Assert.Contains("ReasonCode=OK", line);
            Assert.Contains("DurationMs=", line);
        }
        finally
        {
            DeleteTemporaryDirectory(directoryPath);
        }
    }

    [Fact]
    public async Task HandleAsync_OcrFailure_RethrowsAndWritesProcessingErrorAuditLog()
    {
        var directoryPath = CreateTemporaryDirectoryPath();
        try
        {
            var handler = CreateHandler(PipelineScenarios.OcrFailure(), directoryPath);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.HandleAsync(new AnalyzeTicketCommand([0x89, 0x50, 0x4E, 0x47], ExpenseType.Meals)));

            var line = Assert.Single(await File.ReadAllLinesAsync(GetLogFilePath(directoryPath)));
            Assert.Contains("Status=PROCESSING_ERROR", line);
            Assert.Contains("ReasonCode= |", line);
            Assert.Contains("ErrorType=InvalidOperationException", line);
        }
        finally
        {
            DeleteTemporaryDirectory(directoryPath);
        }
    }

    private static AnalyzeTicketHandler CreateHandler(PipelineScenario scenario, string directoryPath) => new(
        new ScenarioDocumentOrientationService(),
        new ScenarioOcrService(scenario),
        new ScenarioProductClassifier(),
        new ScenarioExpenseCoherenceAnalyzer(scenario),
        new ScenarioVisualAnalysisService(scenario),
        new TicketVerificationService(),
        new ExpenseRuleEngine(),
        new FileAuditLogger(
            Options.Create(new AuditLogOptions { DirectoryPath = directoryPath, FileName = "audit.log" }),
            NullLogger<FileAuditLogger>.Instance));

    private static string GetLogFilePath(string directoryPath) => Path.Combine(directoryPath, "audit.log");

    private static string CreateTemporaryDirectoryPath() => Path.Combine(
        Path.GetTempPath(),
        "TicketValidatorIntegrationTests",
        Guid.NewGuid().ToString("N"));

    private static void DeleteTemporaryDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
