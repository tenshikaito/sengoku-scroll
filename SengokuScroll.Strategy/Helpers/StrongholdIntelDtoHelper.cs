using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>据点情报 DTO：常备军、农业季作、商户等扩展映射。</summary>
public static class StrongholdIntelDtoHelper
{
    public static IReadOnlyList<StrategyGarrisonStandingUnitDto> MapStandingGarrison(
        Stronghold stronghold,
        GameData gameData)
    {
        var rows = new List<StrategyGarrisonStandingUnitDto>();
        var forceActor = stronghold.ForceActor;

        foreach (var subId in forceActor.SubUnitIds)
        {
            if (!gameData.SubUnits.TryGetValue(subId, out var sub) || sub.UnitId != 0 || sub.Soldier <= 0)
                continue;

            rows.Add(MapStandingSubUnit(sub, forceActor));
        }

        if (forceActor.Soldier > 0)
        {
            rows.Add(new StrategyGarrisonStandingUnitDto
            {
                SubUnitId = 0,
                UnitName = "农兵备",
                TypeId = StrategyTroopTypes.Ashigaru,
                TypeName = StrategyTroopTypes.ResolveName(StrategyTroopTypes.Ashigaru, null),
                IsMounted = false,
                Soldiers = forceActor.Soldier,
                Role = "Militia",
                Morale = forceActor.Morale,
                Training = forceActor.Training,
                MaintenanceMoney = 0
            });
        }

        return rows;
    }

    private static StrategyGarrisonStandingUnitDto MapStandingSubUnit(SubUnit sub, StrongholdActor forceActor)
        => new()
        {
            SubUnitId = sub.Id,
            UnitName = ResolveUnitName(sub),
            TypeId = sub.TypeId,
            TypeName = StrategyTroopTypes.ResolveName(sub.TypeId, sub.TypeName),
            IsMounted = ResolveIsMounted(sub),
            Soldiers = sub.Soldier,
            Role = sub.TypeId == StrategyTroopTypes.Ashigaru ? "Militia" : "Samurai",
            Morale = sub.Morale > 0 ? sub.Morale : forceActor.Morale,
            Training = sub.Training > 0 ? sub.Training : forceActor.Training,
            MaintenanceMoney = EconomyCalculator.CalculateGarrisonSubUnitMaintenanceMoney(sub)
        };

    private static string ResolveUnitName(SubUnit sub)
    {
        if (!string.IsNullOrWhiteSpace(sub.UnitName))
            return sub.UnitName.Trim();

        return StrategyTroopTypes.ResolveName(sub.TypeId, sub.TypeName);
    }

    private static bool ResolveIsMounted(SubUnit sub)
        => sub.IsMounted || sub.TypeId == StrategyTroopTypes.Cavalry;

    public static IReadOnlyList<StrategyStrongholdCityActorStateDto> MapCityActors(
        Stronghold stronghold,
        string lordName,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var wildCharacterIds = ResolveWildCharacterIds(gameData, stronghold.Id);
        var civilianCharacterIds = wildCharacterIds.Count > 0
            ? wildCharacterIds
            : [.. stronghold.CivilianActor.CharacterIds];

        var rows = new List<StrategyStrongholdCityActorStateDto>
        {
            MapCoreActor(
                stronghold.ForceActor.Id,
                $"{stronghold.Name}官府",
                "Government",
                stronghold.ForceActor,
                lordName,
                characterIds: null),
            MapCoreActor(
                stronghold.CivilianActor.Id,
                "民间",
                "Civilian",
                stronghold.CivilianActor,
                leaderName: "—",
                characterIds: civilianCharacterIds)
        };

        if (stronghold.LordId > 0 && !StrategyStrongholdLordHelper.IsDirectRule(stronghold))
        {
            rows.Add(new StrategyStrongholdCityActorStateDto
            {
                Id = stronghold.LordId,
                Name = string.IsNullOrWhiteSpace(lordName) ? $"国人 #{stronghold.LordId}" : lordName,
                Kind = "Kokujin",
                Money = 0,
                Food = 0,
                LuxuryGoods = 0,
                CommerceProduction = 0,
                AgricultureProduction = 0,
                CharacterCount = 1,
                CharacterIds = [stronghold.LordId],
                LeaderName = string.IsNullOrWhiteSpace(lordName) ? "—" : lordName,
                BranchLabel = "—",
                ForceId = stronghold.ForceId,
            });
        }

        rows.AddRange(stronghold.MerchantActors.Select(m => MapCityActor(m, "Merchant", gameData, meta)));
        rows.AddRange(stronghold.ReligionActors.Select(r => MapCityActor(r, "Religion", gameData, meta)));
        return rows;
    }

