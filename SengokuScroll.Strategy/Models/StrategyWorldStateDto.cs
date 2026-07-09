using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.World;

namespace SengokuScroll.Strategy.Models;

/// <summary>策略世界状态 API 响应（M2-a：供前端地图渲染）。</summary>
public sealed record StrategyWorldStateDto
{
    public required string ScenarioId { get; init; }

    public required StrategyMapStateDto Map { get; init; }

    public required StrategyDateStateDto Date { get; init; }

    public required IReadOnlyList<StrategyForceStateDto> Forces { get; init; }

    public required IReadOnlyList<StrategyStrongholdStateDto> Strongholds { get; init; }

    public required IReadOnlyList<StrategyUnitStateDto> Units { get; init; }

    public required IReadOnlyList<StrategySupplyConvoyStateDto> SupplyConvoys { get; init; }

    public required IReadOnlyList<StrategyMessengerStateDto> Messengers { get; init; }

    /// <summary>玩家势力视角外交（目标势力 + 关系）。</summary>
    public required IReadOnlyList<StrategyDiplomacyStateDto> Diplomacies { get; init; }

    /// <summary>玩家势力 Id。</summary>
    public required int PlayerForceId { get; init; }

    /// <summary>当主摘要（方针/战报信使出发点）。</summary>
    public required StrategyLordStateDto Lord { get; init; }
}

/// <summary>地图尺寸与名称。</summary>
public sealed record StrategyMapStateDto
{
    public required string Name { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    /// <summary>道路格（region 层；typeId&gt;0）。</summary>
    public required IReadOnlyList<StrategyRoadCellDto> RoadCells { get; init; }

    /// <summary>逐格地形名（行优先，长度 = Width × Height）。</summary>
    public required IReadOnlyList<string> TileTerrainNames { get; init; }

    /// <summary>逐格政治区域名（行优先；无区域为 null）。</summary>
    public required IReadOnlyList<string?> TileRegionNames { get; init; }

    /// <summary>地图地标。</summary>
    public required IReadOnlyList<StrategyMapLandmarkDto> Landmarks { get; init; }
}

/// <summary>地图地标 DTO。</summary>
public sealed record StrategyMapLandmarkDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }
}

/// <summary>地图道路格。</summary>
public sealed record StrategyRoadCellDto
{
    public required int X { get; init; }

    public required int Y { get; init; }

    public required int TypeId { get; init; }

    public required string TypeName { get; init; }

    /// <summary>道路等级 Id（与 typeId 一致）。</summary>
    public required int Level { get; init; }

    /// <summary>移动力加成（AP 减免）。</summary>
    public required int SpeedBonus { get; init; }

    /// <summary>该道路格移动力消耗（AP）。</summary>
    public required int MovementCost { get; init; }
}

/// <summary>当前游戏内日期。</summary>
public sealed record StrategyDateStateDto
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public required int Day { get; init; }
}

/// <summary>势力摘要。</summary>
public sealed record StrategyForceStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int Food { get; init; }

    public required int Money { get; init; }

    /// <summary>Independence | InnerVassal | OuterVassal。</summary>
    public required string Status { get; init; }

    /// <summary>宗主势力 Id；内藩/外藩时有效。</summary>
    public int? SuzerainForceId { get; init; }
}

/// <summary>玩家视角外交摘要。</summary>
public sealed record StrategyDiplomacyStateDto
{
    public required int TargetForceId { get; init; }

    /// <summary>Neutral | Allied | Enemy。</summary>
    public required string Relation { get; init; }
}

/// <summary>据点摘要。</summary>
public sealed record StrategyStrongholdStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int Food { get; init; }

    public required int Population { get; init; }

    /// <summary>领主角色 Id；0 = 当主直辖。</summary>
    public required int LordId { get; init; }

    /// <summary>是否当主直辖（LordId=0）。</summary>
    public required bool IsDirectRule { get; init; }

    /// <summary>领主显示名；直辖时为势力当主名。</summary>
    public required string LordName { get; init; }

    public string? MayorName { get; init; }

    public required int Morale { get; init; }

    public required int Training { get; init; }

    public required string CultureName { get; init; }

    public required string ReligionName { get; init; }

    public required int Money { get; init; }

    public required byte PollTaxRate { get; init; }

    public required byte AgricultureTaxRate { get; init; }

    public required byte CommerceTaxRate { get; init; }

    public required byte TariffTaxRate { get; init; }
}

