using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Helpers;

public static class StrategyForcePerspective
{
    public static StrategyScenarioMeta Create(StrategyScenarioMeta source, GameData data, int forceId)
    {
        var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(forceId, source, data);
        data.Characters.TryGetValue(lordId, out var lord);
        return new StrategyScenarioMeta
        {
            PlayerForceId = forceId,
            HasHumanControlConfiguration = source.HasHumanControlConfiguration,
            HumanControlledForceIds = source.HumanControlledForceIds,
            AllForcesAiControlled = source.AllForcesAiControlled,
            Difficulty = source.Difficulty, StartOptions = source.StartOptions,
            KnownStrongholdIds = source.KnownStrongholdIds,
            ForceLordCharacterIds = source.ForceLordCharacterIds,
            ForceLordResidenceStrongholdIds = source.ForceLordResidenceStrongholdIds,
            Intel = source.Intel, RegionHarvestProfiles = source.RegionHarvestProfiles,
            GameOptions = source.GameOptions,
            LordName = lord?.Name ?? "当主",
            LordUnitId = lord is null ? null : data.Units.Values
                .Where(u => u.ForceId == forceId && u.LeaderId == lord.Id)
                .OrderBy(u => u.Id).Select(u => (int?)u.Id).FirstOrDefault(),
            LordStrongholdId = StrategyLordHelper.ResolveLordResidenceStrongholdId(forceId, data, source)
        };
    }

    public static bool ReceivesReports(StrategyScenarioMeta meta, int forceId)
        => forceId > 0 && (meta.HasHumanControlConfiguration || forceId == meta.PlayerForceId);
}
