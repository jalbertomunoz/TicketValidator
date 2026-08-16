using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Infrastructure.AI.Contracts;
using TicketValidator.Infrastructure.AI.Prompts;

namespace TicketValidator.Infrastructure.AI;

public sealed class OpenAiExpenseCoherenceAnalyzer : IExpenseCoherenceAnalyzer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ChatClient _client;
    private readonly float _temperature;

    public OpenAiExpenseCoherenceAnalyzer(ChatClient client, IOptions<OpenAiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _temperature = (float)options.Value.Temperature;
    }

    public async Task<ExpenseCoherenceResult> AnalyzeAsync(
        TicketData ticket,
        ExpenseType expenseType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        if (ticket.Products.Count == 0)
        {
            return new ExpenseCoherenceResult();
        }

        var options = new ChatCompletionOptions
        {
            Temperature = _temperature,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                ExpenseCoherenceSchema.Name,
                BinaryData.FromString(ExpenseCoherenceSchema.Json),
                jsonSchemaIsStrict: true)
        };
        var messages = new ChatMessage[]
        {
            new SystemChatMessage(ExpenseCoherencePrompt.SystemMessage),
            new UserChatMessage(ExpenseCoherencePrompt.CreateUserMessage(ticket, expenseType))
        };

        var completion = await _client.CompleteChatAsync(messages, options, cancellationToken);
        var responseText = completion.Value.Content.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("OpenAI returned an empty structured expense coherence response.");
        }

        var response = JsonSerializer.Deserialize<ExpenseCoherenceResponse>(responseText, JsonSerializerOptions)
            ?? throw new JsonException("OpenAI returned an invalid structured expense coherence response.");

        return ExpenseCoherenceMapper.Map(ticket, response);
    }
}
