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
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
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

        var difficulty = StrategyDifficultyRules.Parse(scenario.Difficulty);

        return new StrategyScenarioMeta
        {
            PlayerForceId = scenario.PlayerForceId,
            Difficulty = difficulty,
            StartOptions = GameStartOptionsPresets.Resolve(difficulty, customOverride: null),
            KnownStrongholdIds = scenario.KnownStrongholdIds,
            LordName = scenario.Lord?.Name ?? "当主",
            LordUnitId = scenario.Lord?.UnitId,
            LordStrongholdId = scenario.Lord?.StrongholdId,
            ForceLordCharacterIds = BuildForceLordCharacterIds(scenario),
            ForceLordResidenceStrongholdIds = BuildForceLordResidenceStrongholdIds(scenario),
            Intel = intel,
            RegionHarvestProfiles = BuildRegionHarvestProfiles(document.Map),
            GameOptions = document.Scenario.GameOptions ?? new StrategyScenarioGameOptions()
        };
    }

    /// <summary>用 UI/联机传入的加载选项覆盖剧本默认难度与开局配置。</summary>
    public static StrategyScenarioMeta ApplyLoadOptions(
        StrategyScenarioMeta meta,
        StrategyLoadOptions? loadOptions)
    {
        if (loadOptions is null)
            return meta;

        var difficulty = loadOptions.Difficulty ?? meta.Difficulty;
        var startOptions = difficulty == StrategyDifficulty.Custom && loadOptions.CustomStartOptions is not null
            ? loadOptions.CustomStartOptions
            : GameStartOptionsPresets.Resolve(difficulty, loadOptions.CustomStartOptions);

        return new StrategyScenarioMeta
        {
            PlayerForceId = meta.PlayerForceId,
            AllForcesAiControlled = loadOptions.AllForcesAiControlled || meta.AllForcesAiControlled,
            Difficulty = difficulty,
            StartOptions = startOptions,
            KnownStrongholdIds = meta.KnownStrongholdIds,
            LordName = meta.LordName,
            LordUnitId = meta.LordUnitId,
            LordStrongholdId = meta.LordStrongholdId,
            ForceLordCharacterIds = meta.ForceLordCharacterIds,
            ForceLordResidenceStrongholdIds = meta.ForceLordResidenceStrongholdIds,
            Intel = meta.Intel,
            RegionHarvestProfiles = meta.RegionHarvestProfiles,
            GameOptions = meta.GameOptions
        };
    }

    /// <summary>剧本指定种子优先；否则用 Id+起始日派生，保证同剧本开局可回放。</summary>
    private static int ResolveSimulationSeed(StrategyScenarioDocument document)
    {
        if (document.Scenario.SimulationSeed != 0)
            return document.Scenario.SimulationSeed;

        var start = document.Scenario.StartDate;
        var hash = HashCode.Combine(document.Id, start.Year, start.Month, start.Day);
        return hash == int.MinValue ? 1 : Math.Abs(hash);
    }

    private static Dictionary<int, RegionHarvestProfile> BuildRegionHarvestProfiles(StrategyMapDefinition map)
    {
        var profiles = new Dictionary<int, RegionHarvestProfile>();

        foreach (var region in map.Regions)
        {
            var events = ResolveHarvestEvents(region);
            profiles[region.Id] = new RegionHarvestProfile
            {
                RegionId = region.Id,
                Events = events
            };
        }

        return profiles;
    }

    private static IReadOnlyList<HarvestEventDefinition> ResolveHarvestEvents(
        StrategyRegionDefinition region)
    {
        if (region.HarvestEvents is { Count: > 0 } custom)
        {
            return custom
                .Select(e => new HarvestEventDefinition(e.Month, e.Day, e.ShareBasisPoints))
                .ToList();
        }

        // 业务：未自定义时按作物模式套用默认收成分配（二作/三作/单作）
        return region.CropPattern?.Trim() switch
        {
            "Double" or "double" =>
            [
                new(HarvestConstants.DefaultDoubleEarly.Month,
                    HarvestConstants.DefaultDoubleEarly.Day,
                    HarvestConstants.DefaultDoubleEarly.ShareBasisPoints),
                new(HarvestConstants.DefaultDoubleLate.Month,
                    HarvestConstants.DefaultDoubleLate.Day,
                    HarvestConstants.DefaultDoubleLate.ShareBasisPoints)
            ],
            "Triple" or "triple" =>
            [
                new(HarvestConstants.DefaultDoubleEarly.Month,
                    HarvestConstants.DefaultDoubleEarly.Day,
                    3333),
                new(HarvestConstants.DefaultDoubleLate.Month,
                    HarvestConstants.DefaultDoubleLate.Day,
                    3333),
                new(HarvestConstants.DefaultNorthernSingle.Month,
                    HarvestConstants.DefaultNorthernSingle.Day,
                    3334)
            ],
            _ =>
            [
                new(HarvestConstants.DefaultNorthernSingle.Month,
                    HarvestConstants.DefaultNorthernSingle.Day,
                    HarvestConstants.DefaultNorthernSingle.ShareBasisPoints)
            ]
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
            // 业务：剧本显式当主名时，覆盖玩家势力的当主角色 Id
            var lordCharacter = scenario.Characters.FirstOrDefault(c =>
                string.Equals(c.Name, scenario.Lord.Name, StringComparison.Ordinal));
            if (lordCharacter is not null)
                map[scenario.PlayerForceId] = lordCharacter.Id;
        }

        return map;
    }

    private static Dictionary<int, int> BuildForceLordResidenceStrongholdIds(StrategyScenarioDefinition scenario)
    {
        var map = new Dictionary<int, int>();

        foreach (var force in scenario.Forces)
        {
            if (force.LordCharacterId <= 0)
                continue;

            var lordCharacter = scenario.Characters.FirstOrDefault(c => c.Id == force.LordCharacterId);
            if (lordCharacter?.StrongholdId is int residenceId)
                map[force.Id] = residenceId;
        }

        if (scenario.Lord?.StrongholdId is int playerResidenceId)
            map[scenario.PlayerForceId] = playerResidenceId;
        else if (scenario.Lord is not null)
        {
            var lordCharacter = scenario.Characters.FirstOrDefault(c =>
                string.Equals(c.Name, scenario.Lord.Name, StringComparison.Ordinal));
            if (lordCharacter?.StrongholdId is int residenceId)
                map[scenario.PlayerForceId] = residenceId;
        }

        return map;
    }

    private static GameWorld BuildWorld(StrategyScenarioDocument document)
    {
        var terrainByKey = document.Map.Terrains.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);
        if (!terrainByKey.TryGetValue(document.Map.DefaultTerrain, out var defaultTerrain))
            throw new InvalidOperationException($"默认地形 '{document.Map.DefaultTerrain}' 未在 terrains 中定义。");

        var tileMap = BuildTileMap(document.Map, terrainByKey, defaultTerrain);
        var regions = BuildRegions(document.Map);
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
                Description = t.Key,
                MovementCost = t.MovementCost,
                Type = TerrainType.Plain
            });

        var forces = document.Scenario.Forces.ToDictionary(
            f => f.Id,
            f => CreateForce(f));

        var strongholds = new Dictionary<int, Stronghold>();

        foreach (var definition in document.Scenario.Strongholds)
        {
            var stronghold = CreateStronghold(definition, landmarks);
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
            Characters = [],
            Roads = BuildRoadCells(document.Map, tileMap.Width, tileMap.Height)
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
                Regions = regions,
                Roads = roads,
                Landmarks = landmarks
            },
            GameMapData = mapData,
            GameMasterData = new GameMasterData
            {
                CultureGroups = [],
                Cultures = [],
                ReligionGroups = [],
                Religions = [],
                StrongholdTypes = [],
                DefenseFacilityTypes = StrongholdDefenseRules.CreateDefaultDefenseFacilityTypes(),
                UnitTypes = [],
                Characters = [],
                Technologies = StrategyDefaultMasterDataSeed.CreateDefaultTechnologies()
            },
            GameData = new GameData
            {
                GameDate = new GameDate(startDate.Year, startDate.Month, startDate.Day),
                SimulationSeed = ResolveSimulationSeed(document),
                Forces = forces,
                Strongholds = strongholds,
                Units = units,
                SubUnits = subUnits,
                Characters = characters,
                SupplyConvoys = [],
                MessageCarriers = []
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

        ApplyScenarioDiplomacy(forces, document.Scenario.Diplomacies);
        ApplyDefaultWarDiplomacy(forces.Values, document.Scenario.Forces);
        StrategyDefaultMasterDataSeed.Apply(world);
        ApplyDefaultStrongholdDefense(world);
        ApplyDefaultEconomyFacilities(world);
        ApplyAgricultureAndGarrisonBootstrap(world, document);

        return world;
    }

    private static void ApplyAgricultureAndGarrisonBootstrap(GameWorld world, StrategyScenarioDocument document)
    {
        var profiles = BuildRegionHarvestProfiles(document.Map);
        var gameData = world.GameData;

        foreach (var stronghold in gameData.Strongholds.Values)
        {
            var regionId = RegionLocationHelper.ResolveRegionId(world, stronghold.Location);
            var regionPattern = AgricultureCropRules.ResolveRegionCropPattern(profiles, regionId);
            AgricultureCropRules.InitializeForStronghold(stronghold, regionPattern);
            StrongholdMilitaryBootstrapHelper.InitializeGarrisonComposition(stronghold, gameData);
        }
    }

    private static void ApplyDefaultEconomyFacilities(GameWorld world)
    {
        foreach (var stronghold in world.GameData.Strongholds.Values)
        {
            // 业务：剧本未写经济设施时补默认（如 Market）
            if (stronghold.EconomyFacilityIds.Count > 0)
                continue;

            stronghold.EconomyFacilityIds.AddRange(
                EconomyFacilityRules.ResolveDefaultFacilityIds(stronghold));
        }
    }

    private static void ApplyDefaultStrongholdDefense(GameWorld world)
    {
        var master = world.GameMasterData;
        foreach (var stronghold in world.GameData.Strongholds.Values)
        {
            // 业务：未声明城防设施时按人口补默认，并重算城防值
            if (stronghold.DefenseFacilityIds.Count == 0)
            {
                stronghold.DefenseFacilityIds.AddRange(
                    StrongholdDefenseRules.ResolveDefaultFacilityIds(stronghold.Population));
            }

            stronghold.Defense = (byte)Math.Min(
                byte.MaxValue,
                StrongholdDefenseRules.ResolveTotalDefense(stronghold, master));
        }
    }

    /// <summary>应用剧本 JSON 中声明的开局外交（双向）。</summary>
    private static void ApplyScenarioDiplomacy(
        Dictionary<int, Force> forces,
        IReadOnlyList<StrategyDiplomacyDefinition> diplomacies)
    {
        foreach (var definition in diplomacies)
        {
            if (!forces.TryGetValue(definition.ForceId, out var source))
                throw new InvalidOperationException($"外交配置引用了未知势力 Id：{definition.ForceId}。");

            if (!forces.TryGetValue(definition.TargetForceId, out var target))
                throw new InvalidOperationException($"外交配置引用了未知势力 Id：{definition.TargetForceId}。");

            if (source.Id == target.Id)
                throw new InvalidOperationException($"外交配置不能指向自身势力 Id：{source.Id}。");

            var relation = ParseDiplomacyRelation(definition.Relation);

            if (!source.Diplomacies.Any(d => d.TargetForceId == target.Id))
            {
                source.Diplomacies.Add(new Diplomacy
                {
                    ForceId = source.Id,
                    TargetForceId = target.Id,
                    Relation = relation
                });
            }

            if (!target.Diplomacies.Any(d => d.TargetForceId == source.Id))
            {
                target.Diplomacies.Add(new Diplomacy
                {
                    ForceId = target.Id,
                    TargetForceId = source.Id,
                    Relation = relation
                });
            }
        }
    }

    private static Diplomacy.DiplomacyRelation ParseDiplomacyRelation(string? relation)
        => relation?.Trim() switch
        {
            nameof(Diplomacy.DiplomacyRelation.Allied) => Diplomacy.DiplomacyRelation.Allied,
            nameof(Diplomacy.DiplomacyRelation.Enemy) => Diplomacy.DiplomacyRelation.Enemy,
            nameof(Diplomacy.DiplomacyRelation.Neutral) => Diplomacy.DiplomacyRelation.Neutral,
            _ => throw new InvalidOperationException($"未知外交关系：{relation}。")
        };

    /// <summary>剧本未声明外交时，多势力默认互相敌对（M3 最小战争状态）。</summary>
    private static void ApplyDefaultWarDiplomacy(
        IEnumerable<Force> forces,
        IReadOnlyList<StrategyForceDefinition> definitions)
    {
        var excludeFromDefaultDiplomacy = definitions
            .Where(d => d.ExcludeFromDefaultDiplomacy)
            .Select(d => d.Id)
            .ToHashSet();

        var list = forces.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                var a = list[i];
                var b = list[j];
                if (a.Diplomacies.Any(d => d.TargetForceId == b.Id))
                    continue;

                // 业务：标记为孤立势力时不自动建立外交关系
                if (excludeFromDefaultDiplomacy.Contains(a.Id)
                    || excludeFromDefaultDiplomacy.Contains(b.Id))
                    continue;

                // 业务：家臣势力与宗主不自动设为敌对
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
                // 业务：角色驻留据点则同步地图坐标
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

        // 业务：剧本只给主将名而无角色记录时，自动创建并绑定
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
            // 业务：代官（LeaderId）可与领主分离，按名称匹配同势力角色
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

        var regionBytes = BuildRegionGrid(map, map.Width, map.Height);

        return new TileMap(terrainBytes, regionBytes, map.Width, map.Height);
    }

    private static Dictionary<int, RegionDefinition> BuildRegions(StrategyMapDefinition map)
        => map.Regions.ToDictionary(
            r => r.Id,
            r => new RegionDefinition
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Key,
                ClimateId = 0
            });

    private static byte[] BuildRegionGrid(StrategyMapDefinition map, int width, int height)
    {
        var length = width * height;
        var grid = new byte[length];
        if (map.RegionGrid is not { Count: > 0 } keys)
            return grid;

        if (keys.Count != length)
            throw new InvalidOperationException($"regionGrid 长度应为 {length}，实际为 {keys.Count}。");

        var regionByKey = map.Regions.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);
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

    private static Dictionary<int, byte> BuildRoadCells(
        StrategyMapDefinition map,
        int width,
        int height)
    {
        var roads = new Dictionary<int, byte>();
        ApplyRoadTemplates(roads, map, width, height);
        return roads;
    }

    private static Dictionary<int, Landmark> BuildLandmarks(StrategyMapDefinition map)
    {
        var points = new Dictionary<int, Landmark>();
        foreach (var landmark in map.Landmarks)
        {
            points[landmark.Id] = new Landmark
            {
                Id = landmark.Id,
                Name = landmark.Name,
                Location = new Point3(landmark.X, landmark.Y)
            };
        }

        return points;
    }

    private static void ApplyRoadTemplates(
        Dictionary<int, byte> roadCells,
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
                roadCells[index] = (byte)template.TypeId;
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

    private static Stronghold CreateStronghold(
        StrategyStrongholdDefinition definition,
        IReadOnlyDictionary<int, Landmark> landmarks)
    {
        var forceActor = CreateStrongholdActor(
            definition.Id * 10,
            definition.ForceId,
            definition.Id,
            definition.Food,
            definition.Morale,
            definition.Training,
            definition.Money,
            garrisonSoldiers: definition.GarrisonSoldiers);

        var civilianActor = CreateCivilianActor(
            definition.Id * 10 + 1,
            definition.ForceId,
            definition.Id,
            definition.Population,
            popularFeelings: definition.PopularFeelings > 0 ? definition.PopularFeelings : (byte)50);

        return new Stronghold
        {
            Id = definition.Id,
            Name = definition.Name,
            ForceId = definition.ForceId,
            Location = new Point3(definition.X, definition.Y),
            Population = definition.Population,
            Stability = definition.Stability > 0 ? definition.Stability : (byte)50,
            PollTaxRate = definition.PollTaxRate,
            AgricultureTaxRate = definition.AgricultureTaxRate,
            CommerceTaxRate = definition.CommerceTaxRate,
            TariffTaxRate = definition.TariffTaxRate,
            CommerceValue = Math.Max(1000, definition.Population * 2),
            Defense = definition.Defense,
            IsHistorical = ResolveIsHistorical(definition, landmarks),
            ForceActor = forceActor,
            CivilianActor = civilianActor,
            Market = new StrongholdMarket(),
            MerchantActors = [],
            ReligionActors = [],
            DefenseFacilityIds = definition.DefenseFacilityIds.Count > 0
                ? [..definition.DefenseFacilityIds]
                : [],
            EconomyFacilityIds = definition.EconomyFacilityIds.Count > 0
                ? [..definition.EconomyFacilityIds]
                : [],
            HasCoreForceIds = [definition.ForceId],
            Agriculture = new StrongholdAgricultureState()
        };
    }

    private static StrongholdActor CreateCivilianActor(
        int id,
        int forceId,
        int strongholdId,
        int population,
        byte popularFeelings = 50)
    {
        var actor = CreateStrongholdActor(
            id,
            forceId,
            strongholdId,
            popularFeelings: popularFeelings);
        actor.Name = "民间";
        actor.AgricultureProduction = Math.Max(1000, population * 15);
        actor.CommerceProduction = Math.Max(100, population * 10);
        return actor;
    }

    private static bool ResolveIsHistorical(
        StrategyStrongholdDefinition definition,
        IReadOnlyDictionary<int, Landmark> landmarks)
    {
        if (definition.IsHistorical.HasValue)
            return definition.IsHistorical.Value;

        return landmarks.Values.Any(l =>
            l.Location.X == definition.X && l.Location.Y == definition.Y);
    }

    private static StrongholdActor CreateStrongholdActor(
        int id,
        int forceId,
        int strongholdId,
        int food = 0,
        int morale = 80,
        int training = 65,
        int money = 0,
        byte popularFeelings = 50,
        int garrisonSoldiers = 0)
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
            Money = money,
            Soldier = garrisonSoldiers,
            PopularFeelings = popularFeelings
        };

    private static Unit CreateUnit(StrategyUnitDefinition definition)
    {
        var directive = UnitDirective.Move;
        // 业务：方针 Support 时单位进入 Hold 姿态（驻守/支援）
        if (!string.IsNullOrWhiteSpace(definition.Directive)
            && Enum.TryParse<UnitDirective>(definition.Directive, ignoreCase: true, out var parsed))
            directive = parsed;

        const int defaultMovementCap = 5;
        var movement = Math.Clamp(definition.Movement, 1, defaultMovementCap);

        return new()
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
            Movement = movement,
            Ap = movement,
            IsMilitary = true,
            IsReadyToMove = true,
            Status = UnitStatus.Waiting,
            Directive = directive,
            Stance = directive == UnitDirective.Support ? UnitStance.Hold : UnitStance.Normal,
            SubUnitIds = [],
            ActionTarget = new UnitActionTarget
            {
                RoutePoints = new Queue<Point2>()
            }
        };
    }
}