/// <summary>军事单位摘要。</summary>
public sealed record StrategyUnitStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int Soldiers { get; init; }

    public required int Food { get; init; }

    public required int Ap { get; init; }

    public required int Movement { get; init; }

    public required string Status { get; init; }

    /// <summary>当前战斗/行动方针（UnitDirective 枚举名）。</summary>
    public required string Directive { get; init; }

    /// <summary>剩余移动路径（含当前格），无路径时为空。</summary>
    public required IReadOnlyList<StrategyMapPointDto> Route { get; init; }

    public string? CommanderName { get; init; }

    public int? CommanderId { get; init; }

    public required int Morale { get; init; }

    public required int Training { get; init; }

    public required string CultureName { get; init; }

    public required string ReligionName { get; init; }

    public required int Money { get; init; }

    /// <summary>兵种/备队构成（出征编组时确定；无则空列表）。</summary>
    public required IReadOnlyList<StrategySubUnitStateDto> Composition { get; init; }

    /// <summary>补给三态：Sufficient / Strained / CutOff。</summary>
    public required string SupplyStatus { get; init; }

    /// <summary>携带粮预计可维持天数。</summary>
    public required int FoodDaysRemaining { get; init; }

    /// <summary>在途运输队补给摘要。</summary>
    public required IReadOnlyList<StrategyInTransitSupplyDto> InTransitSupplies { get; init; }
}

/// <summary>单位在途补给摘要。</summary>
public sealed record StrategyInTransitSupplyDto
{
    public required int ConvoyId { get; init; }

    public required int CargoFoodGo { get; init; }

    public required int EstimatedDays { get; init; }

    public required bool IsDeceived { get; init; }

    public string? OriginStrongholdName { get; init; }
}

/// <summary>单位内子编制（兵种/备队）摘要。</summary>
public sealed record StrategySubUnitStateDto
{
    public required int Id { get; init; }

    public required int TypeId { get; init; }

    public required string TypeName { get; init; }

    public required int Soldiers { get; init; }

    /// <summary>占该单位总兵数百分比（0–100）。</summary>
    public required int RatioPercent { get; init; }

    public int? CommanderId { get; init; }

    public string? CommanderName { get; init; }
}

/// <summary>地图格点坐标。</summary>
public sealed record StrategyMapPointDto
{
    public required int X { get; init; }

    public required int Y { get; init; }
}

/// <summary>路径预览响应。</summary>
public sealed record StrategyPathPreviewDto
{
    public required IReadOnlyList<StrategyMapPointDto> Points { get; init; }
}

/// <summary>瞬间战战前预览（M3-a）。</summary>
public sealed record StrategyBattlePreviewDto
{
    public required int AttackerUnitId { get; init; }

    public required int DefenderUnitId { get; init; }

    public required int TargetX { get; init; }

    public required int TargetY { get; init; }

    public required int AttackerWinRatePercent { get; init; }

    public required int AttackerSoldiers { get; init; }

    public required int DefenderSoldiers { get; init; }

    public required string DefenderName { get; init; }

    public required int EstimatedAttackerLossMin { get; init; }

    public required int EstimatedAttackerLossMax { get; init; }

    public required int EstimatedDefenderLossMin { get; init; }

    public required int EstimatedDefenderLossMax { get; init; }

    public required int ResolutionSeed { get; init; }
}

/// <summary>瞬间战结算结果（M3-a）。</summary>
public sealed record StrategyBattleResultDto
{
    public required bool AttackerWon { get; init; }

    public required int AttackerUnitId { get; init; }

    public required int DefenderUnitId { get; init; }

    public required string AttackerName { get; init; }

    public required string DefenderName { get; init; }

    public required int AttackerSoldiersBefore { get; init; }

