namespace SengokuScroll.Common.Abstractions;

public interface ISystemClock
{
    public DateTime UtcNow { get; }
}
