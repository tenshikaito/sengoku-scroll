namespace SengokuScroll.Application.Exceptions;

public class CharacterNotFoundException : GameException
{
    public CharacterNotFoundException(int? id) : this($"Character Not Found: id={id}")
    {
    }

    public CharacterNotFoundException(string? message) : base(message)
    {
    }
}