    public required int DefenderSoldiersBefore { get; init; }

    public required int AttackerCasualties { get; init; }

    public required int DefenderCasualties { get; init; }

    public required int AttackerSoldiersAfter { get; init; }

    public required int DefenderSoldiersAfter { get; init; }

    public required int AttackerWinRatePercent { get; init; }

    public required int ResolutionSeed { get; init; }

    public required int ResolutionRoll { get; init; }

    public required IReadOnlyList<StrategyBattleLogEntryDto> LogEntries { get; init; }
}

/// <summary>战斗过程日志条目。</summary>
public sealed record StrategyBattleLogEntryDto
{
    public required int Order { get; init; }

    /// <summary>attacker / defender / system</summary>
    public required string Side { get; init; }

    public required string Phase { get; init; }

    public required string Message { get; init; }
}

/// <summary>瞬间战执行响应：世界状态 + 战斗结果。</summary>
public sealed record StrategyInstantBattleResponseDto
{
    public required StrategyWorldStateDto State { get; init; }

    public required StrategyBattleResultDto Result { get; init; }
}

/// <summary>方针变更响应（M3-b）。</summary>
public sealed record StrategyPolicyChangeResponseDto
{
    public required StrategyWorldStateDto State { get; init; }

    /// <summary>AppliedImmediately | MessengerDispatched</summary>
    public required string Outcome { get; init; }
}

/// <summary>日推进响应：含当回合已结算战斗（M3-b）。</summary>
public sealed record StrategyAdvanceDayResponseDto
{
    public required StrategyWorldStateDto State { get; init; }

    /// <summary>本日推进期间已结算、待 UI 弹出的战报。</summary>
    public required IReadOnlyList<StrategyBattleResultDto> ResolvedBattles { get; init; }

    /// <summary>本日推进期间信使抵达等事件，供左上角消息栏展示。</summary>
    public required IReadOnlyList<StrategyEventDto> Events { get; init; }
}

/// <summary>当主位置摘要。</summary>
public sealed record StrategyLordStateDto
{
    public required string Name { get; init; }

    /// <summary>领兵时的单位 Id；否则为 null。</summary>
    public int? UnitId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }
}

/// <summary>运输队摘要（非军事单位，情报字段与 <see cref="StrategyUnitStateDto"/> 对齐）。</summary>
public sealed record StrategySupplyConvoyStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    /// <summary>恒为 false（非军事单位）。</summary>
    public required bool IsMilitary { get; init; }

    public string? CommanderName { get; init; }

    public int? CommanderId { get; init; }

    /// <summary>人夫 + 护卫合计人数。</summary>
    public required int Soldiers { get; init; }

    public required int PorterCount { get; init; }

    public required int EscortSoldierCount { get; init; }

    /// <summary>载粮（合），对应单位 <see cref="StrategyUnitStateDto.Food"/>。</summary>
    public required int Food { get; init; }

    public required int Ap { get; init; }

    public required int Movement { get; init; }

    public required string Status { get; init; }

    public required string Directive { get; init; }

    public required IReadOnlyList<StrategyMapPointDto> Route { get; init; }

    public required int Morale { get; init; }

    public required int Training { get; init; }

    public required string CultureName { get; init; }

    public required string ReligionName { get; init; }

    public required int Money { get; init; }

    public required int TargetUnitId { get; init; }

    public string? TargetUnitName { get; init; }

    public required int OriginStrongholdId { get; init; }

    public string? OriginStrongholdName { get; init; }

    /// <summary>是否处于卸粮后返回出发据点的返程阶段。</summary>
    public required bool IsReturningToOrigin { get; init; }

    /// <summary>兼容旧字段；等同 <see cref="Food"/>。</summary>
    public required int CargoFoodGo { get; init; }
}

