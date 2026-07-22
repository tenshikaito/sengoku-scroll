using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Data.Models;
using static SengokuScroll.Domain.Definitions.CharacterDefinition;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Data;

/// <summary>从剧本 JSON 创建最小可玩 <see cref="Character"/> 实体。</summary>
internal static class StrategyScenarioCharacterFactory
{
    internal static Character Create(
        StrategyCharacterDefinition definition,
        Point3 location,
        CharacterLocationType locationType,
        int locationStrongholdId = 0)
    {
        var birthday = definition.BirthYear is int year
            ? new GameDate(year, definition.BirthMonth ?? 1, definition.BirthDay ?? 1)
            : new GameDate(1530, 1, 1);

        return new Character
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = string.IsNullOrWhiteSpace(definition.Description)
                ? definition.Name
                : definition.Description,
            Portrait = string.IsNullOrWhiteSpace(definition.Portrait) ? "default" : definition.Portrait,
            Sex = definition.Sex?.Equals("Female", StringComparison.OrdinalIgnoreCase) == true
                ? SexType.Female
                : SexType.Male,
            Birthday = birthday,
            Type = CharacterType.AI,
            Birth = BirtyType.Landlord,
            CultureId = definition.CultureId ?? 1,
            RegligionId = definition.ReligionId ?? 1,
            Personality = DefaultPersonality(),
            Proficiency = DefaultProficiency(),
            Leadership = (byte)Math.Clamp(definition.Leadership ?? 70, 0, 100),
            Power = (byte)Math.Clamp(definition.Power ?? 70, 0, 100),
            Politics = (byte)Math.Clamp(definition.Politics ?? 60, 0, 100),
            Strategy = (byte)Math.Clamp(definition.Strategy ?? 60, 0, 100),
            Charm = (byte)Math.Clamp(definition.Charm ?? 60, 0, 100),
            ForceId = definition.ForceId,
            StrongholdId = definition.StrongholdId ?? locationStrongholdId,
            LeaderId = definition.LeaderId ?? 0,
            FatherId = definition.FatherId ?? 0,
            MotherId = definition.MotherId ?? 0,
            SpouseId = definition.SpouseId ?? 0,
            MasterId = definition.MasterId ?? 0,
            EnemyIds = definition.EnemyIds?.ToList() ?? [],
            LocationType = locationType,
            Location = location,
            LocationStrongholdId = locationStrongholdId,
            ForceStatus = CharacterForceStatus.Idle,
            ActionPlan = CharacterActionPlan.Rest,
            ActionStatus = CharacterActionStatus.Waiting,
            IsReadyToMove = false,
            Hp = 100,
            Ap = 5,
            ActionTarget = new CharacterActionTarget
            {
                RoutePoints = new Queue<Point2>()
            }
        };
    }

    internal static Character CreateAutoCommander(int id, string name, int forceId, Point3 unitLocation)
        => Create(
            new StrategyCharacterDefinition
            {
                Id = id,
                Name = name,
                ForceId = forceId,
                Leadership = 70
            },
            unitLocation,
            CharacterLocationType.Unit);

    private static PersonalityData DefaultPersonality()
        => new()
        {
            Temper = 50,
            Courage = 50,
            Principle = 50,
            Action = 50,
            Friendship = 50,
            Ambition = 50,
            Hobby = 50,
            Desire = 50,
            Drinking = 50,
            Fortune = 50
        };

    private static ProficiencyData DefaultProficiency()
    {
        ProficiencyStats stat(byte level = 1) => new() { Level = level, Exp = 0 };
        return new ProficiencyData
        {
            Infantry = stat(),
            Ride = stat(),
            Archery = stat(),
            Firelock = stat(),
            Sealing = stat(),
            Military = stat(),
            Fighting = stat(),
            Spy = stat(),
            Agriculture = stat(),
            Commerce = stat(),
            Construct = stat(),
            Smelt = stat(),
            Eloquence = stat(),
            Court = stat(),
            Sociality = stat(),
            Healing = stat()
        };
    }
}
