using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TicketValidator.IntegrationTests;

public sealed class SwaggerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task WebDemoRoot_ReturnsHtml()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("TicketValidator", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Arrastra aquí tu ticket", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Código de motivo", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<option value=\"Lunch\" selected>Comida</option>", content, StringComparison.Ordinal);
        Assert.Contains("js/app.js?v=es-2", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WebDemoScript_ContainsSpanishDynamicLabels()
    {
        var response = await _client.GetAsync("/js/app.js?v=es-2");
        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Tipo de documento", content, StringComparison.Ordinal);
        Assert.Contains("OCR legible", content, StringComparison.Ordinal);
        Assert.Contains("REVISIÓN REQUERIDA", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Document type", content, StringComparison.Ordinal);
        Assert.DoesNotContain("OCR readable", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SwaggerUi_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/swagger/index.html");
        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Swagger UI", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesTicketAnalysisMultipartEndpoint()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var post = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/tickets/analyze")
            .GetProperty("post");

        var requestSchema = ResolveSchema(
            document.RootElement,
            post.GetProperty("requestBody").GetProperty("content").GetProperty("multipart/form-data").GetProperty("schema"));
        Assert.True(requestSchema.TryGetProperty("properties", out var properties), requestSchema.GetRawText());
        Assert.True(properties.TryGetProperty("file", out var fileProperty), properties.GetRawText());
        Assert.True(properties.TryGetProperty("expenseType", out var expenseTypeProperty), properties.GetRawText());
        var fileSchema = ResolveSchema(document.RootElement, fileProperty);
        var expenseTypeSchema = ResolveSchema(document.RootElement, expenseTypeProperty);

        Assert.Equal("string", fileSchema.GetProperty("type").GetString());
        Assert.Equal("binary", fileSchema.GetProperty("format").GetString());
        Assert.Contains(expenseTypeSchema.GetProperty("enum").EnumerateArray(), value => value.GetString() == "Meals");
        var responses = post.GetProperty("responses");
        var successSchema = ResolveSchema(
            document.RootElement,
            responses.GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));

        Assert.True(successSchema.TryGetProperty("properties", out var successProperties));
        Assert.True(successProperties.TryGetProperty("analysisId", out _));
        Assert.True(successProperties.TryGetProperty("status", out _));
        Assert.True(successProperties.TryGetProperty("reasonCode", out _));
        Assert.True(successProperties.TryGetProperty("message", out _));
        Assert.True(successProperties.TryGetProperty("ticket", out _));
        Assert.True(successProperties.TryGetProperty("verification", out _));
        Assert.True(post.GetProperty("responses").TryGetProperty("400", out _));
        Assert.True(post.GetProperty("responses").TryGetProperty("500", out _));
    }

    private static JsonElement ResolveSchema(JsonElement document, JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference))
        {
            return schema;
        }

        var schemaName = reference.GetString()!.Split('/').Last();
        return document.GetProperty("components").GetProperty("schemas").GetProperty(schemaName);
    }
}
