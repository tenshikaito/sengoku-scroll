namespace SengokuScroll.Domain;

public readonly struct GameResult(bool isSuccess, GameError? error)
{
    public bool IsSuccess { get; } = isSuccess;

    public GameError? Error { get; } = error;

    public static GameResult Ok() => new(true, null);

    public static GameResult Fail(GameError error) => new(false, error);

    public static implicit operator bool(GameResult r) => r.IsSuccess;

    public static implicit operator GameResult(GameError error) => Fail(error);
}

public readonly struct GameResult<T>(bool isSuccess, GameError? error)
{
    private readonly T? value;

    public bool IsSuccess { get; } = isSuccess;

    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("No value on failure");

            return value!;
        }
    }

    public GameError? Error { get; } = error;

    private GameResult(T value) : this(true, null)
    {
        this.value = value;
    }

    private GameResult(GameError error) : this(false, error)
    {
        value = default!;
    }

    public static GameResult<T> Ok(T value) => new(value);

    public static GameResult<T> Fail(GameError error) => new(error);

    public static implicit operator bool(GameResult<T> r) => r.IsSuccess;

    public static implicit operator GameResult<T>(T value) => Ok(value);

    public static implicit operator GameResult<T>(GameError error) => Fail(error);
}