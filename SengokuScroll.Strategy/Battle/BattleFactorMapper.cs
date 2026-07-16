using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Battle;

/// <summary>战斗因素明细 → DTO / 战报日志。</summary>
public static class BattleFactorMapper
{
    /// <summary>将因素明细转为 DTO，供前端战报展示。</summary>
    public static IReadOnlyList<StrategyBattleFactorNoteDto> ToFactorNotes(BattleFactorBreakdown breakdown)
        => [.. breakdown.Notes.Select(n => new StrategyBattleFactorNoteDto
        {
            FactorId = n.FactorId,
            Label = n.Label,
            AttackerWinRateDelta = n.AttackerWinRateDelta,
            DefenderWinRateDelta = n.DefenderWinRateDelta,
            Detail = n.Detail
        })];

    /// <summary>在战报日志末尾追加胜负因素明细段落。</summary>
    public static IReadOnlyList<StrategyBattleLogEntryDto> AppendFactorNotesToLog(
        IReadOnlyList<StrategyBattleLogEntryDto> logEntries,
        BattleFactorBreakdown breakdown)
    {
        if (breakdown.Notes.Count == 0)
            return logEntries;

        var list = logEntries.ToList();
        var order = list.Count > 0 ? list.Max(e => e.Order) : 0;

        list.Add(new StrategyBattleLogEntryDto
        {
            Order = ++order,
            Side = "system",
            Phase = "因素",
            Message = "── 胜负因素明细 ──"
        });

        foreach (var note in breakdown.Notes)
        {
            var delta = FormatDelta(note.AttackerWinRateDelta, note.DefenderWinRateDelta);
            var detail = string.IsNullOrWhiteSpace(note.Detail) ? note.Label : $"{note.Label}（{note.Detail}）";
            list.Add(new StrategyBattleLogEntryDto
            {
                Order = ++order,
                Side = "system",
                Phase = "修正",
                Message = $"{detail}{delta}"
            });
        }

        return list;
    }

    private static string FormatDelta(int atk, int def)
    {
        var parts = new List<string>();
        if (atk != 0) parts.Add($"攻方{atk:+0;-0}%");
        if (def != 0) parts.Add($"守方{def:+0;-0}%");
        return parts.Count == 0 ? "" : $" [{string.Join("，", parts)}]";
    }
}
