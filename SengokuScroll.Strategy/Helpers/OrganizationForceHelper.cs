using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>商家/寺社组织势力：Force 实体 + 据点内店 Actor（MerchantActors/ReligionActors）。</summary>
public static class OrganizationForceHelper
{
    public static class KnownIds
    {
        public const int Mitsui = 10_001;
        public const int Imai = 10_002;
        public const int Nanban = 10_003;
        public const int Shoganji = 10_004;
    }

    public static int ResolveForceId(string organizationName)
        => organizationName switch
        {
            "三井屋" => KnownIds.Mitsui,
            "今井屋" => KnownIds.Imai,
            "南蛮商会" => KnownIds.Nanban,
            "证愿寺" => KnownIds.Shoganji,
            _ => 10_100 + Math.Abs(StringComparer.Ordinal.GetHashCode(organizationName) % 8_900),
        };

    public static bool IsOrganizationForce(Force force)
        => force.Category is ForceCategory.Merchant or ForceCategory.Religion;

    public static bool IsOrganizationForceId(GameData gameData, int forceId)
        => gameData.Forces.TryGetValue(forceId, out var force) && IsOrganizationForce(force);

    public static Force GetOrCreate(
        GameData gameData,
        string name,
        ForceCategory category)
    {
        var id = ResolveForceId(name);
        if (gameData.Forces.TryGetValue(id, out var existing))
            return existing;

        var force = new Force
        {
            Id = id,
            Name = name,
            ForceId = id,
            Category = category,
            Status = Force.ForceStatus.Independence,
            AcceptedCultureIds = [],
            Provinces = [],
            CharacterIds = [],
            Diplomacies = [],
            SubUnitIds = [],
        };
        gameData.Forces[id] = force;
        return force;
    }

    public static IEnumerable<StrongholdActor> EnumerateShops(GameData gameData, int organizationForceId)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            foreach (var merchant in stronghold.MerchantActors)
            {
                if (merchant.ForceId == organizationForceId)
                    yield return merchant;
            }

