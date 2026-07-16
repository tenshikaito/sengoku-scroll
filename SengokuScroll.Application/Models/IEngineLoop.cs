namespace SengokuScroll.Application.Models;

/// <summary>引擎循环控制：启停与暂停（日/回合推进）。</summary>
public interface IEngineLoop
{
    void Start(bool isPause = false);

    void Pause();

    void Resume();

    Task StopAsync();
}