    private static IReadOnlyList<int> ResolveWildCharacterIds(GameData gameData, int strongholdId)
        => [.. gameData.Characters.Values
            .Where(c => !c.IsDead
                        && c.ForceId == 0
                        && ResolveCharacterStrongholdId(c) == strongholdId)
            .Select(c => c.Id)
            .OrderBy(id => id)];

    private static int ResolveCharacterStrongholdId(Character character)
        => character.LocationType == Character.CharacterLocationType.Stronghold
            ? character.LocationStrongholdId
            : character.StrongholdId;

    private static StrategyStrongholdCityActorStateDto MapCoreActor(
        int id,
        string name,
        string kind,
        StrongholdActor actor,
        string leaderName,
        IReadOnlyList<int>? characterIds = null)
    {
        var ids = characterIds ?? [.. actor.CharacterIds];
        return new StrategyStrongholdCityActorStateDto
        {
            Id = id,
            Name = name,
            Kind = kind,
            Money = actor.Money,
            Food = actor.Food,
            LuxuryGoods = actor.LuxuryGoods,
            CommerceProduction = actor.CommerceProduction,
            AgricultureProduction = actor.AgricultureProduction,
            CharacterCount = ids.Count,
            CharacterIds = ids,
            LeaderName = leaderName,
            BranchLabel = "—",
            ForceId = actor.ForceId
        };
    }

    private static StrategyStrongholdCityActorStateDto MapCityActor(
        StrongholdActor actor,
        string kind,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var ids = actor.CharacterIds.ToList();
        return new StrategyStrongholdCityActorStateDto
        {
            Id = actor.Id,
            Name = string.IsNullOrWhiteSpace(actor.Name) ? ResolveDefaultCityActorName(kind, actor.Id) : actor.Name,
            Kind = kind,
            Money = actor.Money,
            Food = actor.Food,
            LuxuryGoods = actor.LuxuryGoods,
            CommerceProduction = actor.CommerceProduction,
            AgricultureProduction = actor.AgricultureProduction,
            CharacterCount = ids.Count,
            CharacterIds = ids,
            LeaderName = ResolveLeaderName(gameData, ids, actor, kind),
            BranchLabel = ResolveBranchLabel(gameData, meta, actor),
            ForceId = actor.ForceId
        };
    }

    private static string ResolveLeaderName(
        GameData gameData,
        IReadOnlyList<int> characterIds,
        StrongholdActor actor,
        string kind)
    {
        foreach (var id in characterIds)
        {
            if (gameData.Characters.TryGetValue(id, out var character)
                && !string.IsNullOrWhiteSpace(character.Name))
            {
                return character.Name;
            }
        }

        if (characterIds.Count == 0)
            return ResolveVirtualCityActorLeaderName(actor, kind);

        return "—";
    }

    private static string ResolveVirtualCityActorLeaderName(StrongholdActor actor, string kind)
    {
        var name = string.IsNullOrWhiteSpace(actor.Name) ? "寺社" : actor.Name.Trim();
        if (kind == "Religion")
        {
            if (name.Contains('神', StringComparison.Ordinal)
                || name.Contains('社', StringComparison.Ordinal)
                || name.Contains('宫', StringComparison.Ordinal))
            {
                return $"{name}司祭";
            }

            return $"{name}住持";
        }

        return $"{name}掌柜";
    }

