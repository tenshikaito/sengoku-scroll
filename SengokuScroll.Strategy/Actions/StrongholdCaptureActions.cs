using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>据点归属变更与城防折损。</summary>
public static class StrongholdCaptureActions
{
    /// <summary>变更据点归属势力，同步各 Actor 势力 Id、折损城防并刷新双方势力库藏汇总。</summary>
    public static void TransferStrongholdOwnership(
        Stronghold stronghold,
        int newForceId,
        GameData gameData,
        GameMasterData gameMasterData,
        int siegeDamage = 15)
    {
        var oldForceId = stronghold.ForceId;
        stronghold.ForceId = newForceId;
        // 业务：易手后清除原领主任命，由新势力重新任命
        stronghold.LordId = 0;
        stronghold.ForceActor.ForceId = newForceId;
        stronghold.CivilianActor.ForceId = newForceId;

        // 商家/寺社店 Actor 仍归属各自组织势力，不随领内易手变更。

        StrongholdDefenseRules.ApplySiegeDamage(stronghold, siegeDamage);
        stronghold.Defense = (byte)Math.Min(
            byte.MaxValue,
            StrongholdDefenseRules.ResolveTotalDefense(stronghold, gameMasterData));

        if (gameData.Forces.TryGetValue(oldForceId, out var oldForce))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(oldForce, gameData);

        if (gameData.Forces.TryGetValue(newForceId, out var newForce))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(newForce, gameData);
    }
}
