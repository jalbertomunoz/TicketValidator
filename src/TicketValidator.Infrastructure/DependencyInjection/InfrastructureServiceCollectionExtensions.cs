using Microsoft.Extensions.DependencyInjection;
using TicketValidator.Application.Abstractions;
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

        return services;
    }
}
