using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TicketValidator.Application.Abstractions;
using TicketValidator.Infrastructure.AI;
using TicketValidator.Infrastructure.OCR;

namespace TicketValidator.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<TesseractOcrOptions>? configureTesseract = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new TesseractOcrOptions();
        configureTesseract?.Invoke(options);

        services.AddSingleton(options);
        services.AddTransient<IOcrService, TesseractOcrService>();
        services.AddSingleton<ChatClient>(serviceProvider =>
        {
            var openAiOptions = serviceProvider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            if (string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured.");
            }

            return new ChatClient(openAiOptions.Model, openAiOptions.ApiKey);
        });
        services.AddTransient<IAiTicketExtractor, OpenAiTicketExtractor>();

        return services;
    }
}
