using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TicketValidator.Application.Abstractions;
using TicketValidator.Domain.Models;
using TicketValidator.Infrastructure.AI.Contracts;
using TicketValidator.Infrastructure.AI.Prompts;

namespace TicketValidator.Infrastructure.AI;

public sealed class OpenAiProductClassifier : IProductClassifier
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ChatClient _client;
    private readonly float _temperature;

    public OpenAiProductClassifier(ChatClient client, IOptions<OpenAiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _temperature = (float)options.Value.Temperature;
    }

    public async Task<IReadOnlyList<ProductData>> ClassifyAsync(
        IReadOnlyList<ProductData> products,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(products);

        if (products.Count == 0)
        {
            return [];
        }

        if (products.All(product => string.IsNullOrWhiteSpace(product.Concept)))
        {
            return ProductClassificationMapper.Map(products, new ProductClassificationResponse());
        }

        var options = new ChatCompletionOptions
        {
            Temperature = _temperature,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                ProductClassificationSchema.Name,
                BinaryData.FromString(ProductClassificationSchema.Json),
                jsonSchemaIsStrict: true)
        };
        var messages = new ChatMessage[]
        {
            new SystemChatMessage(ProductClassificationPrompt.SystemMessage),
            new UserChatMessage(ProductClassificationPrompt.CreateUserMessage(products))
        };

        var completion = await _client.CompleteChatAsync(messages, options, cancellationToken);
        var responseText = completion.Value.Content.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("OpenAI returned an empty structured product classification response.");
        }

        var response = JsonSerializer.Deserialize<ProductClassificationResponse>(responseText, JsonSerializerOptions)
            ?? throw new JsonException("OpenAI returned an invalid structured product classification response.");

        return ProductClassificationMapper.Map(products, response);
    }
}
