using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Infrastructure.AI.Contracts;
using TicketValidator.Infrastructure.AI.Prompts;

namespace TicketValidator.Infrastructure.AI;

public sealed class OpenAiTicketExtractor : IAiTicketExtractor
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ChatClient _client;
    private readonly float _temperature;

    public OpenAiTicketExtractor(ChatClient client, IOptions<OpenAiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _temperature = (float)options.Value.Temperature;
    }

    public async Task<AiTicketExtraction> ExtractAsync(
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        ValidateOcrText(ocrText);

        var options = new ChatCompletionOptions
        {
            Temperature = _temperature,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                TicketExtractionSchema.Name,
                BinaryData.FromString(TicketExtractionSchema.Json),
                jsonSchemaIsStrict: true)
        };
        var messages = new ChatMessage[]
        {
            new SystemChatMessage(TicketExtractionPrompt.SystemMessage),
            new UserChatMessage(ocrText)
        };

        var completion = await _client.CompleteChatAsync(messages, options, cancellationToken);
        var responseText = completion.Value.Content.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("OpenAI returned an empty structured response.");
        }

        var response = JsonSerializer.Deserialize<TicketExtractionResponse>(responseText, JsonSerializerOptions)
            ?? throw new JsonException("OpenAI returned an invalid structured response.");

        return TicketExtractionMapper.Map(response);
    }

    internal static void ValidateOcrText(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            throw new ArgumentException("OCR text cannot be empty.", nameof(ocrText));
        }
    }
}
