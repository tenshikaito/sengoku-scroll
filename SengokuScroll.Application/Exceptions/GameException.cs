namespace SengokuScroll.Application.Exceptions;

public class GameException : Exception
{
    public GameException()
    {
    }

    public GameException(string? message) : base(message)
    {
    }
}
