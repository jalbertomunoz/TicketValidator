using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI.Chat;
using TicketValidator.Application.Abstractions;

namespace TicketValidator.IntegrationTests;

public sealed class TicketAnalysisApiIntegrationTests
{
    public static TheoryData<PipelineScenario, string, string> FunctionalCases => new()
    {
        { PipelineScenarios.Approved(), "APPROVED", "OK" },
        { PipelineScenarios.Alcohol(), "REJECTED", "ERR_BEBIDA_ALCOHOLICA" },
        { PipelineScenarios.DateMismatch(), "REVIEW_REQUIRED", "DATE_MISMATCH" },
        { PipelineScenarios.Unreadable(), "UNREADABLE", "ERR_NO_LEGIBLE" },
        { PipelineScenarios.NotDocument(), "REJECTED", "ERR_NO_DOCUMENTO" },
        { PipelineScenarios.DocumentTypeMismatch(), "REJECTED", "ERR_NO_DOCUMENTO" },
        { PipelineScenarios.IncoherentExpense(), "REJECTED", "ERR_TIPO_GASTO_INCOHERENTE" }
    };

    [Theory]
    [MemberData(nameof(FunctionalCases))]
    public async Task AnalyzeAsync_ReturnsFunctionalDecisionFromCompleteFakePipeline(
        PipelineScenario scenario,
        string expectedStatus,
        string expectedReasonCode)
    {
        using var factory = new PipelineWebApplicationFactory(scenario);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/v1/tickets/analyze", CreateAnalyzeRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedStatus, document.RootElement.GetProperty("status").GetString());
        Assert.Equal(expectedReasonCode, document.RootElement.GetProperty("reasonCode").GetString());
    }

    [Fact]
    public async Task AnalyzeAsync_WhenOcrFails_ReturnsInternalServerError()
    {
        using var factory = new PipelineWebApplicationFactory(PipelineScenarios.OcrFailure());
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/v1/tickets/analyze", CreateAnalyzeRequest());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private static MultipartFormDataContent CreateAnalyzeRequest()
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL1ZQAAAABJRU5ErkJggg=="));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "ticket.png");
        content.Add(new StringContent("Meals"), "expenseType");
        return content;
    }

    private sealed class PipelineWebApplicationFactory(PipelineScenario scenario) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ChatClient>();
                services.RemoveAll<IDocumentOrientationService>();
                services.RemoveAll<IOcrService>();
                services.RemoveAll<IProductClassifier>();
                services.RemoveAll<IExpenseCoherenceAnalyzer>();
                services.RemoveAll<IVisualAnalysisService>();
                services.RemoveAll<IAuditLogger>();

                services.AddSingleton(scenario);
                services.AddSingleton<IDocumentOrientationService, ScenarioDocumentOrientationService>();
                services.AddSingleton<IOcrService, ScenarioOcrService>();
                services.AddSingleton<IProductClassifier, ScenarioProductClassifier>();
                services.AddSingleton<IExpenseCoherenceAnalyzer, ScenarioExpenseCoherenceAnalyzer>();
                services.AddSingleton<IVisualAnalysisService, ScenarioVisualAnalysisService>();
                services.AddSingleton<IAuditLogger, TestAuditLogger>();
            });
        }
    }
}
