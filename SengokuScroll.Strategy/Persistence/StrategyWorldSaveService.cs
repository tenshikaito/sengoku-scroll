using System.Text.Json;
using System.Text.Json.Serialization;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using static SengokuScroll.Domain.Entities.Unit;

using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Persistence;

/// <summary>单机 JSON 存档文档（M3-d 最小可恢复字段）。</summary>
public sealed class StrategySaveDocument
{
    /// <summary>存档格式版本；V2 起包含完整运行时 GameData 快照。</summary>
    public int FormatVersion { get; init; } = 2;

    public required string ScenarioId { get; init; }

    public required int PlayerForceId { get; init; }
    public bool IsMultiplayer { get; init; }

    /// <summary>保存本局难度与开局规则；旧存档缺省时沿用剧本默认。</summary>
    public string? Difficulty { get; init; }

    public GameStartOptionsDto? StartOptions { get; init; }

    public bool? AllForcesAiControlled { get; init; }

    /// <summary>本局固定随机种子；缺省兼容旧存档（Apply 时不覆盖）。</summary>
    public int? SimulationSeed { get; init; }

    public required StrategySaveDate Date { get; init; }

    public required List<StrategySaveForce> Forces { get; init; }

    public required List<StrategySaveStronghold> Strongholds { get; init; }

    public required List<StrategySaveUnit> Units { get; init; }

    /// <summary>玩家势力探索态（explored + known 据点）。</summary>
    public StrategyVisibilitySaveDto? Visibility { get; init; }

    /// <summary>角色情报运行时字段（忠诚/关系/任务/增减益等）。</summary>
    public List<StrategySaveCharacter>? Characters { get; init; }

    /// <summary>
    /// 完整运行时状态。使用 JSON 元素隔离捕获时对象，避免保存后世界继续推进而污染存档。
    /// 旧 V1 存档缺少本字段时仍由下方兼容 DTO 恢复。
    /// </summary>
    public JsonElement? RuntimeState { get; init; }

    /// <summary>情报、税费、贡纳与在途报告等非 GameData 单局服务状态。</summary>
    public JsonElement? RuntimeServices { get; set; }
}

public sealed class StrategySaveDate
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public required int Day { get; init; }
}

public sealed class StrategySaveForce
{
    public required int Id { get; init; }

    public required int Money { get; init; }

    public required int Food { get; init; }

    public int? LordCharacterId { get; init; }

    public string? Introduction { get; init; }

    public List<StrategySaveEntityEffect>? ActiveEffects { get; init; }

    public List<StrategySaveDiplomacy>? Diplomacies { get; init; }
}

public sealed class StrategySaveStronghold
{
    public required int Id { get; init; }

    public required int ForceId { get; init; }

    public required int LordId { get; init; }

    public required int Population { get; init; }

    public required int Food { get; init; }

    public required int Money { get; init; }

    public int GarrisonSoldiers { get; init; }

    /// <summary>农兵池（ForceActor.Soldier）；新存档优先读此字段。</summary>
    public int MilitiaSoldiers { get; init; }

    public byte Scale { get; init; }

    public string? Introduction { get; init; }

    public List<StrategySaveEntityEffect>? ActiveEffects { get; init; }

    public StrategySaveAgriculture? Agriculture { get; init; }
}

public sealed class StrategySaveAgriculture
{
    public int EarlyCycleProgressBp { get; init; }

    public int LateCycleProgressBp { get; init; }

    public int ThirdCycleProgressBp { get; init; }

    public int EarlyCycleProgressCapBp { get; init; } = 10_000;

    public int LateCycleProgressCapBp { get; init; } = 10_000;

    public int ThirdCycleProgressCapBp { get; init; } = 10_000;

    public bool KnowsDoubleCrop { get; init; }

    public bool KnowsTripleCrop { get; init; }
}

public sealed class StrategySaveUnit
{
    public required int Id { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int Soldiers { get; init; }

    public required int Food { get; init; }

    public required int Ap { get; init; }

    public required string Status { get; init; }

    public required string Directive { get; init; }

    public required List<StrategySavePoint> Route { get; init; }
}

public sealed class StrategySavePoint
{
    public required int X { get; init; }