            foreach (var religion in stronghold.ReligionActors)
            {
                if (religion.ForceId == organizationForceId)
                    yield return religion;
            }
        }
    }

    public static int CountShops(GameData gameData, int organizationForceId)
        => EnumerateShops(gameData, organizationForceId).Count();

    public static int CountCharacters(GameData gameData, int organizationForceId)
    {
        var referenced = CollectReferencedCharacterIds(gameData, organizationForceId);
        return referenced.Count(id =>
            gameData.Characters.TryGetValue(id, out var character) && !character.IsDead);
    }

    public static HashSet<int> CollectReferencedCharacterIds(GameData gameData, int organizationForceId)
    {
        var ids = new HashSet<int>();
        foreach (var shop in EnumerateShops(gameData, organizationForceId))
        {
            foreach (var id in shop.CharacterIds.Where(id => id > 0))
                ids.Add(id);
        }

        return ids;
    }

    public static void PruneUnreferencedOrganizationCharacters(GameData gameData)
    {
        foreach (var force in gameData.Forces.Values.Where(IsOrganizationForce))
        {
            var referenced = CollectReferencedCharacterIds(gameData, force.Id);
            var orphanIds = gameData.Characters.Values
                .Where(c => !c.IsDead && c.ForceId == force.Id && !referenced.Contains(c.Id))
                .Select(c => c.Id)
                .ToList();

            foreach (var orphanId in orphanIds)
                gameData.Characters.Remove(orphanId);
        }
    }

    public static void AccumulateShopTreasury(GameData gameData, Force organizationForce)
    {
        var money = 0;
        var food = 0;
        foreach (var shop in EnumerateShops(gameData, organizationForce.Id))
        {
            money += shop.Money;
            food += shop.Food;
        }

        organizationForce.Money = money;
        organizationForce.Food = food;
    }

    public static string ResolveBranchLabel(
        GameData gameData,
        StrategyScenarioMeta meta,
        StrongholdActor shop)
    {
        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            shop.ForceId,
            gameData,
            meta);
        if (residenceId <= 0)
            return "—";

        var isReligion = gameData.Forces.TryGetValue(shop.ForceId, out var force)
            && force.Category == ForceCategory.Religion;
        if (isReligion)
            return shop.StrongholdId == residenceId ? "本院" : "分院";

        return shop.StrongholdId == residenceId ? "本店" : "分店";
    }

    /// <summary>店铺店员：本店具名当主，分店/店员为纸娃娃真实 Character。</summary>
    public static int EnsureShopCharacter(
        GameData gameData,
        Stronghold stronghold,
        int characterId,
        int organizationForceId,
        string organizationName,
        bool preferFamousLeader,
        string? explicitName = null,
        int? religionId = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            return CityActorDemoRosterHelper.EnsureCharacter(
                gameData,
                stronghold,
                characterId,
                explicitName.Trim(),
                organizationForceId,
                religionId);
        }

        if (preferFamousLeader)
        {
            var famousName = MerchantBootstrapHelper.ResolveDefaultLeaderName(organizationName);
            if (!string.IsNullOrWhiteSpace(famousName))
            {
                return CityActorDemoRosterHelper.EnsureCharacter(
                    gameData,
                    stronghold,
                    characterId,
                    famousName,
                    organizationForceId,
                    religionId);
            }
        }

        return PaperDollCharacterHelper.EnsurePaperDollCharacter(
            gameData,
            stronghold,
            characterId,
            organizationForceId,
            $"{organizationName}#{stronghold.Id}#{characterId}",
            religionId);
    }

    public static void SyncOrganizationLordRegistries(
        GameData gameData,
        StrategyForceLordRegistry registry)
    {
        foreach (var force in gameData.Forces.Values)
        {
            if (!IsOrganizationForce(force))
                continue;

            var shops = EnumerateShops(gameData, force.Id)
                .OrderBy(shop => shop.StrongholdId)
                .ThenBy(shop => shop.Id)
                .ToList();
            if (shops.Count == 0)
                continue;

            var leaderId = shops[0].CharacterIds.FirstOrDefault(id => id > 0);
            if (leaderId > 0)
                registry.SetLordCharacterId(force.Id, leaderId);

            AccumulateShopTreasury(gameData, force);
        }

        PruneUnreferencedOrganizationCharacters(gameData);
        ConsolidateDuplicateMerchantLeaders(gameData);
        SyncOrganizationCharacterStrongholds(gameData);
    }

    public static void SyncOrganizationCharacterStrongholds(GameData gameData)
    {
        foreach (var force in gameData.Forces.Values.Where(IsOrganizationForce))
        {
            foreach (var shop in EnumerateShops(gameData, force.Id))
            {
                if (!gameData.Strongholds.TryGetValue(shop.StrongholdId, out var stronghold))
                    continue;

                foreach (var id in shop.CharacterIds.Where(id => id > 0))
                {
                    if (!gameData.Characters.TryGetValue(id, out var character))
                        continue;

                    character.ForceId = force.Id;
                    character.StrongholdId = shop.StrongholdId;
                    character.LocationStrongholdId = shop.StrongholdId;
                    character.LocationType = Character.CharacterLocationType.Stronghold;
                    character.Location = stronghold.Location;
                }
            }
        }
    }

    /// <summary>合并各分店 bootstrap 重复创建的同名当主（如多个「今井宗久」）。</summary>
    public static void ConsolidateDuplicateMerchantLeaders(GameData gameData)
    {
        foreach (var force in gameData.Forces.Values.Where(f => f.Category == ForceCategory.Merchant))
        {
            var canonicalName = MerchantBootstrapHelper.ResolveDefaultLeaderName(force.Name);
            if (!string.IsNullOrWhiteSpace(canonicalName))
                ConsolidateDuplicateNamedStaff(gameData, force, canonicalName);

            if (IsNanbanMerchantName(force.Name))
                ConsolidateDuplicateNamedStaff(gameData, force, "南蛮商人");
        }
    }

    private static void ConsolidateDuplicateNamedStaff(GameData gameData, Force force, string staffName)
    {
        var duplicates = gameData.Characters.Values
            .Where(c => !c.IsDead && c.ForceId == force.Id && c.Name == staffName)
            .OrderBy(c => c.Id)
            .ToList();
        if (duplicates.Count <= 1)
            return;

        var canonical = duplicates[0];
        foreach (var duplicate in duplicates.Skip(1))
        {
            foreach (var shop in EnumerateShops(gameData, force.Id))
            {
                if (!shop.CharacterIds.Remove(duplicate.Id))
                    continue;

                if (shop.CharacterIds.Count == 0
                    && gameData.Strongholds.TryGetValue(shop.StrongholdId, out var stronghold))
                {
                    var branchId = 90_000 + shop.StrongholdId * 100 + (shop.Id % 1000);
                    EnsureShopCharacter(
                        gameData,
                        stronghold,
                        branchId,
                        force.Id,
                        force.Name,
                        preferFamousLeader: false);
                    shop.CharacterIds.Add(branchId);
                }
            }

            gameData.Characters.Remove(duplicate.Id);
        }

        if (gameData.Characters.TryGetValue(canonical.Id, out var leader))
            leader.StrongholdId = ResolvePrimaryShopStrongholdId(gameData, force.Id, canonical.Id);
    }

    private static int ResolvePrimaryShopStrongholdId(GameData gameData, int organizationForceId, int characterId)
    {
        foreach (var shop in EnumerateShops(gameData, organizationForceId).OrderBy(s => s.StrongholdId).ThenBy(s => s.Id))
        {
            if (shop.CharacterIds.Contains(characterId))
                return shop.StrongholdId;
        }

        return 0;
    }

    public static bool IsNanbanMerchantName(string? name)
        => !string.IsNullOrWhiteSpace(name)
           && name.Contains("南蛮", StringComparison.Ordinal);
}
