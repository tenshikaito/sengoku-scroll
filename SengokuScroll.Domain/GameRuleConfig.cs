namespace SengokuScroll.Domain;

public class GameRuleConfig
{
    public int EnterStrongholdAp { get; set; } = 5;

    public int AttackAp { get; internal set; } = 5;

    public int NextTurnApRecovery { get; set; } = 3;
}
