namespace SengokuScroll.Application.Models;

public interface IEngineLoop
{
    void Start(bool isPause = false);

    void Pause();

    void Resume();

    Task StopAsync();
}
