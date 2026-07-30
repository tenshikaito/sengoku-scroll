using Microsoft.Extensions.FileProviders;

namespace SengokuScroll.WebApi;

/// <summary>发行版内嵌前端静态资源；Development 不托管（联调 Vite）。</summary>
internal static class ClientStaticWebHelper
{
    private const string EmbeddedBaseNamespace = "SengokuScroll.WebApi.wwwroot";

    internal static IFileProvider? ResolveClientFileProvider(IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return null;

        var embedded = new EmbeddedFileProvider(typeof(Program).Assembly, EmbeddedBaseNamespace);
        if (embedded.GetFileInfo("index.html").Exists)
            return embedded;

        var diskIndex = Path.Combine(environment.WebRootPath, "index.html");
        if (File.Exists(diskIndex))
            return environment.WebRootFileProvider;

        return null;
    }

    internal static void UseClientStaticWebIfAvailable(WebApplication app)
    {
        var fileProvider = ResolveClientFileProvider(app.Environment);
        if (fileProvider is null)
            return;

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
    }

    internal static void MapClientSpaFallbackIfAvailable(WebApplication app)
    {
        var fileProvider = ResolveClientFileProvider(app.Environment);
        if (fileProvider is null)
            return;

        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
    }
}
