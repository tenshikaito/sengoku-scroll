using SengokuScroll.Domain.Definitions;

namespace SengokuScroll.Strategy.Constants;

/// <summary>
/// 策略模式兵种类型 Id 与显示名；优先读 <see cref="GameMasterData.UnitTypes"/>，
/// 本类仅作内置 fallback。
/// </summary>
public static class StrategyTroopTypes
{
    public const byte Ashigaru = 1;
    public const byte Archer = 2;
    public const byte Cavalry = 3;
    public const byte Matchlock = 4;

    public static string ResolveName(int typeId, string? typeName)
        => ResolveName(typeId, typeName, unitTypes: null);

    public static string ResolveName(
        int typeId,
        string? typeName,
        IReadOnlyDictionary<int, UnitTypeDefinition>? unitTypes)
    {
        if (!string.IsNullOrWhiteSpace(typeName))
            return typeName.Trim();

        if (unitTypes is not null && unitTypes.TryGetValue(typeId, out var definition))
            return definition.Name;

        return typeId switch
        {
            Ashigaru => "足轻",
            Archer => "弓兵",
            Cavalry => "骑兵",
            Matchlock => "铁炮",
            _ => $"兵种#{typeId}"
        };
    }
}
