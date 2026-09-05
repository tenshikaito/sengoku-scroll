using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using SengokuScroll.Localization;
using SengokuScroll.Localization.Abstractions;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Extensions;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.WebApi;
using SengokuScroll.WebApi.Multiplayer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
var browserOrigins = (builder.Configuration.GetSection("Strategy:AllowedBrowserOrigins").Get<string[]>() ?? [])
    .Concat(builder.Environment.IsDevelopment()
        ? ["http://localhost:5173", "http://127.0.0.1:5173", "http://[::1]:5173"]
        : Array.Empty<string>())
    .Select(origin => BrowserOriginPolicy.NormalizeOrigin(origin)
        ?? throw new InvalidOperationException("Strategy:AllowedBrowserOrigins requires explicit HTTP(S) origins."))
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
builder.Services.AddCors(options =>
    options.AddPolicy("default", policy =>
        policy.SetIsOriginAllowed(origin =>
            BrowserOriginPolicy.NormalizeOrigin(origin) is { } normalized && browserOrigins.Contains(normalized))
            .AllowAnyHeader().AllowAnyMethod()));

builder.Services.Configure<StrategyDayDebugOptions>(builder.Configuration.GetSection("Strategy:DayDebug"));
builder.Services.Configure<StrategyAiTraceOptions>(builder.Configuration.GetSection("Strategy:AiTrace"));
builder.Services.AddStrategySimulationHost();
builder.Services.AddOptions<StrategyMultiplayerOptions>()
    .Bind(builder.Configuration.GetSection("Strategy:Multiplayer"))
    .Validate(options => options.ConnectionLeaseSeconds is >= 15 and <= 3600,
        "Strategy:Multiplayer:ConnectionLeaseSeconds must be between 15 and 3600.")
    .ValidateOnStart();
builder.Services.AddSingleton<StrategyMultiplayerRoomManager>();
builder.Services.AddHostedService<StrategyRoomMemoryMaintenance>();
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var directory = Path.Combine(env.ContentRootPath, "App_Data", "strategy-saves");
    return new StrategySaveSlotRepository(directory);
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    try { await next(); }
    catch (StrategyMultiplayerException ex) when (ex.Code == "RoomStorageFailed" && !context.Response.HasStarted)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new SengokuScroll.WebApi.Models.ApiErrorResponse(ex.Code));
    }
});

// CORS headers alone do not prevent simple cross-origin requests from executing.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if ((path.StartsWithSegments("/api") || path.StartsWithSegments("/strategy")
            || path.StartsWithSegments("/hubs"))
        && !BrowserOriginPolicy.Allows(context.Request, browserOrigins))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new SengokuScroll.WebApi.Models.ApiErrorResponse("UntrustedBrowserOrigin"));
        return;
    }
    await next();
});

app.Use(async (context, next) =>
{
    var cultureContext = context.RequestServices.GetRequiredService<ICultureContext>();
    var header = context.Request.Headers.AcceptLanguage.FirstOrDefault();
    var cultureName = header?.Split(',').FirstOrDefault()?.Split(';').FirstOrDefault()?.Trim();
    cultureContext.UseCulture(cultureName);
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("default");
app.UseAuthorization();

ClientStaticWebHelper.UseClientStaticWebIfAvailable(app);

app.MapControllers();
app.MapHub<StrategyRoomHub>("/hubs/strategy");

ClientStaticWebHelper.MapClientSpaFallbackIfAvailable(app);

var defaultScenarioId = app.Configuration["Strategy:DefaultScenarioId"] ?? "mini_kanto";
var simulationHost = app.Services.GetRequiredService<StrategySimulationHost>();
var bootstrap = simulationHost.LoadScenario(defaultScenarioId);
if (!bootstrap.IsSuccess)
{
    app.Logger.LogWarning(
        "Strategy simulation bootstrap failed for scenario {ScenarioId}: {Error}",
        defaultScenarioId,
        bootstrap.Error?.Code ?? "Unknown");
}
else
{
    app.Logger.LogInformation(
        "Strategy simulation loaded scenario {ScenarioId} on startup",
        defaultScenarioId);
}

if (app.Configuration.GetValue("Strategy:OpenBrowserOnStart", app.Environment.IsProduction()))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()
                ?.Addresses;
            var url = addresses?.FirstOrDefault(a =>
                          a.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                      ?? "http://127.0.0.1:5100";
            url = url
                .Replace("0.0.0.0", "127.0.0.1", StringComparison.Ordinal)
                .Replace("[::]", "127.0.0.1", StringComparison.Ordinal);
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to open browser on startup");
        }
    });
}

app.Run();

/// <summary>供 WebApplicationFactory 集成测试引用。</summary>
public partial class Program;
