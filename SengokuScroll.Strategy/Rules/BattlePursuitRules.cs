using static SengokuScroll.Domain.Definitions.CharacterDefinition;

namespace SengokuScroll.Strategy.Rules;

/// <summary>根据指挥官性格判定胜方是否追击（AI 自动；玩家部队仅改方针不自动再攻）。</summary>
public static class BattlePursuitRules
{
    /// <summary>AI 胜方是否应继续压迫（占领/追击）。</summary>
    public static bool ShouldAiPursue(PersonalityData? personality)
    {
        if (personality is null)
            return false;

        // 业务：勇武极高（≥75）几乎必追击
        if (personality.Courage >= 75)
            return true;

        // 业务：勇武极低（≤35）倾向收兵
        if (personality.Courage <= 35)
            return false;

        // 业务：行动性极高（≥78）的谨慎型将领不冒进追击
        if (personality.Action >= 78)
            return false;

        // 业务：高野心+低行动性——政治型扩张者仍愿追击
        if (personality.Ambition >= 65 && personality.Action <= 55)
            return true;

        // 业务：中等勇武与野心组合时适度追击
        return personality.Courage >= 60 && personality.Ambition >= 50;
    }
}
