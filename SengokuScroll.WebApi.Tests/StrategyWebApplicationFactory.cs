using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SengokuScroll.WebApi.Tests;

/// <summary>策略 WebApi 测试用宿主工厂。</summary>
public sealed class StrategyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Development").ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Strategy:Multiplayer:PersistenceEnabled"] = "false" }));
}
