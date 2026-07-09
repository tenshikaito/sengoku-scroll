using System.Text.Json;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Domain.World;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Unit;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Data;

/// <summary>从 JSON 剧本文件构建 <see cref="GameWorld"/>（M1-d）。</summary>
public static class StrategyScenarioLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>从 JSON 文件路径加载剧本世界。</summary>
    public static StrategyLoadedScenario LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    /// <summary>从 JSON 字符串加载剧本世界。</summary>
    public static StrategyLoadedScenario LoadFromJson(string json)
    {
        var document = JsonSerializer.Deserialize<StrategyScenarioDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("无法解析策略剧本 JSON。");

        return new StrategyLoadedScenario(BuildWorld(document), BuildMeta(document));
    }

    private static StrategyScenarioMeta BuildMeta(StrategyScenarioDocument document)
    {
        var scenario = document.Scenario;
        var intel = new StrategyScenarioIntelCatalog
        {
            Units = scenario.Units.ToDictionary(
                u => u.Id,
                u => new StrategyUnitIntelOverlay
                {
                    CommanderName = u.CommanderName,
                    CultureName = u.CultureName,
                    ReligionName = u.ReligionName
                }),
            Strongholds = scenario.Strongholds.ToDictionary(
                s => s.Id,
                s => new StrategyStrongholdIntelOverlay
                {
                    LordName = s.LordName,
                    MayorName = s.MayorName,
                    CultureName = s.CultureName,
                    ReligionName = s.ReligionName
                })
        };

        return new StrategyScenarioMeta
        {
            PlayerForceId = scenario.PlayerForceId,
            LordName = scenario.Lord?.Name ?? "当主",
            LordUnitId = scenario.Lord?.UnitId,
            LordStrongholdId = scenario.Lord?.StrongholdId,
            ForceLordCharacterIds = BuildForceLordCharacterIds(scenario),
            Intel = intel
        };
    }

    private static Dictionary<int, int> BuildForceLordCharacterIds(StrategyScenarioDefinition scenario)
    {
        var map = new Dictionary<int, int>();

        foreach (var force in scenario.Forces)
        {
            if (force.LordCharacterId > 0)
                map[force.Id] = force.LordCharacterId;
        }

        if (scenario.Lord is not null)
        {
            var lordCharacter = scenario.Characters.FirstOrDefault(c =>
                string.Equals(c.Name, scenario.Lord.Name, StringComparison.Ordinal));
            if (lordCharacter is not null)
                map[scenario.PlayerForceId] = lordCharacter.Id;
        }

        return map;
    }

    private static GameWorld BuildWorld(StrategyScenarioDocument document)
    {
        var terrainByKey = document.Map.Terrains.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);
        if (!terrainByKey.TryGetValue(document.Map.DefaultTerrain, out var defaultTerrain))
            throw new InvalidOperationException($"默认地形 '{document.Map.DefaultTerrain}' 未在 terrains 中定义。");

        var tileMap = BuildTileMap(document.Map, terrainByKey, defaultTerrain);
        var politicalRegions = BuildPoliticalRegions(document.Map);
        var politicalRegionGrid = BuildPoliticalRegionGrid(document.Map);
        var landmarks = BuildLandmarks(document.Map);

        var roads = document.Map.RoadTypes.ToDictionary(
            r => r.Id,
            r => new RoadDefinition
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Key,
                SpeedBonus = r.SpeedBonus,
                MovementCostOverride = r.MovementCost
            });

        var terrains = document.Map.Terrains.ToDictionary(
            t => t.Id,
            t => new TerrainDefinition
            {
                Name = t.Name,
                Altitude = 0,
                Description = t.Name,
                MovementCost = t.MovementCost,
                Type = TerrainType.Plain
            });

        var forces = document.Scenario.Forces.ToDictionary(
            f => f.Id,
            f => CreateForce(f));

        var strongholds = new Dictionary<int, Stronghold>();

        foreach (var definition in document.Scenario.Strongholds)
        {
            var stronghold = CreateStronghold(definition);
            strongholds[definition.Id] = stronghold;
        }

        var units = new Dictionary<int, Unit>();

        foreach (var definition in document.Scenario.Units)
        {
            var unit = CreateUnit(definition);
            units[definition.Id] = unit;
        }

        var characters = LoadCharacters(document.Scenario, strongholds, units);

        var subUnits = LoadSubUnits(document.Scenario, units, characters);

        var mapData = new GameMapData
        {
            Strongholds = [],
            Units = [],
            Characters = []
        };

        var startDate = document.Scenario.StartDate;

        var world = new GameWorld(document.Id)
        {
            GameMapMasterData = new GameMapMasterData
            {
                Name = document.Name,
                Version = document.Version,
                TileMap = tileMap,
                Terrains = terrains,
                TerrainVegatationFeatures = [],
                TerrainSurfaceFeatures = [],
                Climates = [],
                Regions = politicalRegions,
                PoliticalRegionGrid = politicalRegionGrid,
                Roads = roads,
                StrongholdPoints = landmarks
            },
            GameMapData = mapData,
            GameMasterData = new GameMasterData
            {
                CultureGroups = [],
                Cultures = [],
                ReligionGroups = [],
                Religions = [],
                StrongholdTypes = [],
                DefenseFacilityTypes = [],
                UnitTypes = [],
                Characters = []
            },
            GameData = new GameData
            {
                GameDate = new GameDate(startDate.Year, startDate.Month, startDate.Day),
                Forces = forces,
                Strongholds = strongholds,
                Units = units,
                SubUnits = subUnits,
                Characters = characters,
                SupplyConvoys = [],
                Messengers = []
            }
        };

        foreach (var stronghold in strongholds.Values)
            MapLocationActions.RegisterStronghold(world, stronghold);

        foreach (var unit in units.Values)
            MapLocationActions.RegisterUnit(world, unit);

        foreach (var character in characters.Values)
            MapLocationActions.RegisterCharacter(world, character);

        foreach (var character in characters.Values)
        {
            if (forces.TryGetValue(character.ForceId, out var force)
                && !force.CharacterIds.Contains(character.Id))
            {
                force.CharacterIds.Add(character.Id);
            }
        }

        ApplyDefaultWarDiplomacy(forces.Values);

        return world;
    }

    /// <summary>剧本未声明外交时，多势力默认互相敌对（M3 最小战争状态）。</summary>
    private static void ApplyDefaultWarDiplomacy(IEnumerable<Force> forces)
    {
        var list = forces.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                var a = list[i];
                var b = list[j];
                if (a.Diplomacies.Any(d => d.TargetForceId == b.Id))
                    continue;

                if (IsInnerVassalOf(a, b) || IsInnerVassalOf(b, a))
                    continue;

                a.Diplomacies.Add(new Diplomacy
                {
                    ForceId = a.Id,
                    TargetForceId = b.Id,
                    Relation = Diplomacy.DiplomacyRelation.Enemy
                });
                b.Diplomacies.Add(new Diplomacy
                {
                    ForceId = b.Id,
                    TargetForceId = a.Id,
                    Relation = Diplomacy.DiplomacyRelation.Enemy
                });
            }
        }
    }

    private static bool IsInnerVassalOf(Force vassal, Force suzerain)
        => vassal.Status == Force.ForceStatus.InnerVassal
           && vassal.SuzerainForceId == suzerain.Id;

    private static Dictionary<int, Character> LoadCharacters(
        StrategyScenarioDefinition scenario,
        Dictionary<int, Stronghold> strongholds,
        Dictionary<int, Unit> units)
    {
        var characters = new Dictionary<int, Character>();

        foreach (var definition in scenario.Characters)
        {
            if (characters.ContainsKey(definition.Id))
                throw new InvalidOperationException($"重复的角色 Id：{definition.Id}。");

            var location = new Point3(0, 0);
            var locationType = CharacterLocationType.Stronghold;
            var locationStrongholdId = 0;

            if (definition.StrongholdId is int strongholdId
                && strongholds.TryGetValue(strongholdId, out var stronghold))
            {
                location = stronghold.Location;
                locationStrongholdId = strongholdId;
            }

            characters[definition.Id] = StrategyScenarioCharacterFactory.Create(
                definition,
                location,
                locationType,
                locationStrongholdId);
        }

        foreach (var unitDefinition in scenario.Units)
        {
            if (!units.TryGetValue(unitDefinition.Id, out var unit))
                continue;

            AssignUnitCommander(unitDefinition, unit, characters);
        }

        foreach (var strongholdDefinition in scenario.Strongholds)
        {
            if (!strongholds.TryGetValue(strongholdDefinition.Id, out var stronghold))
                continue;

            AssignStrongholdStaff(strongholdDefinition, stronghold, characters);
        }

        return characters;
    }

    private static Dictionary<int, SubUnit> LoadSubUnits(
        StrategyScenarioDefinition scenario,
        Dictionary<int, Unit> units,
        Dictionary<int, Character> characters)
    {
        var subUnits = new Dictionary<int, SubUnit>();
        var nextId = 1;

        foreach (var unitDefinition in scenario.Units)
        {
            if (unitDefinition.Composition.Count == 0)
                continue;

            if (!units.TryGetValue(unitDefinition.Id, out var unit))
                continue;

            var totalSoldiers = 0;

            foreach (var entry in unitDefinition.Composition)
            {
                var subUnitId = entry.Id ?? nextId++;
                if (entry.Id is null)
                    nextId = Math.Max(nextId, subUnitId + 1);
                else
                    nextId = Math.Max(nextId, subUnitId + 1);

                if (subUnits.ContainsKey(subUnitId))
                    throw new InvalidOperationException($"重复的子编制 Id：{subUnitId}。");

                var leaderId = ResolveSubUnitCommanderId(entry, unitDefinition.ForceId, characters);

                var subUnit = new SubUnit
                {
                    Id = subUnitId,
                    TypeId = (byte)entry.TypeId,
                    TypeName = StrategyTroopTypes.ResolveName(entry.TypeId, entry.TypeName),
                    ForceId = unitDefinition.ForceId,
                    StrongholdId = 0,
                    UnitId = unit.Id,
                    Soldier = entry.Soldiers,
                    LeaderId = leaderId
                };

                subUnits[subUnitId] = subUnit;
                unit.SubUnitIds.Add(subUnitId);
                totalSoldiers += entry.Soldiers;
            }

            if (totalSoldiers > 0)
                unit.Soldier = totalSoldiers;
        }

        return subUnits;
    }

    private static int ResolveSubUnitCommanderId(
        StrategySubUnitCompositionDefinition entry,
        int forceId,
        Dictionary<int, Character> characters)
    {
        if (entry.CommanderId is int commanderId && characters.ContainsKey(commanderId))
            return commanderId;

        if (string.IsNullOrWhiteSpace(entry.CommanderName))
            return 0;

        var existing = characters.Values.FirstOrDefault(c =>
            c.ForceId == forceId
            && string.Equals(c.Name, entry.CommanderName, StringComparison.Ordinal));

        return existing?.Id ?? 0;
    }

    private static void AssignUnitCommander(
        StrategyUnitDefinition unitDefinition,
        Unit unit,
        Dictionary<int, Character> characters)
    {
        if (unitDefinition.CommanderId is int commanderId
            && characters.TryGetValue(commanderId, out var commanderById))
        {
            unit.LeaderId = commanderId;
            AttachCommanderToUnit(commanderById, unit);
            return;
        }

        if (string.IsNullOrWhiteSpace(unitDefinition.CommanderName))
            return;

        var existing = characters.Values.FirstOrDefault(c =>
            c.ForceId == unitDefinition.ForceId
            && string.Equals(c.Name, unitDefinition.CommanderName, StringComparison.Ordinal));

        if (existing is not null)
        {
            unit.LeaderId = existing.Id;
            AttachCommanderToUnit(existing, unit);
            return;
        }

        var nextId = characters.Keys.DefaultIfEmpty(0).Max() + 1;
        var created = StrategyScenarioCharacterFactory.CreateAutoCommander(
            nextId,
            unitDefinition.CommanderName,
            unitDefinition.ForceId,
            unit.Location);
        characters[nextId] = created;
        unit.LeaderId = nextId;
        AttachCommanderToUnit(created, unit);
    }

    private static void AttachCommanderToUnit(Character commander, Unit unit)
    {
        commander.ForceId = unit.ForceId;
        commander.Location = unit.Location;
        commander.LocationType = CharacterLocationType.Unit;
    }

    private static void AssignStrongholdStaff(
        StrategyStrongholdDefinition definition,
        Stronghold stronghold,
        Dictionary<int, Character> characters)
    {
        stronghold.LordId = ResolveStrongholdLordId(definition, characters);

        if (stronghold.LordId > 0)
        {
            if (!characters.TryGetValue(stronghold.LordId, out var lord))
            {
                throw new InvalidOperationException(
                    $"据点 {definition.Id} 的领主角色 Id {stronghold.LordId} 不存在。");
            }

            StrategyStrongholdLordHelper.EnsureLordResidence(stronghold, lord);
        }

        if (!string.IsNullOrWhiteSpace(definition.MayorName))
        {
            var mayor = characters.Values.FirstOrDefault(c =>
                c.ForceId == definition.ForceId
                && string.Equals(c.Name, definition.MayorName, StringComparison.Ordinal));
            if (mayor is not null)
                stronghold.LeaderId = mayor.Id;
        }
    }

    private static int ResolveStrongholdLordId(
        StrategyStrongholdDefinition definition,
        Dictionary<int, Character> characters)
    {
        if (definition.LordId > 0)
            return definition.LordId;

        if (!string.IsNullOrWhiteSpace(definition.LordName))
        {
            var lord = characters.Values.FirstOrDefault(c =>
                c.ForceId == definition.ForceId
                && string.Equals(c.Name, definition.LordName, StringComparison.Ordinal));
            if (lord is not null)
                return lord.Id;
        }

        return 0;
    }

    private static TileMap BuildTileMap(
        StrategyMapDefinition map,
        IReadOnlyDictionary<string, StrategyTerrainDefinition> terrainByKey,
        StrategyTerrainDefinition defaultTerrain)
    {
        var length = map.Width * map.Height;
        var terrainBytes = new byte[length];

        if (map.TerrainGrid is { Count: > 0 } grid)
        {
            if (grid.Count != length)
                throw new InvalidOperationException($"terrainGrid 长度应为 {length}，实际为 {grid.Count}。");

            for (var i = 0; i < length; i++)
            {
                var key = grid[i];
                terrainBytes[i] = (byte)(terrainByKey.TryGetValue(key, out var terrain) ? terrain.Id : defaultTerrain.Id);
            }
        }
        else
        {
            Array.Fill(terrainBytes, (byte)defaultTerrain.Id);
        }

        var regionBytes = new byte[length];
        ApplyRoadTemplates(regionBytes, map, map.Width, map.Height);

        return new TileMap(terrainBytes, regionBytes, map.Width, map.Height);
    }

    private static Dictionary<int, RegionDefinition> BuildPoliticalRegions(StrategyMapDefinition map)
        => map.PoliticalRegions.ToDictionary(
            r => r.Id,
            r => new RegionDefinition
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Key,
                ClimateId = 0
            });

    private static byte[] BuildPoliticalRegionGrid(StrategyMapDefinition map)
    {
        var length = map.Width * map.Height;
        var grid = new byte[length];
        if (map.PoliticalRegionGrid is not { Count: > 0 } keys)
            return grid;

        if (keys.Count != length)
            throw new InvalidOperationException($"politicalRegionGrid 长度应为 {length}，实际为 {keys.Count}。");

        var regionByKey = map.PoliticalRegions.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < length; i++)
        {
            var key = keys[i];
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (regionByKey.TryGetValue(key, out var region))
                grid[i] = (byte)region.Id;
        }

        return grid;
    }

    private static Dictionary<int, StrongholdPoint> BuildLandmarks(StrategyMapDefinition map)
    {
        var points = new Dictionary<int, StrongholdPoint>();
        foreach (var landmark in map.Landmarks)
        {
            points[landmark.Id] = new StrongholdPoint
            {
                Id = landmark.Id,
                Name = landmark.Name,
                Location = new Point3(landmark.X, landmark.Y)
            };
        }

        return points;
    }

    private static void ApplyRoadTemplates(
        byte[] regionBytes,
        StrategyMapDefinition map,
        int width,
        int height)
    {
        if (map.RoadTemplates.Count == 0 || map.PlacedRoads.Count == 0)
            return;

        var templates = map.RoadTemplates.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var templateId in map.PlacedRoads)
        {
            if (!templates.TryGetValue(templateId, out var template))
                continue;

            foreach (var point in template.Points)
            {
                if (point.X < 0 || point.Y < 0 || point.X >= width || point.Y >= height)
                    continue;

                var index = point.Y * width + point.X;
                regionBytes[index] = (byte)template.TypeId;
            }
        }
    }

    private static Force CreateForce(StrategyForceDefinition definition)
        => new()
        {
            Id = definition.Id,
            Name = definition.Name,
            ForceId = definition.Id,
            Food = definition.Food,
            Money = definition.Money,
            Status = ParseForceStatus(definition.Status),
            SuzerainForceId = definition.SuzerainForceId,
            AcceptedCultureIds = [],
            Provinces = [],
            CharacterIds = [],
            Diplomacies = [],
            SubUnitIds = []
        };

    private static Force.ForceStatus ParseForceStatus(string? status)
        => status?.Trim() switch
        {
            nameof(Force.ForceStatus.InnerVassal) => Force.ForceStatus.InnerVassal,
            nameof(Force.ForceStatus.OuterVassal) => Force.ForceStatus.OuterVassal,
            _ => Force.ForceStatus.Independence
        };

    private static Stronghold CreateStronghold(StrategyStrongholdDefinition definition)
    {
        var forceActor = CreateStrongholdActor(
            definition.Id * 10,
            definition.ForceId,
            definition.Id,
            definition.Food,
            definition.Morale,
            definition.Training,
            definition.Money);

        return new Stronghold
        {
            Id = definition.Id,
            Name = definition.Name,
            ForceId = definition.ForceId,
            Location = new Point3(definition.X, definition.Y),
            Population = definition.Population,
            PollTaxRate = definition.PollTaxRate,
            AgricultureTaxRate = definition.AgricultureTaxRate,
            CommerceTaxRate = definition.CommerceTaxRate,
            TariffTaxRate = definition.TariffTaxRate,
            ForceActor = forceActor,
            CivilianActor = CreateStrongholdActor(definition.Id * 10 + 1, definition.ForceId, definition.Id),
            MerchantActors = [],
            ReligionActors = [],
            DefenseFacilityIds = [],
            HasCoreForceIds = [definition.ForceId]
        };
    }

    private static StrongholdActor CreateStrongholdActor(
        int id,
        int forceId,
        int strongholdId,
        int food = 0,
        int morale = 80,
        int training = 65,
        int money = 0)
        => new()
        {
            Id = id,
            Name = "官府",
            Type = ActorType.Force,
            ForceId = forceId,
            StrongholdId = strongholdId,
            CharacterIds = [],
            SubUnitIds = [],
            Food = food,
            Morale = (byte)morale,
            Training = (byte)training,
            Money = money
        };

    private static Unit CreateUnit(StrategyUnitDefinition definition)
        => new()
        {
            Id = definition.Id,
            Name = definition.Name,
            ForceId = definition.ForceId,
            Location = new Point3(definition.X, definition.Y),
            Soldier = definition.Soldiers,
            Food = definition.Food,
            Money = definition.Money,
            Morale = (byte)definition.Morale,
            Training = (byte)definition.Training,
            Movement = definition.Movement,
            Ap = definition.Movement,
            IsMilitary = true,
            IsReadyToMove = true,
            Status = UnitStatus.Waiting,
            SubUnitIds = [],
            ActionTarget = new UnitActionTarget
            {
                RoutePoints = new Queue<Point2>()
            }
        };
}
