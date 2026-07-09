using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Persistence;

/// <summary>单机 JSON 存档文档（M3-d 最小可恢复字段）。</summary>
public sealed class StrategySaveDocument
{
    public required string ScenarioId { get; init; }

    public required int PlayerForceId { get; init; }

    public required StrategySaveDate Date { get; init; }

    public required List<StrategySaveForce> Forces { get; init; }

    public required List<StrategySaveStronghold> Strongholds { get; init; }

    public required List<StrategySaveUnit> Units { get; init; }
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
}

public sealed class StrategySaveStronghold
{
    public required int Id { get; init; }

    public required int ForceId { get; init; }

    public required int LordId { get; init; }

    public required int Population { get; init; }

    public required int Food { get; init; }

    public required int Money { get; init; }
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

/// <summary>从运行中世界捕获/恢复存档。</summary>
public static class StrategyWorldSaveService
{
    public static StrategySaveDocument Capture(GameWorld world, string scenarioId, int playerForceId)
    {
        var data = world.GameData;
        var date = data.GameDate;

        return new StrategySaveDocument
        {
            ScenarioId = scenarioId,
            PlayerForceId = playerForceId,
            Date = new StrategySaveDate
            {
                Year = date.Year,
                Month = date.Month,
                Day = date.Day
            },
            Forces = data.Forces.Values
                .Select(f => new StrategySaveForce { Id = f.Id, Money = f.Money, Food = f.Food })
                .OrderBy(f => f.Id)
                .ToList(),
            Strongholds = data.Strongholds.Values
                .Select(s => new StrategySaveStronghold
                {
                    Id = s.Id,
                    ForceId = s.ForceId,
                    LordId = s.LordId,
                    Population = s.Population,
                    Food = s.ForceActor.Food,
                    Money = s.ForceActor.Money
                })
                .OrderBy(s => s.Id)
                .ToList(),
            Units = data.Units.Values
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
                .OrderBy(u => u.Id)
                .ToList()
        };
    }

    public static void Apply(StrategySaveDocument save, GameWorld world)
    {
        var data = world.GameData;

        data.GameDate = new Domain.Types.GameDate(save.Date.Year, save.Date.Month, save.Date.Day);

        foreach (var forceSave in save.Forces)
        {
            if (!data.Forces.TryGetValue(forceSave.Id, out var force))
                continue;

            force.Money = forceSave.Money;
            force.Food = forceSave.Food;
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

            if (Enum.TryParse<UnitDirective>(unitSave.Directive, ignoreCase: true, out var directive))
                unit.Directive = directive;

            unit.ActionTarget.RoutePoints.Clear();
            foreach (var point in unitSave.Route.Skip(1))
                unit.ActionTarget.RoutePoints.Enqueue(new Point2(point.X, point.Y));
        }
    }
}
