namespace SengokuScroll.Strategy.Constants;

/// <summary>
/// 策略模式兵种类型 Id 与显示名（M3-b 最小集）。
/// TODO(M4+)：迁入剧本 JSON / <see cref="Domain.GameMasterData.UnitTypes"/>，供玩家自定义世界兵种表；
/// 本类届时仅作内置 fallback。
/// </summary>
public static class StrategyTroopTypes
{
    public const byte Ashigaru = 1;
    public const byte Archer = 2;
    public const byte Cavalry = 3;
    public const byte Matchlock = 4;

    public static string ResolveName(int typeId, string? typeName)
    {
        if (!string.IsNullOrWhiteSpace(typeName))
            return typeName.Trim();

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
