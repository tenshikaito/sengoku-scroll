namespace SengokuScroll.Common.Models;

public class GameModelBase
{
    public static implicit operator bool(GameModelBase? obj) => obj is not null;
}
