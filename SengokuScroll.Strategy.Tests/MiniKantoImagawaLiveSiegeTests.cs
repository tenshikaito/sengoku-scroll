using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>实机对齐：无方针目标、双今川满编，踏城后应自动强攻而非改打其它城。</summary>
public class MiniKantoImagawaLiveSiegeTests
{
    [Fact]
    public void LiveSetup_NoDirectiveTarget_AutoAssaultWhenSteppingOnMikawa()
    {
        using var ctx = MiniKantoSiegeScenarioHelper.CreateLiveImagawaVsMikawaContext(maxTilesMovedPerDay: 1);
        var mikawa = MiniKantoSiegeScenarioHelper.MikawaMinato(ctx.World.GameData);
        var mikawaTile = mikawa.Location;

        Unit? sieging = null;
        for (var day = 1; day <= 10; day++)
        {
            MiniKantoSiegeScenarioHelper.AdvanceDay(ctx);

            var main = MiniKantoSiegeScenarioHelper.ImagawaMain(ctx.World.GameData);
            var vanguard = MiniKantoSiegeScenarioHelper.ImagawaVanguard(ctx.World.GameData);

            if (main.SiegeMode != UnitSiegeMode.None && main.Location.IsSameTile(mikawaTile))
                sieging = main;
            else if (vanguard.SiegeMode != UnitSiegeMode.None && vanguard.Location.IsSameTile(mikawaTile))
                sieging = vanguard;

            if (sieging is not null)
                break;
        }

        Assert.NotNull(sieging);
        Assert.Equal(MiniKantoSiegeScenarioHelper.MikawaMinatoStrongholdId, sieging!.ActionTarget.StrongholdId);
        Assert.NotEqual(UnitSiegeMode.None, sieging.SiegeMode);

        var (_, meta) = MiniKantoSiegeScenarioHelper.Load();
        var dto = MiniKantoSiegeScenarioHelper.ToDto(ctx, meta);
        var siegeBf = MiniKantoSiegeScenarioHelper.OpenSiegeBattlefieldAt(dto, mikawaTile.X, mikawaTile.Y);
        Assert.NotNull(siegeBf);

        for (var extra = 0; extra < 5; extra++)
            MiniKantoSiegeScenarioHelper.AdvanceDay(ctx);

        var buffer = ctx.Services.GetRequiredService<SengokuScroll.Strategy.Diagnostics.StrategyDayOutcomeBuffer>();
        Assert.True(
            MiniKantoSiegeScenarioHelper.HasBattleReportMessenger(ctx.World.GameData, MiniKantoSiegeScenarioHelper.OdaForceId)
            || MiniKantoSiegeScenarioHelper.HasStrategicReportMessenger(ctx.World.GameData, MiniKantoSiegeScenarioHelper.OdaForceId)
            || buffer.Events.Any(e => e.Category is "BattleReportArrived" or "StrategicReportArrived"),
            "强攻开始应向守方（织田）派送战报/战略信使或当日抵达事件");
    }

    [Fact]
    public void OnEnemyStrongholdTile_LowAp_DoesNotMarchToOtherStronghold()
    {
        using var ctx = MiniKantoSiegeScenarioHelper.CreateLiveImagawaVsMikawaContext(maxTilesMovedPerDay: 1);
        var mikawa = MiniKantoSiegeScenarioHelper.MikawaMinato(ctx.World.GameData);
        var main = MiniKantoSiegeScenarioHelper.ImagawaMain(ctx.World.GameData);

        MiniKantoSiegeScenarioHelper.TeleportUnit(ctx.World, main, mikawa.Location);
        main.Directive = UnitDirective.Occupy;
        main.DirectiveTargetId = 0;
        main.Ap = 1;
        main.ActionTarget.RoutePoints.Clear();
        main.Status = UnitStatus.Waiting;
        main.SiegeMode = UnitSiegeMode.None;

        MiniKantoSiegeScenarioHelper.AdvanceDay(ctx);

        main = MiniKantoSiegeScenarioHelper.ImagawaMain(ctx.World.GameData);
        Assert.True(main.Location.IsSameTile(mikawa.Location));
        Assert.NotEqual(UnitSiegeMode.None, main.SiegeMode);
        Assert.Empty(main.ActionTarget.RoutePoints);
    }
}
