using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Rules;

/// <summary>当主出入据点：AP 消耗与封锁/包围下强行突围风险。</summary>
public static class CharacterStrongholdGateRules
{
    /// <summary>据点是否处于封锁（含充分包围、敌占城格等）。</summary>
    public static bool IsGateBlocked(Stronghold stronghold, GameData gameData)
        => GarrisonBehaviorRules.IsStrongholdBlockaded(stronghold, gameData);

    public static GameResult TryPayGateAp(Character character, int apCost)
    {
        if (apCost <= 0)
            return GameResult.Ok();

        if (character.Ap < apCost)
            return GameError.ApNotEnough;

        character.Ap -= apCost;
        return GameResult.Ok();
    }

    /// <summary>强行出入被围据点：小概率被俘，否则可能负伤。</summary>
    public static string? ApplyForcedGateRisk(Character character, Stronghold stronghold, GameData gameData, int seed)
    {
        if (character.IsDead || character.ForceStatus == CharacterForceStatus.Prisoner)
            return null;

        var roll = Math.Abs(HashCode.Combine(seed, character.Id, stronghold.Id, 7919)) % 100;
        if (roll < 12)
        {
            character.ForceStatus = CharacterForceStatus.Prisoner;
            character.ActionStatus = CharacterActionStatus.Waiting;
            character.ActionTarget.RoutePoints.Clear();
            return $"{character.Name} 强行出入 {stronghold.Name} 时被俘";
        }

        if (roll < 40)
        {
            character.IsSick = true;
            character.Hp = Math.Max(1, character.Hp - 25);
            return $"{character.Name} 强行出入 {stronghold.Name} 时负伤";
        }

        return null;
    }
}
