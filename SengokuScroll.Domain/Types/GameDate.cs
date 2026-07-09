namespace SengokuScroll.Domain.Types;

public readonly struct GameDate : IComparable<GameDate>
{
    public const int DayPhasePerDay = 4;
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;

    private readonly int totalPhases;

    public int TotalPhases => totalPhases;

    public int TotalDays => totalPhases / DayPhasePerDay;

    public int Year => TotalDays / (MonthsPerYear * DaysPerMonth) + 1;

    public int Month => TotalDays / DaysPerMonth % MonthsPerYear + 1;

    public int Day => TotalDays % DaysPerMonth + 1;

    public GameDate Date => new(Year, Month, Day);

    public GameDateDayPhase DayPhase => (GameDateDayPhase)(totalPhases % DayPhasePerDay);

    public GameDate(int year, int month, int day, int phase = 0)
        => totalPhases = (year - 1) * MonthsPerYear * DaysPerMonth * DayPhasePerDay + (month - 1) * DaysPerMonth * DayPhasePerDay + (day - 1) * DayPhasePerDay + phase;

    private GameDate(int totalPhases) => this.totalPhases = totalPhases;

    public static GameDate FromTotalPhases(int total) => new(total);

    public static GameDate FromTotalDays(int total) => new(total * DayPhasePerDay);

    public GameDate AddDayPhase(int value = 1) => new(totalPhases + value);

    public GameDate AddDays(int days = 1) => new(totalPhases + days * DayPhasePerDay);

    public GameDate AddMonths(int months = 1) => AddDays(months * DaysPerMonth);

    public GameDate AddYears(int years = 1) => AddMonths(years * MonthsPerYear);

    public int CompareTo(GameDate other) => totalPhases.CompareTo(other.totalPhases);

    public override bool Equals(object? obj) => obj is GameDate other && totalPhases == other.totalPhases;

    public override int GetHashCode() => totalPhases;

    public static bool operator ==(GameDate a, GameDate b) => a.totalPhases == b.totalPhases;

    public static bool operator !=(GameDate a, GameDate b) => !(a == b);

    public static bool operator >(GameDate a, GameDate b) => a.totalPhases > b.totalPhases;

    public static bool operator <(GameDate a, GameDate b) => a.totalPhases < b.totalPhases;
}
