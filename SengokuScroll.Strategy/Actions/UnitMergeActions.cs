using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>合并两支友军：子编制并入目标部队并移除来源单位。</summary>
public static class UnitMergeActions
{
    public static GameResult MergeUnits(
        IGameWorldContext context,
        Unit source,
        Unit target,
        GameData gameData)
    {
        var validate = UnitMergeRules.ValidateMerge(source, target, gameData);
        if (!validate.IsSuccess)
            return validate;

        foreach (var subId in source.SubUnitIds.ToList())
        {
            if (!gameData.SubUnits.TryGetValue(subId, out var sub))
                continue;

            sub.UnitId = target.Id;
            if (!target.SubUnitIds.Contains(subId))
                target.SubUnitIds.Add(subId);
        }

        ConsolidateSubUnitsByType(target, gameData);

        target.Food += source.Food;
        target.Money += source.Money;
        target.Soldier = RecalculateSoldiers(target, gameData);

        if (target.LeaderId <= 0 && source.LeaderId > 0
            && gameData.Characters.TryGetValue(source.LeaderId, out var commander))
        {
            target.LeaderId = commander.Id;
            UnitCommanderHelper.AttachToUnit(commander, target);
        }

        MapLocationActions.RemoveUnit(context, source);
        gameData.Units.Remove(source.Id);

        return GameResult.Ok();
    }

    private static void ConsolidateSubUnitsByType(Unit unit, GameData gameData)
    {
        var grouped = unit.SubUnitIds
            .Select(id => gameData.SubUnits.TryGetValue(id, out var s) ? s : null)
            .Where(s => s is not null)
            .GroupBy(s => s!.TypeId)
            .ToList();

        var keptIds = new List<int>();
        foreach (var group in grouped)
        {
            var subs = group.ToList()!;
            if (subs.Count == 1)
            {
                keptIds.Add(subs[0]!.Id);
                continue;
            }

            var primary = subs[0]!;
            var total = subs.Sum(s => s!.Soldier);
            primary.Soldier = total;
            keptIds.Add(primary.Id);

            foreach (var extra in subs.Skip(1))
            {
                gameData.SubUnits.Remove(extra!.Id);
                unit.SubUnitIds.Remove(extra.Id);
            }
        }

        unit.SubUnitIds.Clear();
        unit.SubUnitIds.AddRange(keptIds);
    }

    private static int RecalculateSoldiers(Unit unit, GameData gameData)
    {
        if (unit.SubUnitIds.Count == 0)
            return unit.Soldier;

        return unit.SubUnitIds.Sum(id =>
            gameData.SubUnits.TryGetValue(id, out var s) ? Math.Max(0, s.Soldier) : 0);
    }
}
