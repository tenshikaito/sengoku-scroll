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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
    options.AddPolicy("default", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.Configure<StrategyDayDebugOptions>(builder.Configuration.GetSection("Strategy:DayDebug"));
builder.Services.AddStrategySimulationHost();
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var directory = Path.Combine(env.ContentRootPath, "App_Data", "strategy-saves");
    return new StrategySaveSlotRepository(directory);
});

var app = builder.Build();

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
