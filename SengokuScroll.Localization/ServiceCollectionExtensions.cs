using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Localization.Abstractions;

namespace SengokuScroll.Localization;

/// <summary>多语言 DI 注册。</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>注册文化上下文、JSON 译文表与 <see cref="ITextLocalizer"/>。</summary>
    public static IServiceCollection AddSengokuLocalization(this IServiceCollection services)
    {
        services.AddSingleton<ICultureContext, CultureContext>();

        services.AddSingleton<IReadOnlyList<ITextCatalog>>(_ =>
        {
            var assembly = typeof(ServiceCollectionExtensions).Assembly;
            return
            [
                JsonTextCatalog.LoadEmbedded(assembly, TextLocalizer.DefaultCulture),
                JsonTextCatalog.LoadEmbedded(assembly, TextLocalizer.FallbackCulture)
            ];
        });

        services.AddSingleton<ITextLocalizer, TextLocalizer>();
        return services;
    }

    /// <summary>从请求头或配置设置文化（WebApi 中间件可调用）。</summary>
    public static void UseCulture(this ICultureContext cultureContext, string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return;

        try
        {
            cultureContext.SetCulture(cultureName);
        }
        catch (CultureNotFoundException)
        {
            cultureContext.SetCulture(TextLocalizer.DefaultCulture);
        }
    }
}
