using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>势力总兵力、当主、文化/信仰等情报派生字段。</summary>
public static class ForceIntelHelper
{
    public sealed record ForceMilitaryBreakdown(int Total, int Garrison, int Militia, int FieldUnits);

    public static ForceMilitaryBreakdown CalculateMilitaryBreakdown(int forceId, GameData gameData)
    {
        var garrison = 0;
        var militia = 0;
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.ForceId != forceId)
                continue;

            StrongholdMilitaryStatsHelper.Recalculate(stronghold, gameData);
            garrison += Math.Max(0, stronghold.ForceActor.GarrisonSoldiers);
            militia += StrongholdMilitaryStatsHelper.GetMilitiaSoldiers(stronghold);
        }

        var fieldUnits = 0;
        foreach (var unit in gameData.Units.Values)
        {
            if (unit.ForceId != forceId || !unit.IsMilitary)
                continue;

            fieldUnits += Math.Max(0, unit.Soldier);
        }

        return new ForceMilitaryBreakdown(
            garrison + militia + fieldUnits,
            garrison,
            militia,
            fieldUnits);
    }

    public static int CalculateTotalSoldiers(int forceId, GameData gameData)
        => CalculateMilitaryBreakdown(forceId, gameData).Total;

    public static void SyncMilitaryCaches(Force force, GameData gameData)
    {
        var breakdown = CalculateMilitaryBreakdown(force.Id, gameData);
        force.TotalSoldiers = breakdown.Total;
        force.GarrisonSoldiers = breakdown.Garrison;
        force.MilitiaSoldiers = breakdown.Militia;
    }

    public static void SyncTotalSoldiersCache(Force force, GameData gameData)
        => SyncMilitaryCaches(force, gameData);

    public static int ResolveLordCharacterId(
        int forceId,
        GameData gameData,
        Data.Models.StrategyScenarioMeta meta)
    {
        if (gameData.Forces.TryGetValue(forceId, out var force)
            && force.LordCharacterId is int cachedLordId
            && cachedLordId > 0
            && gameData.Characters.ContainsKey(cachedLordId))
        {
            return cachedLordId;
        }

        return StrategyStrongholdLordHelper.ResolveForceLordCharacterId(forceId, meta, gameData);
    }

    public static string ResolveLordName(
        Force force,
        GameData gameData,
        Data.Models.StrategyScenarioMeta meta)
    {
        var lordId = ResolveLordCharacterId(force.Id, gameData, meta);
        if (lordId > 0 && gameData.Characters.TryGetValue(lordId, out var lord))
            return lord.Name;

        return "—";
    }

    public static string ResolveCultureName(
        Force force,
        GameData gameData,
        GameMasterData masterData,
        Data.Models.StrategyScenarioMeta meta)
    {
        if (force.CultureId > 0)
            return CultureReligionDisplayHelper.ResolveCultureName(masterData, force.CultureId);

        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(force.Id, gameData, meta);
        if (residenceId > 0
            && gameData.Strongholds.TryGetValue(residenceId, out var residence)
            && meta.Intel.Strongholds.TryGetValue(residence.Id, out var overlay)
            && !string.IsNullOrWhiteSpace(overlay.CultureName))
        {
            return overlay.CultureName.Trim();
        }

        return "日本";
    }

    public static string ResolveReligionName(
        Force force,
        GameData gameData,
        GameMasterData masterData,
        Data.Models.StrategyScenarioMeta meta)
    {
        if (force.RegligionId > 0)
            return CultureReligionDisplayHelper.ResolveReligionName(masterData, force.RegligionId);

        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(force.Id, gameData, meta);
        if (residenceId > 0
            && gameData.Strongholds.TryGetValue(residenceId, out var residence)
            && meta.Intel.Strongholds.TryGetValue(residence.Id, out var overlay)
            && !string.IsNullOrWhiteSpace(overlay.ReligionName))
        {
            return overlay.ReligionName.Trim();
        }

        return "神道教";
    }

    public static int ResolveYearsInForce(Character character, GameDate gameDate)
    {
        if (character.ServiceDate.Year <= 0)
            return 0;

        var years = Math.Max(0, gameDate.Year - character.ServiceDate.Year);
        if (gameDate.Month < character.ServiceDate.Month
            || (gameDate.Month == character.ServiceDate.Month && gameDate.Day < character.ServiceDate.Day))
        {
            years = Math.Max(0, years - 1);
        }

        return years;
    }
}
