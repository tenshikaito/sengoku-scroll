namespace SengokuScroll.Application.Tests.Log;

using Microsoft.Extensions.Logging;

public class TestOutputLoggerProvider(ITestOutputHelper output) : ILoggerProvider
{
    private readonly ITestOutputHelper _output = output;

    public ILogger CreateLogger(string categoryName) => new TestOutputLogger(_output, categoryName);

    public void Dispose() => GC.SuppressFinalize(this);
}