    public required int Y { get; init; }
}

public sealed class StrategySaveEntityEffect
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string TargetStat { get; init; }

    public int Magnitude { get; init; }

    public required string Duration { get; init; }

    public string? Description { get; init; }

    public StrategySaveDate? ExpiresOn { get; init; }
}

public sealed class StrategySaveCharacterRelationship
{
    public int TargetCharacterId { get; init; }

    public sbyte Relationship { get; init; }

    public sbyte Trust { get; init; }

    public List<StrategySaveEntityEffect>? ViewEffects { get; init; }
}

public sealed class StrategySaveIntelTask
{
    public required string TaskCategory { get; init; }

    public required string Name { get; init; }

    public required string Target { get; init; }

    public required string Status { get; init; }

    public required string Remaining { get; init; }
}

public sealed class StrategySaveDiplomacyMission
{
    public required string Action { get; init; }

    public int TargetForceId { get; init; }

    public int RemainingDays { get; init; }

    public int SuccessChancePercent { get; init; }

    public List<int>? CededStrongholdIds { get; init; }

    public int ReparationsMoney { get; init; }

    public bool DemandOuterVassalage { get; init; }
}

public sealed class StrategySaveCharacter
{
    public required int Id { get; init; }

    public int? Money { get; init; }

    public int? Ap { get; init; }

    public int? Hp { get; init; }

    public int? Emotion { get; init; }

    public byte Loyalty { get; init; }

    public StrategySaveDate? ServiceDate { get; init; }

    public List<StrategySaveCharacterRelationship>? Relationships { get; init; }

    public List<StrategySaveEntityEffect>? ActiveEffects { get; init; }

    public List<StrategySaveIntelTask>? IntelTasks { get; init; }

    public StrategySaveDiplomacyMission? DiplomacyMission { get; init; }

    public string? ForceStatus { get; init; }
}

public sealed class StrategySaveDiplomacy
{
    public int TargetForceId { get; init; }

    public List<StrategySaveEntityEffect>? ViewEffects { get; init; }
}

