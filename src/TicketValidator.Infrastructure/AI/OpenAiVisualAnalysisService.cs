using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Infrastructure.AI.Contracts;
using TicketValidator.Infrastructure.AI.Prompts;

namespace TicketValidator.Infrastructure.AI;

public sealed class OpenAiVisualAnalysisService : IVisualAnalysisService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ChatClient _client;
    private readonly float _temperature;

    public OpenAiVisualAnalysisService(ChatClient client, IOptions<OpenAiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _temperature = (float)options.Value.Temperature;
    }

    public async Task<VisualAnalysisResult> AnalyzeAsync(
        byte[] image,
        CancellationToken cancellationToken = default)
    {
        var mediaType = GetImageMediaType(image);
        var options = new ChatCompletionOptions
        {
            Temperature = _temperature,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                VisualAnalysisSchema.Name,
                BinaryData.FromString(VisualAnalysisSchema.Json),
                jsonSchemaIsStrict: true)
        };
        var messages = new ChatMessage[]
        {
            new SystemChatMessage(VisualAnalysisPrompt.SystemMessage),
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(VisualAnalysisPrompt.UserMessage),
                ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(image), mediaType))
        };

        var completion = await _client.CompleteChatAsync(messages, options, cancellationToken);
        var responseText = completion.Value.Content.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("OpenAI returned an empty structured visual analysis response.");
        }

        var response = JsonSerializer.Deserialize<VisualAnalysisResponse>(responseText, JsonSerializerOptions)
            ?? throw new JsonException("OpenAI returned an invalid structured visual analysis response.");

        return VisualAnalysisMapper.Map(response);
    }

    internal static string GetImageMediaType(byte[] image)
    {
        if (image is null || image.Length == 0)
        {
            throw new ArgumentException("Image cannot be empty.", nameof(image));
        }

        if (IsPng(image))
        {
            ValidatePng(image);
            return "image/png";
        }

        if (IsJpeg(image))
        {
            ValidateJpeg(image);
            return "image/jpeg";
        }

        throw new ArgumentException("Only JPEG and PNG images are supported.", nameof(image));
    }

    private static bool IsPng(byte[] image) => image.Length >= 8
        && image[0] == 0x89
        && image[1] == 0x50
        && image[2] == 0x4E
        && image[3] == 0x47
        && image[4] == 0x0D
        && image[5] == 0x0A
        && image[6] == 0x1A
        && image[7] == 0x0A;

    private static bool IsJpeg(byte[] image) => image.Length >= 3
        && image[0] == 0xFF
        && image[1] == 0xD8
        && image[2] == 0xFF;

    private static void ValidatePng(byte[] image)
    {
        if (image.Length < 45
            || image[12] != 0x49
            || image[13] != 0x48
            || image[14] != 0x44
            || image[15] != 0x52
            || image[^8] != 0x49
            || image[^7] != 0x45
            || image[^6] != 0x4E
            || image[^5] != 0x44)
        {
            throw new InvalidOperationException("The PNG image data is corrupt.");
        }
    }

    private static void ValidateJpeg(byte[] image)
    {
        if (image.Length < 4 || image[^2] != 0xFF || image[^1] != 0xD9)
        {
            throw new InvalidOperationException("The JPEG image data is corrupt.");
        }
    }
}
