namespace SengokuScroll.Domain;

public class GameRuleConfig
{
    /// <summary>进入非己方据点格附加 AP（叠在地形成本上；默认 +1，避免无法入城）。</summary>
    public int EnterStrongholdAp { get; set; } = 1;

    public int AttackAp { get; internal set; } = 5;

    /// <summary>下达攻城指令（强攻/包围）消耗的 AP。</summary>
    public int SiegeOrderAp { get; set; } = 5;

    /// <summary>日初恢复的 AP（与 <see cref="MilitaryMaxMovement"/> 配合，约 2 日 3 格）。</summary>
    public int NextTurnApRecovery { get; set; } = 1;

    /// <summary>军事单位移动力上限（剧本加载与 AP 恢复均以此封顶）。</summary>
    public int MilitaryMaxMovement { get; set; } = 5;

    /// <summary>军事单位单日最多移动的地图格数（道路/AP 富余时仍不可超过）。</summary>
    public int MaxTilesMovedPerDay { get; set; } = 2;
}