/// <summary>从运行中世界捕获/恢复存档。</summary>
public static class StrategyWorldSaveService
{
    private static readonly JsonSerializerOptions RuntimeStateJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new GameDateJsonConverter(),
            new Point2JsonConverter(),
            new Point3JsonConverter()
        }
    };

    internal static JsonSerializerOptions RuntimeStateSerializationOptions
        => RuntimeStateJsonOptions;

    /// <summary>捕获势力金粮、据点归属/领主/驻军与单位位置/路线等可变状态。</summary>
    public static StrategySaveDocument Capture(
        GameWorld world,
        string scenarioId,
        int playerForceId,
        StrategyVisibilityLedger visibilityLedger,
        StrategyScenarioMeta? scenarioMeta = null)
    {
        var data = world.GameData;
        var date = data.GameDate;
        var tileMap = world.GameMapMasterData.TileMap;

        return new StrategySaveDocument
        {
            ScenarioId = scenarioId,
            PlayerForceId = playerForceId,
            IsMultiplayer = scenarioMeta?.HasHumanControlConfiguration ?? false,
            Difficulty = scenarioMeta?.Difficulty.ToString(),
            StartOptions = scenarioMeta is null
                ? null
                : GameStartOptionsMapper.ToDto(scenarioMeta.StartOptions),
            AllForcesAiControlled = scenarioMeta?.AllForcesAiControlled,
            SimulationSeed = data.SimulationSeed,
            RuntimeState = JsonSerializer.SerializeToElement(new GameData
            {
                GameDate = data.GameDate, SimulationSeed = data.SimulationSeed,
                NextBattlefieldId = data.NextBattlefieldId,
                Forces = data.Forces.OrderBy(x => x.Key).ToDictionary(),
                Strongholds = data.Strongholds.OrderBy(x => x.Key).ToDictionary(),
                Units = data.Units.OrderBy(x => x.Key).ToDictionary(),
                SubUnits = data.SubUnits.OrderBy(x => x.Key).ToDictionary(),
                Characters = data.Characters.OrderBy(x => x.Key).ToDictionary(),
                SupplyConvoys = data.SupplyConvoys.OrderBy(x => x.Key).ToDictionary(),
                MessageCarriers = data.MessageCarriers.OrderBy(x => x.Key).ToDictionary(),
                Wars = data.Wars.OrderBy(x => x.Key).ToDictionary(),
                Battlefields = data.Battlefields.OrderBy(x => x.Key).ToDictionary()
            }, RuntimeStateJsonOptions),
            Visibility = visibilityLedger.Capture(playerForceId, tileMap.Width, tileMap.Height),
            Date = new StrategySaveDate
            {
                Year = date.Year,
                Month = date.Month,
                Day = date.Day
            },
            Forces = [.. data.Forces.Values
                .Select(f => new StrategySaveForce
                {
                    Id = f.Id,
                    Money = f.Money,
                    Food = f.Food,
                    LordCharacterId = f.LordCharacterId,
                    Introduction = f.Introduction,
                    ActiveEffects = MapEffects(f.ActiveEffects),
                    Diplomacies = [.. f.Diplomacies
                        .Where(d => d.ViewEffects.Count > 0)
                        .Select(d => new StrategySaveDiplomacy
                        {
                            TargetForceId = d.TargetForceId,
                            ViewEffects = MapEffects(d.ViewEffects)
                        })]
                })
                .OrderBy(f => f.Id)],
            Strongholds = [.. data.Strongholds.Values
                .Select(s =>
                {
                    s.Agriculture ??= new StrongholdAgricultureState();
                    StrongholdMilitaryStatsHelper.Recalculate(s, data);
                    return new StrategySaveStronghold
                    {
                        Id = s.Id,
                        ForceId = s.ForceId,
                        LordId = s.LordId,
                        Population = s.Population,
                        Food = s.ForceActor.Food,
                        Money = s.ForceActor.Money,
                        GarrisonSoldiers = s.ForceActor.Soldier,
                        MilitiaSoldiers = s.ForceActor.Soldier,
                        Scale = s.Scale,
                        Introduction = s.Introduction,
                        ActiveEffects = MapEffects(s.ActiveEffects),
                        Agriculture = new StrategySaveAgriculture
                        {
                            EarlyCycleProgressBp = s.Agriculture.EarlyCycleProgressBp,
                            LateCycleProgressBp = s.Agriculture.LateCycleProgressBp,
                            ThirdCycleProgressBp = s.Agriculture.ThirdCycleProgressBp,
                            EarlyCycleProgressCapBp = s.Agriculture.EarlyCycleProgressCapBp,
                            LateCycleProgressCapBp = s.Agriculture.LateCycleProgressCapBp,
                            ThirdCycleProgressCapBp = s.Agriculture.ThirdCycleProgressCapBp,
                            KnowsDoubleCrop = s.Agriculture.KnowsDoubleCrop,
                            KnowsTripleCrop = s.Agriculture.KnowsTripleCrop
                        }
                    };
                })
                .OrderBy(s => s.Id)],
            Units = [.. data.Units.Values
                .Select(u =>
                {
                    var route = new List<StrategySavePoint>
                    {
                        new() { X = u.Location.X, Y = u.Location.Y }
                    };
                    foreach (var p in u.ActionTarget.RoutePoints)
                        route.Add(new StrategySavePoint { X = p.X, Y = p.Y });

                    return new StrategySaveUnit
                    {
                        Id = u.Id,
                        ForceId = u.ForceId,
                        X = u.Location.X,
                        Y = u.Location.Y,
                        Soldiers = u.Soldier,
                        Food = u.Food,
                        Ap = u.Ap,
                        Status = u.Status.ToString(),
                        Directive = u.Directive.ToString(),
                        Route = route
                    };
                })
                .OrderBy(u => u.Id)],
            Characters = [.. data.Characters.Values
                .Select(MapCharacterSave)
                .Where(c => c is not null)
                .Cast<StrategySaveCharacter>()
                .OrderBy(c => c.Id)]
        };
    }

    /// <summary>将存档覆盖到已加载剧本世界（先 LoadScenario 再 Apply）。</summary>
    public static void Apply(StrategySaveDocument save, GameWorld world)
    {
        if (save.FormatVersion >= 2)
        {
            if (save.RuntimeState is not { ValueKind: JsonValueKind.Object } runtimeState
                || !TryRestoreRuntimeState(runtimeState, world))
                throw new InvalidOperationException("完整运行时存档损坏，不能降级为旧格式恢复。");
            return;
        }

        var data = world.GameData;

        data.GameDate = new Domain.Types.GameDate(save.Date.Year, save.Date.Month, save.Date.Day);

        if (save.SimulationSeed is int seed)
            data.SimulationSeed = seed;

        foreach (var forceSave in save.Forces)
        {
            if (!data.Forces.TryGetValue(forceSave.Id, out var force))
                continue;

            force.Money = forceSave.Money;
            force.Food = forceSave.Food;
            if (forceSave.LordCharacterId is int lordId)
                force.LordCharacterId = lordId;
            if (forceSave.Introduction is not null)
                force.Introduction = forceSave.Introduction;
            if (forceSave.ActiveEffects is { Count: > 0 })
                force.ActiveEffects = RestoreEffects(forceSave.ActiveEffects);
            if (forceSave.Diplomacies is { Count: > 0 })
            {
                foreach (var dipSave in forceSave.Diplomacies)
                {
                    var diplomacy = force.Diplomacies.FirstOrDefault(d => d.TargetForceId == dipSave.TargetForceId);
                    if (diplomacy is null || dipSave.ViewEffects is not { Count: > 0 })
                        continue;

                    diplomacy.ViewEffects = RestoreEffects(dipSave.ViewEffects);
                }
            }

            ForceIntelHelper.SyncMilitaryCaches(force, data);
        }

        foreach (var shSave in save.Strongholds)
        {
            if (!data.Strongholds.TryGetValue(shSave.Id, out var stronghold))
                continue;

            stronghold.ForceId = shSave.ForceId;
            stronghold.LordId = shSave.LordId;
            stronghold.Population = shSave.Population;
            stronghold.ForceActor.Food = shSave.Food;
            stronghold.ForceActor.Money = shSave.Money;
            stronghold.ForceActor.Soldier = shSave.MilitiaSoldiers > 0
                ? shSave.MilitiaSoldiers
                : shSave.GarrisonSoldiers;
            if (shSave.Scale is >= 1 and <= 30)
                stronghold.Scale = shSave.Scale;
            if (shSave.Introduction is not null)
                stronghold.Introduction = shSave.Introduction;
            if (shSave.ActiveEffects is { Count: > 0 })
                stronghold.ActiveEffects = RestoreEffects(shSave.ActiveEffects);

            if (shSave.Agriculture is { } agriSave)
            {
                stronghold.Agriculture ??= new StrongholdAgricultureState();
                stronghold.Agriculture.EarlyCycleProgressBp = agriSave.EarlyCycleProgressBp;
                stronghold.Agriculture.LateCycleProgressBp = agriSave.LateCycleProgressBp;
                stronghold.Agriculture.ThirdCycleProgressBp = agriSave.ThirdCycleProgressBp;
                stronghold.Agriculture.EarlyCycleProgressCapBp = agriSave.EarlyCycleProgressCapBp;
                stronghold.Agriculture.LateCycleProgressCapBp = agriSave.LateCycleProgressCapBp;
                stronghold.Agriculture.ThirdCycleProgressCapBp = agriSave.ThirdCycleProgressCapBp;
                stronghold.Agriculture.KnowsDoubleCrop = agriSave.KnowsDoubleCrop;
                stronghold.Agriculture.KnowsTripleCrop = agriSave.KnowsTripleCrop;
            }

            ResetGarrisonComposition(stronghold, data);
            StrongholdMilitaryBootstrapHelper.InitializeGarrisonComposition(stronghold, data);
            StrongholdMilitaryStatsHelper.Recalculate(stronghold, data);
            StrongholdMaintenanceHelper.Sync(stronghold, world.GameMasterData);
        }

        if (save.Characters is { Count: > 0 })
        {
            foreach (var charSave in save.Characters)
            {
                if (!data.Characters.TryGetValue(charSave.Id, out var character))
                    continue;

                character.Loyalty = charSave.Loyalty;
                if (charSave.Money is int money)
                    character.Money = money;
                if (charSave.Ap is int ap)
                    character.Ap = ap;
                if (charSave.Hp is int hp)
                    character.Hp = hp;
                if (charSave.Emotion is int emotion)
                    character.Emotion = emotion;
                if (charSave.ServiceDate is { } serviceDate)
                    character.ServiceDate = ToGameDate(serviceDate);
                if (charSave.Relationships is { Count: > 0 })
                    character.Relationships = RestoreRelationships(character.Id, charSave.Relationships);
                if (charSave.ActiveEffects is { Count: > 0 })
                    character.ActiveEffects = RestoreEffects(charSave.ActiveEffects);
                if (charSave.IntelTasks is { Count: > 0 })
                {
                    character.IntelTasks = charSave.IntelTasks
                        .Select(t => new CharacterIntelTask
                        {
                            TaskCategory = t.TaskCategory,
                            Name = t.Name,
                            Target = t.Target,
                            Status = t.Status,
                            Remaining = t.Remaining
                        })
                        .ToList();
                }

                if (charSave.DiplomacyMission is { } missionSave)
                {
                    character.DiplomacyMission = new CharacterDiplomacyMission
                    {
                        Action = missionSave.Action,
                        TargetForceId = missionSave.TargetForceId,
                        RemainingDays = missionSave.RemainingDays,
                        SuccessChancePercent = missionSave.SuccessChancePercent,
                        PeaceTerms = missionSave.Action == "Peace"
                            ? new PeaceSettlementTerms
                            {
                                CededStrongholdIds = missionSave.CededStrongholdIds ?? [],
                                ReparationsMoney = missionSave.ReparationsMoney,
                                DemandOuterVassalage = missionSave.DemandOuterVassalage,
                            }
                            : null,
                    };
                }

                if (charSave.ForceStatus is { } forceStatus
                    && Enum.TryParse<Character.CharacterForceStatus>(forceStatus, ignoreCase: true, out var parsedStatus))
                {
                    character.ForceStatus = parsedStatus;
                }
            }
        }

        foreach (var unitSave in save.Units)
        {
            if (!data.Units.TryGetValue(unitSave.Id, out var unit))
                continue;

            unit.ForceId = unitSave.ForceId;
            unit.Location = new Point3(unitSave.X, unitSave.Y);
            unit.Soldier = unitSave.Soldiers;
            unit.Food = unitSave.Food;
            unit.Ap = unitSave.Ap;

            if (Enum.TryParse<UnitStatus>(unitSave.Status, ignoreCase: true, out var status))
                unit.Status = status;

            // 业务：恢复单位方针（移动/支援/攻击等）
            if (Enum.TryParse<UnitDirective>(unitSave.Directive, ignoreCase: true, out var directive))
                unit.Directive = directive;

            unit.ActionTarget.RoutePoints.Clear();
            foreach (var point in unitSave.Route.Skip(1))
                unit.ActionTarget.RoutePoints.Enqueue(new Point2(point.X, point.Y));
        }

        // 存档坐标覆盖实体后，必须同步重建地图格索引；否则显示位置与战斗/占格查询会分裂。
        world.GameMapData.Units.Clear();
        foreach (var unit in data.Units.Values.Where(unit => !unit.InStronghold))
            MapLocationActions.RegisterUnit(world, unit);
    }

    private static bool TryRestoreRuntimeState(JsonElement runtimeState, GameWorld world)
    {
        GameData? restored;
        try
        {
            restored = runtimeState.Deserialize<GameData>(RuntimeStateJsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }

        if (restored is null
            || restored.Forces is null
            || restored.Strongholds is null
            || restored.Units is null
            || restored.SubUnits is null
            || restored.Characters is null
            || restored.SupplyConvoys is null
            || restored.MessageCarriers is null
            || restored.Wars is null
            || restored.Battlefields is null)
            return false;

        var data = world.GameData;
        data.GameDate = restored.GameDate;
        data.SimulationSeed = restored.SimulationSeed;
        data.NextBattlefieldId = restored.NextBattlefieldId;

        ReplaceDictionary(data.Forces, restored.Forces);
        ReplaceDictionary(data.Strongholds, restored.Strongholds);
        ReplaceDictionary(data.Units, restored.Units);
        ReplaceDictionary(data.SubUnits, restored.SubUnits);
        ReplaceDictionary(data.Characters, restored.Characters);
        ReplaceDictionary(data.SupplyConvoys, restored.SupplyConvoys);
        ReplaceDictionary(data.MessageCarriers, restored.MessageCarriers);
        ReplaceDictionary(data.Wars, restored.Wars);
        ReplaceDictionary(data.Battlefields, restored.Battlefields);

        RebuildMapIndexes(world);
        return true;
    }

    private static void ReplaceDictionary<TKey, TValue>(
        Dictionary<TKey, TValue> target,
        IReadOnlyDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        target.Clear();
        foreach (var (key, value) in source)
            target[key] = value;
    }

    private static void RebuildMapIndexes(GameWorld world)
    {
        world.GameMapData.Units.Clear();
        foreach (var unit in world.GameData.Units.Values.Where(unit => !unit.InStronghold))
            MapLocationActions.RegisterUnit(world, unit);

        world.GameMapData.Characters.Clear();
        foreach (var character in world.GameData.Characters.Values)
            MapLocationActions.RegisterCharacter(world, character);

        world.GameMapData.Strongholds.Clear();
        foreach (var stronghold in world.GameData.Strongholds.Values)
            MapLocationActions.RegisterStronghold(world, stronghold);
    }

    private sealed class GameDateJsonConverter : JsonConverter<GameDate>
    {
        public override GameDate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => GameDate.FromTotalPhases(reader.GetInt32());

        public override void Write(Utf8JsonWriter writer, GameDate value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.TotalPhases);
    }

    private sealed class Point2JsonConverter : JsonConverter<Point2>
    {
        public override Point2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            return new Point2(ReadCoordinate(root, "x"), ReadCoordinate(root, "y"));
        }

        public override void Write(Utf8JsonWriter writer, Point2 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteEndObject();
        }
    }

    private sealed class Point3JsonConverter : JsonConverter<Point3>
    {
        public override Point3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            return new Point3(
                ReadCoordinate(root, "x"),
                ReadCoordinate(root, "y"),
                ReadCoordinate(root, "z"));
        }

        public override void Write(Utf8JsonWriter writer, Point3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteNumber("z", value.Z);
            writer.WriteEndObject();
        }
    }

    private static int ReadCoordinate(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return property.Value.GetInt32();
        }

        return 0;
    }

    private static void ResetGarrisonComposition(Stronghold stronghold, GameData gameData)
    {
        foreach (var subId in stronghold.ForceActor.SubUnitIds.ToList())
        {
            if (gameData.SubUnits.TryGetValue(subId, out var sub) && sub.UnitId == 0)
                gameData.SubUnits.Remove(subId);
        }

        stronghold.ForceActor.SubUnitIds.Clear();
    }

    private static StrategySaveCharacter? MapCharacterSave(Character character)
    {
        if (character.Loyalty == 50
            && character.ServiceDate.Year <= 0
            && character.Relationships.Count == 0
            && character.ActiveEffects.Count == 0
            && character.IntelTasks.Count == 0
            && character.DiplomacyMission is null
            && character.ForceStatus == Character.CharacterForceStatus.Idle)
        {
            return null;
        }

        return new StrategySaveCharacter
        {
            Id = character.Id,
            Money = character.Money,
            Ap = character.Ap,
            Hp = character.Hp,
            Emotion = character.Emotion,
            Loyalty = character.Loyalty,
            ServiceDate = character.ServiceDate.Year > 0 ? ToSaveDate(character.ServiceDate) : null,
            Relationships = character.Relationships.Count == 0
                ? null
                : [.. character.Relationships.Select(r => new StrategySaveCharacterRelationship
                {
                    TargetCharacterId = r.TargetCharacterId,
                    Relationship = r.Relationship,
                    Trust = r.Trust,
                    ViewEffects = r.ViewEffects.Count == 0 ? null : MapEffects(r.ViewEffects)
                })],
            ActiveEffects = MapEffects(character.ActiveEffects),
            IntelTasks = character.IntelTasks.Count == 0
                ? null
                : [.. character.IntelTasks.Select(t => new StrategySaveIntelTask
                {
                    TaskCategory = t.TaskCategory,
                    Name = t.Name,
                    Target = t.Target,
                    Status = t.Status,
                    Remaining = t.Remaining
                })],
            DiplomacyMission = character.DiplomacyMission is null
                ? null
                : new StrategySaveDiplomacyMission
                {
                    Action = character.DiplomacyMission.Action,
                    TargetForceId = character.DiplomacyMission.TargetForceId,
                    RemainingDays = character.DiplomacyMission.RemainingDays,
                    SuccessChancePercent = character.DiplomacyMission.SuccessChancePercent,
                    CededStrongholdIds = character.DiplomacyMission.PeaceTerms?.CededStrongholdIds,
                    ReparationsMoney = character.DiplomacyMission.PeaceTerms?.ReparationsMoney ?? 0,
                    DemandOuterVassalage = character.DiplomacyMission.PeaceTerms?.DemandOuterVassalage ?? false,
                },
            ForceStatus = character.ForceStatus == Character.CharacterForceStatus.Idle
                ? null
                : character.ForceStatus.ToString(),
        };
    }

    private static List<StrategySaveEntityEffect>? MapEffects(IReadOnlyList<EntityEffect> effects)
    {
        if (effects.Count == 0)
            return null;

        return [.. effects.Select(e => new StrategySaveEntityEffect
        {
            Id = e.Id,
            Name = e.Name,
            TargetStat = e.TargetStat.ToString(),
            Magnitude = e.Magnitude,
            Duration = e.Duration.ToString(),
            Description = e.Description,
            ExpiresOn = e.ExpiresOn is { Year: > 0 } expiresOn ? ToSaveDate(expiresOn) : null
        })];
    }

    private static List<EntityEffect> RestoreEffects(IReadOnlyList<StrategySaveEntityEffect> effects)
        => [.. effects.Select(e => new EntityEffect
        {
            Id = e.Id,
            Name = e.Name,
            TargetStat = Enum.TryParse<EffectTargetStat>(e.TargetStat, ignoreCase: true, out var targetStat)
                ? targetStat
                : EffectTargetStat.Relationship,
            Magnitude = e.Magnitude,
            Duration = Enum.TryParse<EffectDurationKind>(e.Duration, ignoreCase: true, out var duration)
                ? duration
                : EffectDurationKind.Permanent,
            Description = e.Description,
            ExpiresOn = e.ExpiresOn is { } expiresOn ? ToGameDate(expiresOn) : null
        })];

    private static List<CharacterRelationship> RestoreRelationships(
        int ownerCharacterId,
        IReadOnlyList<StrategySaveCharacterRelationship> relationships)
        => [.. relationships.Select(r => new CharacterRelationship
        {
            OwnerCharacterId = ownerCharacterId,
            TargetCharacterId = r.TargetCharacterId,
            Relationship = r.Relationship,
            Trust = r.Trust,
            ViewEffects = r.ViewEffects is { Count: > 0 }
                ? RestoreEffects(r.ViewEffects)
                : []
        })];

    private static StrategySaveDate ToSaveDate(GameDate date)
        => new() { Year = date.Year, Month = date.Month, Day = date.Day };

    private static GameDate ToGameDate(StrategySaveDate date)
        => new(date.Year, date.Month, date.Day);
}
