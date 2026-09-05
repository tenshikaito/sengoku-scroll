namespace SengokuScroll.WebApi.Multiplayer;

public sealed class StrategyRoomMemoryMaintenance(StrategyMultiplayerRoomManager manager) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken)) manager.HibernateIdleRooms();
    }
}
