using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Policies.BattlefieldDto;

public sealed class BattlefieldParticipantBuckets
{
    public int Soldiers { get; set; }

    public int AggressorSoldiers { get; set; }

    public Dictionary<int, (int Soldiers, int MoraleWeighted, int Money, int Food)> ForceBuckets { get; } = new();
}

public interface IBattlefieldKindDtoEnrichmentBehavior
{
    BattlefieldKind Kind { get; }

    void EnrichParticipants(Battlefield battlefield, GameData gameData, BattlefieldParticipantBuckets buckets);

    string? ResolveSiegeThreat(Battlefield battlefield, GameData gameData);
}

internal sealed class SiegeBattlefieldKindDtoEnrichmentBehavior : IBattlefieldKindDtoEnrichmentBehavior
{
    public static readonly SiegeBattlefieldKindDtoEnrichmentBehavior Instance = new();

    public BattlefieldKind Kind => BattlefieldKind.Siege;

    public void EnrichParticipants(
        Battlefield battlefield,
        GameData gameData,
        BattlefieldParticipantBuckets buckets)
    {
        if (battlefield.StrongholdId <= 0
            || !gameData.Strongholds.TryGetValue(battlefield.StrongholdId, out var siegeTarget))
        {
            return;
        }

        var garrisonSoldiers = StrongholdGarrisonRules.GetCityGarrisonSoldiers(siegeTarget);
        if (garrisonSoldiers <= 0
            && siegeTarget.ForceActor.Money <= 0
            && siegeTarget.ForceActor.Food <= 0)
        {
            return;
        }

        if (!buckets.ForceBuckets.TryGetValue(siegeTarget.ForceId, out var defenderBucket))
            defenderBucket = (0, 0, 0, 0);

        defenderBucket.Soldiers += garrisonSoldiers;
        defenderBucket.MoraleWeighted += siegeTarget.ForceActor.Morale * Math.Max(1, garrisonSoldiers);
        defenderBucket.Money += siegeTarget.ForceActor.Money;
        defenderBucket.Food += siegeTarget.ForceActor.Food;
        buckets.ForceBuckets[siegeTarget.ForceId] = defenderBucket;
        buckets.Soldiers += garrisonSoldiers;
    }

    public string? ResolveSiegeThreat(Battlefield battlefield, GameData gameData)
    {
        if (battlefield.StrongholdId <= 0
            || !gameData.Strongholds.TryGetValue(battlefield.StrongholdId, out var stronghold))
        {
            return null;
        }

        return StrategyWorldStateDtoSiegeThreatResolver.Resolve(stronghold, gameData);
    }
}

internal sealed class FieldBattlefieldKindDtoEnrichmentBehavior : IBattlefieldKindDtoEnrichmentBehavior
{
    public static readonly FieldBattlefieldKindDtoEnrichmentBehavior Instance = new();

    public BattlefieldKind Kind => BattlefieldKind.Field;

    public void EnrichParticipants(Battlefield battlefield, GameData gameData, BattlefieldParticipantBuckets buckets)
    {
    }

    public string? ResolveSiegeThreat(Battlefield battlefield, GameData gameData) => null;
}

public static class BattlefieldKindDtoEnrichmentRegistry
{
    private static readonly Dictionary<BattlefieldKind, IBattlefieldKindDtoEnrichmentBehavior> ByKind =
        new IBattlefieldKindDtoEnrichmentBehavior[]
        {
            SiegeBattlefieldKindDtoEnrichmentBehavior.Instance,
            FieldBattlefieldKindDtoEnrichmentBehavior.Instance,
        }.ToDictionary(b => b.Kind);

    public static IBattlefieldKindDtoEnrichmentBehavior Resolve(BattlefieldKind kind)
        => ByKind.TryGetValue(kind, out var behavior)
            ? behavior
            : FieldBattlefieldKindDtoEnrichmentBehavior.Instance;
}

public static class StrategyWorldStateDtoSiegeThreatResolver
{
    public static string? Resolve(Stronghold stronghold, GameData gameData)
    {
        if (!gameData.Forces.TryGetValue(stronghold.ForceId, out var holderForce))
            return null;

        string? encircle = null;
        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0 || unit.SiegeMode == UnitSiegeMode.None)
                continue;

            if (!unit.Location.IsSameTile(stronghold.Location))
                continue;

            if (unit.ForceId == stronghold.ForceId)
                continue;

            if (!gameData.Forces.TryGetValue(unit.ForceId, out var unitForce))
                continue;

            if (!DiplomacyRules.IsEnemy(unitForce, holderForce).IsSuccess)
                continue;

            if (unit.SiegeMode == UnitSiegeMode.Assault)
                return "Assault";

            if (unit.SiegeMode == UnitSiegeMode.Encircle)
                encircle = "Encircle";
        }

        foreach (var battlefield in gameData.Battlefields.Values)
        {
            if (battlefield.IsClosed || battlefield.Kind != BattlefieldKind.Siege)
                continue;

            if (battlefield.Location.X != stronghold.Location.X
                || battlefield.Location.Y != stronghold.Location.Y)
                continue;

            return encircle ?? "Assault";
        }

        return encircle;
    }
}
