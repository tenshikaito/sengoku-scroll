using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>演示用城中势力：在野浪人、多商户/寺社及下属。</summary>
public static class CityActorDemoRosterHelper
{
    public static void EnsureDemoRoster(GameData gameData)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.Id != 1 || !string.Equals(stronghold.Name, "清洲", StringComparison.Ordinal))
                continue;

            EnsureWildCharacters(gameData, stronghold);
            EnsureDemoStaffCharacters(gameData, stronghold);
            EnsureDemoMerchants(gameData, stronghold);
            EnsureDemoReligions(gameData, stronghold);
            break;
        }
    }

    private static void EnsureWildCharacters(GameData gameData, Stronghold stronghold)
    {
        var wildIds = new[]
        {
            EnsureCharacter(gameData, stronghold, 90_001, "佐藤源平", forceId: 0),
            EnsureCharacter(gameData, stronghold, 90_002, "和田义盛", forceId: 0),
            EnsureCharacter(gameData, stronghold, 90_003, "山本勘助", forceId: 0),
        };

        stronghold.CivilianActor.CharacterIds = [.. wildIds];
    }

    private static void EnsureDemoStaffCharacters(GameData gameData, Stronghold stronghold)
    {
        var atsutaId = OrganizationForceHelper.GetOrCreate(gameData, "热田神宫", ForceCategory.Religion).Id;
        EnsureCharacter(gameData, stronghold, 90_011, "三井高利", OrganizationForceHelper.KnownIds.Mitsui);
        EnsureCharacter(gameData, stronghold, 90_014, "三井与一", OrganizationForceHelper.KnownIds.Mitsui);
        EnsureCharacter(gameData, stronghold, 90_012, "今井宗久", OrganizationForceHelper.KnownIds.Imai);
        EnsureCharacter(gameData, stronghold, 90_013, "津田作左卫门", OrganizationForceHelper.KnownIds.Imai);
        EnsureCharacter(gameData, stronghold, 90_020, "大祝官", atsutaId, religionId: 1);
        EnsureCharacter(gameData, stronghold, 90_021, "神官", atsutaId, religionId: 1);
        EnsureCharacter(gameData, stronghold, 90_022, "证愿寺住持", OrganizationForceHelper.KnownIds.Shoganji, religionId: 4);
        EnsureCharacter(gameData, stronghold, 90_030, "柏来图", OrganizationForceHelper.KnownIds.Nanban, religionId: 6);

        if (gameData.Characters.TryGetValue(90_014, out var mitsuiClerk))
        {
            mitsuiClerk.LeaderId = 90_011;
            mitsuiClerk.FatherId = 90_011;
        }
        if (gameData.Characters.TryGetValue(90_013, out var imaiClerk))
            imaiClerk.LeaderId = 90_012;
    }

    private static void EnsureDemoMerchants(GameData gameData, Stronghold stronghold)
    {
        var primary = stronghold.MerchantActors.FirstOrDefault(m => m.Id == stronghold.Id * 1000 + 7);
        if (primary != null)
        {
            primary.Name = "三井屋";
            primary.ForceId = OrganizationForceHelper.KnownIds.Mitsui;
            primary.CommerceProduction = Math.Max(primary.CommerceProduction, 720);
            primary.Money = Math.Max(primary.Money, 24_000);
            ReplaceActorCharacterIds(gameData, primary, 90_011, 90_014);
        }

        var nanban = stronghold.MerchantActors.FirstOrDefault(m => m.Id == stronghold.Id * 1000 + 9);
        if (nanban != null)
        {
            nanban.Name = "南蛮商会";
            nanban.ForceId = OrganizationForceHelper.KnownIds.Nanban;
            nanban.Money = Math.Max(nanban.Money, 35_000);
            nanban.CharacterIds = [90_030];
        }

        if (stronghold.MerchantActors.Any(m => m.Id == stronghold.Id * 1000 + 71))
            return;

        stronghold.MerchantActors.Add(new StrongholdActor
        {
            Id = stronghold.Id * 1000 + 71,
            Name = "今井屋",
            ForceId = OrganizationForceHelper.KnownIds.Imai,
            StrongholdId = stronghold.Id,
            Type = ActorType.Merchant,
            Money = 28_000,
            Food = MarketConstants.MerchantFoodReserveGo * 4,
            CommerceProduction = 640,
            AgricultureProduction = 0,
            CharacterIds = [90_012, 90_013],
            SubUnitIds = []
        });
    }

    private static void EnsureDemoReligions(GameData gameData, Stronghold stronghold)
    {
        var atsutaId = OrganizationForceHelper.GetOrCreate(gameData, "热田神宫", ForceCategory.Religion).Id;
        var primary = stronghold.ReligionActors.FirstOrDefault(r => r.Id == stronghold.Id * 1000 + 8);
        if (primary != null)
        {
            primary.Name = "热田神宫";
            primary.ForceId = atsutaId;
            primary.AgricultureProduction = Math.Max(primary.AgricultureProduction, 240_000);
            primary.CharacterIds = [90_020, 90_021];
        }

        if (stronghold.ReligionActors.Any(r => r.Id == stronghold.Id * 1000 + 88))
            return;

        stronghold.ReligionActors.Add(new StrongholdActor
        {
            Id = stronghold.Id * 1000 + 88,
            Name = "证愿寺",
            ForceId = OrganizationForceHelper.KnownIds.Shoganji,
            StrongholdId = stronghold.Id,
            Type = ActorType.Regligion,
            Money = 5_000,
            Food = MarketConstants.MerchantFoodReserveGo / 2,
            CommerceProduction = 0,
            AgricultureProduction = 120_000,
            CharacterIds = [90_022],
            SubUnitIds = []
        });
    }

    public static int EnsureCharacter(
        GameData gameData,
        Stronghold stronghold,
        int id,
        string name,
        int forceId,
        int? religionId = null)
    {
        if (gameData.Characters.TryGetValue(id, out var existing))
        {
            existing.ForceId = forceId;
            existing.StrongholdId = stronghold.Id;
            existing.LocationStrongholdId = stronghold.Id;
            existing.LocationType = CharacterLocationType.Stronghold;
            existing.Location = stronghold.Location;
            return id;
        }

        var character = StrategyScenarioCharacterFactory.Create(
            new StrategyCharacterDefinition
            {
                Id = id,
                Name = name,
                ForceId = forceId,
                StrongholdId = stronghold.Id,
                Leadership = 55,
                Politics = 50,
                Charm = 52,
                ReligionId = religionId
            },
            stronghold.Location,
            CharacterLocationType.Stronghold,
            stronghold.Id);
        character.ForceId = forceId;
        gameData.Characters[id] = character;
        return id;
    }

    private static void ReplaceActorCharacterIds(
        GameData gameData,
        StrongholdActor actor,
        params int[] nextIds)
    {
        var previousIds = actor.CharacterIds.ToList();
        actor.CharacterIds = [.. nextIds];

        foreach (var previousId in previousIds)
        {
            if (nextIds.Contains(previousId))
                continue;

            if (gameData.Characters.TryGetValue(previousId, out var orphan)
                && orphan.ForceId == actor.ForceId)
            {
                gameData.Characters.Remove(previousId);
            }
        }
    }
}