/// <summary>信使摘要（非军事单位，情报字段与兵队对齐；编制为 NPC 传令兵/护卫，无总将）。</summary>
public sealed record StrategyMessengerStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    /// <summary>恒为 false（非军事单位）。</summary>
    public required bool IsMilitary { get; init; }

    /// <summary>传令兵 + 护卫合计人数。</summary>
    public required int Soldiers { get; init; }

    public required int CourierCount { get; init; }

    public required int EscortSoldierCount { get; init; }

    public required int Ap { get; init; }

    public required int Movement { get; init; }

    public required string Status { get; init; }

    public required string PayloadType { get; init; }

    public required string Directive { get; init; }

    public required IReadOnlyList<StrategyMapPointDto> Route { get; init; }

    public required int Morale { get; init; }

    public required int Training { get; init; }

    public required string CultureName { get; init; }

    public required string ReligionName { get; init; }

    public required int Money { get; init; }

    public required int TargetUnitId { get; init; }

    public string? TargetUnitName { get; init; }

    public required int OriginStrongholdId { get; init; }

    public string? OriginStrongholdName { get; init; }

    /// <summary>PolicyChange 时在途待生效方针。</summary>
    public string? PendingDirective { get; init; }
}

/// <summary>将 <see cref="GameWorld"/> 映射为 API DTO。</summary>
public static class StrategyWorldStateMapper
{
    public static StrategyWorldStateDto ToDto(GameWorld world, string scenarioId, StrategyScenarioMeta meta)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var date = world.GameData.GameDate;
        var lordLocation = StrategyLordHelper.ResolveLocation(world.GameData, meta);

