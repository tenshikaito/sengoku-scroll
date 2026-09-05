using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 开局/剧本加载时初始化情报相关运行时字段。
/// </summary>
/// <remarks>
/// 调用链：<c>StrategySimulationHost.LoadScenario</c> → BootstrapGameWorld。
/// 正式开局不注入虚构事件看法。
/// </remarks>
public static class IntelEntityBootstrapHelper
{
    /// <summary>
    /// 填充 ServiceDate、IntelTasks、亲属关系和势力/据点情报缓存。
    /// </summary>
    public static void BootstrapGameWorld(GameWorld world, StrategyScenarioMeta meta)
    {
        var gameData = world.GameData;
        var gameDate = gameData.GameDate;

        foreach (var character in gameData.Characters.Values)
        {
            if (character.ServiceDate.Year <= 0)
                character.ServiceDate = gameDate;

            if (character.IntelTasks.Count == 0)
                character.IntelTasks.AddRange(CharacterIntelTasksHelper.BuildStoredIntelTasks(
                    character,
                    gameData,
                    meta,
                    gameData.Strongholds));

            CharacterRelationshipBootstrapHelper.EnsureKinshipRelationships(character, gameData.Characters);
        }

        foreach (var force in gameData.Forces.Values)
        {
            force.LordCharacterId ??= StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
                force.Id,
                meta,
                gameData);
            if (force.LordCharacterId is int lordId
                && lordId > 0
                && gameData.Characters.TryGetValue(lordId, out var lord)
                && lord.Loyalty == 0)
            {
                lord.Loyalty = 100;
            }

            ForceIntelHelper.SyncMilitaryCaches(force, gameData);
        }

        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.Scale is < 1 or > 30)
                stronghold.Scale = ResolveDefaultScale(stronghold.Population);

            StrongholdMilitaryStatsHelper.Recalculate(stronghold, gameData);
            StrongholdMaintenanceHelper.Sync(stronghold, world.GameMasterData);
            TechnologyIntelHelper.SyncStrongholdTechnologiesFromAgriculture(stronghold);
        }

        // Live worlds must not invent event history. Demo views belong to the mock client only.
    }

    private static byte ResolveDefaultScale(int population)
    {
        if (population >= 50_000) return 24;
        if (population >= 30_000) return 18;
        if (population >= 15_000) return 12;
        return 8;
    }

}
