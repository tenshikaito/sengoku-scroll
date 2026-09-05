using System.Net;
using Microsoft.AspNetCore.Http;

namespace SengokuScroll.WebApi.Tests;

public sealed class BrowserOriginPolicyTests : IClassFixture<StrategyWebApplicationFactory>
{
    private readonly HttpClient client;

    public BrowserOriginPolicyTests(StrategyWebApplicationFactory factory)
        => client = factory.CreateClient();

    [Theory]
    [InlineData("https://untrusted.example", "POST", "/api/strategy/advance-day")]
    [InlineData("https://untrusted.example", "GET", "/strategy/state")]
    [InlineData("null", "POST", "/api/multiplayer/rooms")]
    [InlineData("https://untrusted.example", "GET", "/hubs/strategy")]
    public async Task UntrustedBrowser_CannotReadOrMutateGame(string origin, string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Add("Origin", origin);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("UntrustedBrowserOrigin", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:5173")]
    public async Task SameOriginAndDevelopmentFrontend_ContinueToWork(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/strategy/state");
        request.Headers.Add("Origin", origin);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("http://localhost.evil.example")]
    [InlineData("http://localhost:5100@evil.example")]
    [InlineData("http://localhost:5100/path")]
    [InlineData("http://localhost:5100?query")]
    public void MalformedOrLookalikeOrigins_AreNotTrusted(string origin)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost", 5100);
        context.Request.Headers.Origin = origin;
        Assert.False(BrowserOriginPolicy.Allows(context.Request, new HashSet<string>()));
    }
}
