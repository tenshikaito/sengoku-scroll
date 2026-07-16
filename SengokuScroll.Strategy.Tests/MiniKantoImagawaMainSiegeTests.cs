using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>
/// mini_kanto：今川主力自三河凑东侧进攻，逐日推进、围城、占城与信使路由。
/// </summary>
public class MiniKantoImagawaMainSiegeTests
{
    [Fact]
    public void ImagawaMain_MarchesSiegesAndCapturesMikawaMinato_ByScenarioTimeline()
    {
        var (_, meta) = MiniKantoSiegeScenarioHelper.Load();
        using var ctx = MiniKantoSiegeScenarioHelper.CreateImagawaMainVsMikawaContext(maxTilesMovedPerDay: 1);

        var mikawa = MiniKantoSiegeScenarioHelper.MikawaMinato(ctx.World.GameData);
        var kiyosu = MiniKantoSiegeScenarioHelper.Kiyosu(ctx.World.GameData);
        var kakegawa = MiniKantoSiegeScenarioHelper.Kakegawa(ctx.World.GameData);
        var mikawaTile = mikawa.Location;

        var main = MiniKantoSiegeScenarioHelper.ImagawaMain(ctx.World.GameData);
        Assert.Equal(new Point3(mikawaTile.X + 3, mikawaTile.Y), main.Location);
        Assert.Equal(UnitDirective.Occupy, main.Directive);
        Assert.Equal(MiniKantoSiegeScenarioHelper.MikawaMinatoStrongholdId, main.DirectiveTargetId);

        // 第 1 日：今川主力向西移动 1 格
        MiniKantoSiegeScenarioHelper.AdvanceDay(ctx);
        main = MiniKantoSiegeScenarioHelper.ImagawaMain(ctx.World.GameData);
        Assert.Equal(new Point3(mikawaTile.X + 2, mikawaTile.Y), main.Location);

        // 第 2 日：再向西 1 格
        MiniKantoSiegeScenarioHelper.AdvanceDay(ctx);
        main = MiniKantoSiegeScenarioHelper.ImagawaMain(ctx.World.GameData);
        Assert.Equal(new Point3(mikawaTile.X + 1, mikawaTile.Y), main.Location);

        // 第 3 日：今川主力走上三河凑格（邻格或同格开战/接敌）
        MiniKantoSiegeScenarioHelper.AdvanceDay(ctx);
        main = MiniKantoSiegeScenarioHelper.ImagawaMain(ctx.World.GameData);
        Assert.True(main.Location.IsSameTile(mikawaTile) || main.Location.IsAdjacent(mikawaTile),
            $"第3日主力应在三河凑格或邻格，实际=({main.Location.X},{main.Location.Y})");

        var day3Messengers = ctx.World.GameData.Messengers.Values
            .Where(m => m.PayloadType == MessengerPayloadType.BattleReport)
            .ToList();
        if (day3Messengers.Count > 0)
        {
            Assert.Contains(day3Messengers, m => m.ForceId == MiniKantoSiegeScenarioHelper.OdaForceId);
            Assert.Contains(day3Messengers, m => m.ForceId == MiniKantoSiegeScenarioHelper.ImagawaForceId);
        }

        // 第 4 日：开始攻城，地图显示围城战场；兵力充足时同格为强攻，否则邻格包围
        MiniKantoSiegeScenarioHelper.AdvanceDay(ctx);
        main = MiniKantoSiegeScenarioHelper.ImagawaMain(ctx.World.GameData);
        Assert.NotEqual(UnitSiegeMode.None, main.SiegeMode);
        Assert.Equal(MiniKantoSiegeScenarioHelper.MikawaMinatoStrongholdId, main.ActionTarget.StrongholdId);

        var dtoDay4 = MiniKantoSiegeScenarioHelper.ToDto(ctx, meta);
        var siegeBf = MiniKantoSiegeScenarioHelper.OpenSiegeBattlefieldAt(dtoDay4, mikawaTile.X, mikawaTile.Y);
        Assert.NotNull(siegeBf);
        Assert.Contains(MiniKantoSiegeScenarioHelper.ImagawaMainId, siegeBf!.UnitIds);

        var visibleAtMikawa = MiniKantoSiegeScenarioHelper.VisibleMapUnits(dtoDay4, mikawaTile.X, mikawaTile.Y);
        Assert.DoesNotContain(visibleAtMikawa, u => u.Id == MiniKantoSiegeScenarioHelper.ImagawaMainId);

        if (main.SiegeMode == UnitSiegeMode.Encircle)
        {
            Assert.True(main.Location.IsAdjacent(mikawaTile));
        }
        else
        {
            Assert.True(main.Location.IsSameTile(mikawaTile));
        }

        Assert.True(
            MiniKantoSiegeScenarioHelper.HasStrategicReportMessenger(ctx.World.GameData, MiniKantoSiegeScenarioHelper.OdaForceId)
            || ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>().Events.Any(e => e.Category == "StrategicReportArrived"),
            "强攻开始应向守方（织田）派送情报信使或当日抵达事件");

        // 数日后：城内兵/士气耗尽、三河凑易手、战场关闭
        var captured = false;
        for (var extraDay = 0; extraDay < 60 && !captured; extraDay++)
        {
            MiniKantoSiegeScenarioHelper.AdvanceDay(ctx);
            mikawa = MiniKantoSiegeScenarioHelper.MikawaMinato(ctx.World.GameData);

            if (mikawa.ForceId == MiniKantoSiegeScenarioHelper.ImagawaForceId)
                captured = true;
        }

        Assert.True(captured, "应在数日内完成三河凑占领");
        Assert.Equal(MiniKantoSiegeScenarioHelper.ImagawaForceId, mikawa.ForceId);
        Assert.DoesNotContain(
            ctx.World.GameData.Battlefields.Values,
            bf => !bf.IsClosed && bf.Location.X == mikawaTile.X && bf.Location.Y == mikawaTile.Y);

        var dtoCaptured = MiniKantoSiegeScenarioHelper.ToDto(ctx, meta);
        Assert.Null(MiniKantoSiegeScenarioHelper.OpenSiegeBattlefieldAt(dtoCaptured, mikawaTile.X, mikawaTile.Y));

        var buffer = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        Assert.True(
            buffer.Events.Any(e =>
                e.Category == "StrategicReportArrived" && e.DetailCategory == "StrongholdCaptured")
            || MiniKantoSiegeScenarioHelper.HasStrategicReportMessenger(
                ctx.World.GameData, MiniKantoSiegeScenarioHelper.OdaForceId),
            "占城情报须经信使送达玩家（织田）");

        // 陷落后：织田（前属）与今川（新属）均应有战报信使指向各自当主居城
        Assert.True(
            MiniKantoSiegeScenarioHelper.HasBattleReportMessengerToward(
                ctx.World.GameData,
                MiniKantoSiegeScenarioHelper.OdaForceId,
                kiyosu.Location)
            || buffer.Events.Any(e =>
                e.Category is "BattleReportArrived" or "StrategicReportArrived"
                && (e.Brief?.Contains("三河凑", StringComparison.Ordinal) == true
                    || e.DetailMessage?.Contains("三河凑", StringComparison.Ordinal) == true)),
            "织田方应向清洲居城报送战况");

        Assert.True(
            buffer.Events.Any(e =>
                e.Category == "StrategicReportArrived"
                && e.DetailCategory == "StrongholdCaptured"
                && (e.DetailMessage?.Contains("三河凑", StringComparison.Ordinal) == true
                    || e.Brief?.Contains("三河凑", StringComparison.Ordinal) == true))
            || MiniKantoSiegeScenarioHelper.HasStrategicReportMessenger(
                ctx.World.GameData, MiniKantoSiegeScenarioHelper.OdaForceId),
            "陷落详情须经信使送达玩家居城");
    }

    [Fact]
    public void InitialLayout_MatchesSpecifiedPositions()
    {
        var (world, _) = MiniKantoSiegeScenarioHelper.Load();
        MiniKantoSiegeScenarioHelper.ConfigureImagawaMainVsMikawa(world);

        Assert.Equal(new Point3(4, 4), world.GameData.Units[MiniKantoSiegeScenarioHelper.OdaVanguardId].Location);
        Assert.Equal(new Point3(5, 4), world.GameData.Units[MiniKantoSiegeScenarioHelper.ImagawaVanguardId].Location);
        Assert.Equal(new Point3(6, 6), world.GameData.Units[MiniKantoSiegeScenarioHelper.ImagawaMainId].Location);
        Assert.Equal(UnitDirective.Occupy, world.GameData.Units[MiniKantoSiegeScenarioHelper.ImagawaMainId].Directive);
    }
}
