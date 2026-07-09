using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SengokuScroll.Host.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleLog(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        return services;
    }
}
