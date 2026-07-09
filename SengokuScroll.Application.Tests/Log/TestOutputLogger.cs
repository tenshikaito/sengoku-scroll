using Microsoft.Extensions.Logging;

namespace SengokuScroll.Application.Tests.Log;

public class TestOutputLogger(ITestOutputHelper output, string category) : ILogger
{
    private readonly ITestOutputHelper _output = output;
    private readonly string _category = category;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine($"[{logLevel}] {_category}: {formatter(state, exception)}");
    }
}