    private static string ResolveLeaderName(GameData gameData, IReadOnlyList<int> characterIds)
    {
        foreach (var id in characterIds)
        {
            if (gameData.Characters.TryGetValue(id, out var character)
                && !string.IsNullOrWhiteSpace(character.Name))
            {
                return character.Name;
            }
        }

        return "—";
    }

    private static string ResolveBranchLabel(
        GameData gameData,
        StrategyScenarioMeta meta,
        StrongholdActor actor)
        => OrganizationForceHelper.ResolveBranchLabel(gameData, meta, actor);

    private static string ResolveDefaultCityActorName(string kind, int id)
        => kind switch
        {
            "Religion" => $"寺社 #{id}",
            _ => $"商户 #{id}"
        };

    public static IReadOnlyList<StrategyCropCycleStateDto> MapCropCycles(
        Stronghold stronghold,
        string effectiveCropPattern,
        IReadOnlyDictionary<int, RegionHarvestProfile> regionProfiles,
        int regionId)
    {
        var agriculture = stronghold.Agriculture ?? new StrongholdAgricultureState();
        var phases = AgricultureCropRules.ResolveGrowthPhases(effectiveCropPattern);
        var cycleCount = AgricultureCropRules.ResolveActiveCycleCount(effectiveCropPattern);
        var harvestEvents = regionId > 0 && regionProfiles.TryGetValue(regionId, out var profile)
            ? profile.Events
            : [];

        var rows = new List<StrategyCropCycleStateDto>();
        for (var cycleIndex = 0; cycleIndex < cycleCount; cycleIndex++)
        {
            var cyclePhases = phases.Where(p => p.CycleIndex == cycleIndex).ToList();
            if (cyclePhases.Count == 0)
                continue;

            var start = cyclePhases[0];
            var end = cyclePhases[^1];
            var progressBp = agriculture.GetProgressBp(cycleIndex);
            var capBp = agriculture.GetProgressCapBp(cycleIndex);
            var shareBp = ResolveHarvestShareBp(harvestEvents, cycleIndex, cycleCount);
            var potential = stronghold.CivilianActor.AgricultureProduction * shareBp
                            / AgricultureConstants.ProgressBasisPoints;
            var estimated = AgricultureCalculator.CalculateGrossHarvestGo(
                stronghold,
                new HarvestEventDefinition(end.EndMonth, end.EndDay, shareBp),
                progressBp);

            rows.Add(new StrategyCropCycleStateDto
            {
                CycleIndex = cycleIndex,
                Name = ResolveCycleName(cycleIndex, effectiveCropPattern),
                StartMonth = start.StartMonth,
                StartDay = start.StartDay,
                EndMonth = end.EndMonth,
                EndDay = end.EndDay,
                ProgressPercent = progressBp / 100,
                ProgressCapPercent = capBp / 100,
                PotentialYieldGo = potential,
                EstimatedYieldGo = estimated
            });
        }

        return rows;
    }

    private static int ResolveHarvestShareBp(
        IReadOnlyList<HarvestEventDefinition> events,
        int cycleIndex,
        int cycleCount)
    {
        if (events.Count == 0)
            return AgricultureConstants.ProgressBasisPoints / Math.Max(1, cycleCount);

        if (cycleIndex < events.Count)
            return events[cycleIndex].ShareBasisPoints;

        return events[^1].ShareBasisPoints;
    }

    private static string ResolveCycleName(int cycleIndex, string effectiveCropPattern)
        => effectiveCropPattern switch
        {
            AgricultureCropRules.Double => cycleIndex switch
            {
                0 => "早稻",
                1 => "晚稻",
                _ => $"第{cycleIndex + 1}季"
            },
            AgricultureCropRules.Triple => cycleIndex switch
            {
                0 => "早稻",
                1 => "晚稻",
                2 => "第三季",
                _ => $"第{cycleIndex + 1}季"
            },
            _ => "单季作"
        };
}
