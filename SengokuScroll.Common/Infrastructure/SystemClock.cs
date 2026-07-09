using SengokuScroll.Common.Abstractions;

namespace SengokuScroll.Common.Infrastructure;

public sealed class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}