        return new StrategyWorldStateDto
        {
            ScenarioId = scenarioId,
            PlayerForceId = meta.PlayerForceId,
            Lord = new StrategyLordStateDto
            {
                Name = meta.LordName,
                UnitId = meta.LordUnitId,
                X = lordLocation.X,
                Y = lordLocation.Y
            },
            Map = new StrategyMapStateDto
            {
                Name = world.GameMapMasterData.Name,
                Width = tileMap.Width,
                Height = tileMap.Height,
                RoadCells = MapRoadCells(tileMap, world.GameMapMasterData.Roads),
                TileTerrainNames = MapTileTerrainNames(tileMap, world.GameMapMasterData.Terrains),
                TileRegionNames = MapTileRegionNames(tileMap, world.GameMapMasterData),
                Landmarks = MapLandmarks(world.GameMapMasterData.StrongholdPoints)
            },
            Date = new StrategyDateStateDto
            {
                Year = date.Year,
                Month = date.Month,
                Day = date.Day
            },
            Forces = world.GameData.Forces.Values
                .Select(f => new StrategyForceStateDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Food = f.Food,
                    Money = f.Money,
                    Status = f.Status.ToString(),
                    SuzerainForceId = f.SuzerainForceId
                })
                .OrderBy(f => f.Id)
                .ToList(),
            Strongholds = world.GameData.Strongholds.Values
                .Select(s => MapStronghold(s, meta, world.GameData))
                .OrderBy(s => s.Id)
                .ToList(),
            Units = world.GameData.Units.Values
                .Select(u => MapUnit(u, meta, world.GameData))
                .OrderBy(u => u.Id)
                .ToList(),
            SupplyConvoys = world.GameData.SupplyConvoys.Values
                .Select(c => MapConvoy(c, world.GameData))
                .OrderBy(c => c.Id)
                .ToList(),
            Messengers = world.GameData.Messengers.Values
                .Select(m => MapMessenger(m, world.GameData))
                .OrderBy(m => m.Id)
                .ToList(),
            Diplomacies = MapPlayerDiplomacies(meta.PlayerForceId, world.GameData)
        };
    }

    private static IReadOnlyList<StrategyDiplomacyStateDto> MapPlayerDiplomacies(
        int playerForceId,
        GameData gameData)
    {
        if (!gameData.Forces.TryGetValue(playerForceId, out var playerForce))
            return [];

        return playerForce.Diplomacies
            .Select(d => new StrategyDiplomacyStateDto
            {
                TargetForceId = d.TargetForceId,
                Relation = d.Relation.ToString()
            })
            .OrderBy(d => d.TargetForceId)
            .ToList();
    }

    private static StrategyMessengerStateDto MapMessenger(Messenger m, GameData gameData)
    {
        gameData.Units.TryGetValue(m.TargetUnitId, out var targetUnit);
        gameData.Strongholds.TryGetValue(m.SourceStrongholdId, out var origin);

        var route = new List<StrategyMapPointDto>
        {
            new() { X = m.Location.X, Y = m.Location.Y }
        };
        foreach (var point in m.RoutePoints)
            route.Add(new StrategyMapPointDto { X = point.X, Y = point.Y });

        var directive = m.PayloadType switch
        {
            MessengerPayloadType.PolicyChange => "PolicyChange",
            MessengerPayloadType.BattleReport => "BattleReport",
            MessengerPayloadType.FalseIntelligence => "FalseIntelligence",
            MessengerPayloadType.StrategicOrder => "StrategicOrder",
            _ => m.PayloadType.ToString()
        };

        return new StrategyMessengerStateDto
        {
            Id = m.Id,
            Name = m.Name,
            ForceId = m.ForceId,
            X = m.Location.X,
            Y = m.Location.Y,
            IsMilitary = false,
            Soldiers = m.CourierCount + m.EscortSoldierCount,
            CourierCount = m.CourierCount,
            EscortSoldierCount = m.EscortSoldierCount,
            Ap = Math.Max(m.RoutePoints.Count, 1),
            Movement = LogisticsConstants.MessengerDailyAp,
            Status = m.Status.ToString(),
            PayloadType = m.PayloadType.ToString(),
            Directive = directive,
            Route = route,
            Morale = 80,
            Training = 70,
            CultureName = "日本",
            ReligionName = "神道",
            Money = 0,
            TargetUnitId = m.TargetUnitId,
            TargetUnitName = targetUnit?.Name,
            OriginStrongholdId = m.SourceStrongholdId,
            OriginStrongholdName = origin?.Name,
            PendingDirective = m.PendingDirective?.ToString()
        };
    }

    private static StrategySupplyConvoyStateDto MapConvoy(SupplyConvoy c, GameData gameData)
    {
        string? commanderName = null;
        if (c.LeaderId > 0 && gameData.Characters.TryGetValue(c.LeaderId, out var commander))
            commanderName = commander.Name;

        gameData.Units.TryGetValue(c.TargetUnitId, out var targetUnit);
        gameData.Strongholds.TryGetValue(c.OriginStrongholdId, out var origin);

        var route = new List<StrategyMapPointDto>
        {
            new() { X = c.Location.X, Y = c.Location.Y }
        };
        foreach (var point in c.RoutePoints)
            route.Add(new StrategyMapPointDto { X = point.X, Y = point.Y });

        var directive = c.IsReturningToOrigin ? "Retreat" : "Support";

        return new StrategySupplyConvoyStateDto
        {
            Id = c.Id,
            Name = c.Name,
            ForceId = c.ForceId,
            X = c.Location.X,
            Y = c.Location.Y,
            IsMilitary = false,
            CommanderName = commanderName,
            CommanderId = c.LeaderId > 0 ? c.LeaderId : null,
            Soldiers = c.PorterCount + c.EscortSoldierCount,
            PorterCount = c.PorterCount,
            EscortSoldierCount = c.EscortSoldierCount,
            Food = c.CargoFoodGo,
            CargoFoodGo = c.CargoFoodGo,
            Ap = c.Ap,
            Movement = c.Movement > 0 ? c.Movement : LogisticsConstants.ConvoyDailyAp,
            Status = c.Status.ToString(),
            Directive = directive,
            Route = route,
            Morale = 75,
            Training = 65,
            CultureName = "日本",
            ReligionName = "神道",
            Money = 0,
            TargetUnitId = c.TargetUnitId,
            TargetUnitName = targetUnit?.Name,
            OriginStrongholdId = c.OriginStrongholdId,
            OriginStrongholdName = origin?.Name,
            IsReturningToOrigin = c.IsReturningToOrigin
        };
    }

    private static StrategyUnitStateDto MapUnit(Unit u, StrategyScenarioMeta meta, GameData gameData)
    {
        meta.Intel.Units.TryGetValue(u.Id, out var overlay);
        var commander = overlay?.CommanderName;
        if (u.LeaderId > 0 && gameData.Characters.TryGetValue(u.LeaderId, out var commanderCharacter))
            commander = commanderCharacter.Name;
        else if (string.IsNullOrWhiteSpace(commander) && meta.LordUnitId == u.Id)
            commander = meta.LordName;

        return new StrategyUnitStateDto
        {
            Id = u.Id,
            Name = u.Name,
            ForceId = u.ForceId,
            X = u.Location.X,
            Y = u.Location.Y,
            Soldiers = u.Soldier,
            Food = u.Food,
            Ap = u.Ap,
            Movement = u.Movement,
            Status = u.Status.ToString(),
            Directive = u.Directive.ToString(),
            Route = BuildUnitRoute(u),
            CommanderName = string.IsNullOrWhiteSpace(commander) ? null : commander,
            CommanderId = u.LeaderId > 0 ? u.LeaderId : null,
            Morale = u.Morale,
            Training = u.Training,
            CultureName = overlay?.CultureName ?? "日本",
            ReligionName = overlay?.ReligionName ?? "神道",
            Money = u.Money,
            Composition = MapUnitComposition(u, gameData),
            SupplyStatus = SupplyStatusEvaluator.EvaluateStatus(u, gameData),
            FoodDaysRemaining = SupplyStatusEvaluator.EstimateFoodDaysRemaining(u),
            InTransitSupplies = MapInTransitSupplies(u, gameData)
        };
    }

    private static IReadOnlyList<StrategyInTransitSupplyDto> MapInTransitSupplies(Unit u, GameData gameData)
    {
        return SupplyStatusEvaluator.GetInTransitSummaries(u, gameData)
            .Select(s =>
            {
                gameData.Strongholds.TryGetValue(s.OriginStrongholdId, out var origin);
                return new StrategyInTransitSupplyDto
                {
                    ConvoyId = s.ConvoyId,
                    CargoFoodGo = s.CargoFoodGo,
                    EstimatedDays = s.EstimatedDays,
                    IsDeceived = s.IsDeceived,
                    OriginStrongholdName = origin?.Name
                };
            })
            .ToList();
    }

    private static IReadOnlyList<StrategySubUnitStateDto> MapUnitComposition(Unit u, GameData gameData)
    {
        if (u.SubUnitIds.Count == 0)
            return [];

        var total = Math.Max(u.Soldier, 1);
        var rows = new List<StrategySubUnitStateDto>();

        foreach (var subUnitId in u.SubUnitIds)
        {
            if (!gameData.SubUnits.TryGetValue(subUnitId, out var subUnit))
                continue;

            string? commanderName = null;
            if (subUnit.LeaderId > 0 && gameData.Characters.TryGetValue(subUnit.LeaderId, out var commander))
                commanderName = commander.Name;

            var ratio = (int)Math.Round(subUnit.Soldier * 100.0 / total, MidpointRounding.AwayFromZero);

            rows.Add(new StrategySubUnitStateDto
            {
                Id = subUnit.Id,
                TypeId = subUnit.TypeId,
                TypeName = StrategyTroopTypes.ResolveName(subUnit.TypeId, subUnit.TypeName),
                Soldiers = subUnit.Soldier,
                RatioPercent = ratio,
                CommanderId = subUnit.LeaderId > 0 ? subUnit.LeaderId : null,
                CommanderName = commanderName
            });
        }

        return rows;
    }

    private static IReadOnlyList<StrategyRoadCellDto> MapRoadCells(
        TileMap tileMap,
        IReadOnlyDictionary<int, RoadDefinition> roads)
    {
        var cells = new List<StrategyRoadCellDto>();

        for (var y = 0; y < tileMap.Height; y++)
        {
            for (var x = 0; x < tileMap.Width; x++)
            {
                var p = new Common.Types.Point3(x, y);
                var typeId = tileMap.GetRegion(p);
                if (typeId == 0)
                    continue;

                roads.TryGetValue(typeId, out var roadDef);
                var movementCost = roadDef?.MovementCostOverride
                    ?? Math.Max(1, 2 - (roadDef?.SpeedBonus ?? 0));

                cells.Add(new StrategyRoadCellDto
                {
                    X = x,
                    Y = y,
                    TypeId = typeId,
                    TypeName = roadDef?.Name ?? $"道路#{typeId}",
                    Level = typeId,
                    SpeedBonus = roadDef?.SpeedBonus ?? 0,
                    MovementCost = movementCost
                });
            }
        }

        return cells;
    }

    private static IReadOnlyList<string> MapTileTerrainNames(
        TileMap tileMap,
        IReadOnlyDictionary<int, TerrainDefinition> terrains)
    {
        var names = new string[tileMap.Length];
        for (var y = 0; y < tileMap.Height; y++)
        {
            for (var x = 0; x < tileMap.Width; x++)
            {
                var terrainId = tileMap.GetTerrain(new Common.Types.Point3(x, y));
                names[y * tileMap.Width + x] = terrains.TryGetValue(terrainId, out var def)
                    ? def.Name
                    : $"地形#{terrainId}";
            }
        }

        return names;
    }

    private static IReadOnlyList<string?> MapTileRegionNames(TileMap tileMap, GameMapMasterData master)
    {
        var names = new string?[tileMap.Length];
        var grid = master.PoliticalRegionGrid;
        if (grid.Length != tileMap.Length)
            return names;

        for (var i = 0; i < grid.Length; i++)
        {
            var regionId = grid[i];
            if (regionId == 0)
                continue;

            names[i] = master.Regions.TryGetValue(regionId, out var def) ? def.Name : $"区域#{regionId}";
        }

        return names;
    }

    private static IReadOnlyList<StrategyMapLandmarkDto> MapLandmarks(
        IReadOnlyDictionary<int, StrongholdPoint> points)
        => points.Values
            .Select(p => new StrategyMapLandmarkDto
            {
                Id = p.Id,
                Name = p.Name,
                X = p.Location.X,
                Y = p.Location.Y
            })
            .OrderBy(p => p.Id)
            .ToList();

    private static StrategyStrongholdStateDto MapStronghold(Stronghold s, StrategyScenarioMeta meta, GameData gameData)
    {
        meta.Intel.Strongholds.TryGetValue(s.Id, out var overlay);

        var lordName = StrategyStrongholdLordHelper.ResolveStrongholdLordName(s, meta, gameData);
        var isDirectRule = StrategyStrongholdLordHelper.IsDirectRule(s);

        var mayor = overlay?.MayorName;
        if (string.IsNullOrWhiteSpace(mayor) && s.LeaderId > 0
            && gameData.Characters.TryGetValue(s.LeaderId, out var mayorCharacter))
        {
            mayor = mayorCharacter.Name;
        }

        return new StrategyStrongholdStateDto
        {
            Id = s.Id,
            Name = s.Name,
            ForceId = s.ForceId,
            X = s.Location.X,
            Y = s.Location.Y,
            Food = s.ForceActor.Food,
            Population = s.Population,
            LordId = s.LordId,
            IsDirectRule = isDirectRule,
            LordName = lordName,
            MayorName = string.IsNullOrWhiteSpace(mayor) ? null : mayor,
            Morale = s.ForceActor.Morale,
            Training = s.ForceActor.Training,
            CultureName = overlay?.CultureName ?? "日本",
            ReligionName = overlay?.ReligionName ?? "神道",
            Money = s.ForceActor.Money,
            PollTaxRate = s.PollTaxRate,
            AgricultureTaxRate = s.AgricultureTaxRate,
            CommerceTaxRate = s.CommerceTaxRate,
            TariffTaxRate = s.TariffTaxRate
        };
    }

    private static IReadOnlyList<StrategyMapPointDto> BuildUnitRoute(Unit u)
    {
        if (u.ActionTarget.RoutePoints.Count == 0)
            return [];

        var route = new List<StrategyMapPointDto>
        {
            new() { X = u.Location.X, Y = u.Location.Y }
        };

        foreach (var point in u.ActionTarget.RoutePoints)
            route.Add(new StrategyMapPointDto { X = point.X, Y = point.Y });

        return route;
    }
}
