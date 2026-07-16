namespace SengokuScroll.Localization;

/// <summary>本地化 key 常量（按域分组，避免魔法字符串）。</summary>
public static class LocalizationKeys
{
    public static class Battle
    {
        public const string EngagementField = "battle.engagement.field";
        public const string EngagementAmbush = "battle.engagement.ambush";
        public const string EngagementSiege = "battle.engagement.siege";
    }

    public static class Debug
    {
        public const string DayBegin = "debug.day.begin";
        public const string DayEnd = "debug.day.end";
        public const string SystemStart = "debug.system.start";
        public const string SystemEnd = "debug.system.end";
        public const string AiDirective = "debug.ai.directive";
        public const string AiAction = "debug.ai.action";
        public const string AiSkip = "debug.ai.skip";
        public const string MoveStep = "debug.move.step";
        public const string MoveSkip = "debug.move.skip";
        public const string EngagementQueue = "debug.engagement.queue";
        public const string BattleResolve = "debug.battle.resolve";
        public const string BattleStandoff = "debug.battle.standoff";
        public const string BattleOutcomeSurrender = "debug.battle.outcome.surrender";
        public const string BattleOutcomeAttackerWin = "debug.battle.outcome.attacker_win";
        public const string BattleOutcomeDefenderWin = "debug.battle.outcome.defender_win";
        public const string GarrisonDefense = "debug.garrison.defense";
    }
}
