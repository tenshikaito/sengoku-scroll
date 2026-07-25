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
/// 演示 seed（SeedDemo*Views）待事件/AI 系统接管后移除。
/// </remarks>
public static class IntelEntityBootstrapHelper
{
    /// <summary>
    /// 填充 ServiceDate、IntelTasks、亲属关系、势力/据点情报缓存、演示看法 seed。
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

        SeedDemoDiplomacyViews(gameData, meta);
        SeedDemoCharacterViews(gameData, meta);
    }

    private static byte ResolveDefaultScale(int population)
    {
        if (population >= 50_000) return 24;
        if (population >= 30_000) return 18;
        if (population >= 15_000) return 12;
        return 8;
    }

    /// <summary>演示：玩家势力与今川之间的外交看法（桶狭间、世仇等）。</summary>
    private static void SeedDemoDiplomacyViews(GameData gameData, StrategyScenarioMeta meta)
    {
        if (!gameData.Forces.TryGetValue(meta.PlayerForceId, out var playerForce))
            return;

        EnsureDiplomacyView(
            playerForce,
            targetForceId: 2,
            new EntityEffect
            {
                Id = 1,
                Name = "桶狭间之战",
                TargetStat = EffectTargetStat.Diplomacy,
                Magnitude = -35,
                Duration = EffectDurationKind.LongTerm,
                Description = "今川义元于桶狭间败死，双方怨怼深重。",
            });

        if (gameData.Forces.TryGetValue(2, out var targetForce))
        {
            EnsureDiplomacyView(
                targetForce,
                meta.PlayerForceId,
                new EntityEffect
                {
                    Id = 1,
                    Name = "杀害本家当主",
                    TargetStat = EffectTargetStat.Diplomacy,
                    Magnitude = -100,
                    Duration = EffectDurationKind.Permanent,
                    Description = "杀害本家当主是世仇，不共戴天。",
                });
        }
    }

    /// <summary>幂等写入势力外交看法；目标外交行不存在则跳过。</summary>
    private static void EnsureDiplomacyView(Force source, int targetForceId, EntityEffect effect)
    {
        var dip = source.Diplomacies.FirstOrDefault(d => d.TargetForceId == targetForceId);
        if (dip == null)
            return;

        if (dip.ViewEffects.Any(e => e.Name == effect.Name))
            return;

        dip.ViewEffects.Add(effect);
    }

    /// <summary>演示：当主↔他人物的看法（仅私人关系维度，不含外交）。</summary>
    private static void SeedDemoCharacterViews(GameData gameData, StrategyScenarioMeta meta)
    {
        var playerLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            meta.PlayerForceId,
            meta,
            gameData);
        if (playerLordId <= 0)
            return;

        if (!gameData.Characters.TryGetValue(playerLordId, out var playerLord))
            return;

        foreach (var targetId in new[] { 3, 5, 9 })
        {
            if (!gameData.Characters.TryGetValue(targetId, out _))
                continue;

            EnsureCharacterView(
                playerLord,
                targetId,
                new EntityEffect
                {
                    Id = 1,
                    Name = "骏河继承之争",
                    TargetStat = EffectTargetStat.PersonalOpinion,
                    Magnitude = -20,
                    Duration = EffectDurationKind.LongTerm,
                    Description = "能力不足，难服众臣。",
                });
        }

        if (gameData.Characters.TryGetValue(3, out var yoshimotoHeir))
        {
            EnsureCharacterView(
                yoshimotoHeir,
                playerLordId,
                new EntityEffect
                {
                    Id = 1,
                    Name = "杀害本家当主",
                    TargetStat = EffectTargetStat.Relationship,
                    Magnitude = -100,
                    Duration = EffectDurationKind.Permanent,
                    Description = "杀害本家当主是世仇，今川氏真绝难释怀。",
                },
                new EntityEffect
                {
                    Id = 2,
                    Name = "清洲威胁",
                    TargetStat = EffectTargetStat.PersonalOpinion,
                    Magnitude = -22,
                    Duration = EffectDurationKind.LongTerm,
                    Description = "织田据清洲，对骏河形成直接压力。",
                });
        }
    }

    /// <summary>
    /// 幂等写入角色间看法。误标 Diplomacy 时归并为 Relationship（亲疏）。
    /// 若尚无关系行则创建空 Relationship/Trust 条目。
    /// </summary>
    private static void EnsureCharacterView(Character owner, int targetId, params EntityEffect[] effects)
    {
        var rel = owner.Relationships.FirstOrDefault(r => r.TargetCharacterId == targetId);
        if (rel == null)
        {
            rel = new CharacterRelationship
            {
                OwnerCharacterId = owner.Id,
                TargetCharacterId = targetId,
            };
            owner.Relationships.Add(rel);
        }

        foreach (var effect in effects)
        {
            if (effect.TargetStat is EffectTargetStat.Diplomacy)
                effect.TargetStat = EffectTargetStat.Relationship;

            if (rel.ViewEffects.Any(e => e.Name == effect.Name))
                continue;

            rel.ViewEffects.Add(effect);
        }
    }
}
