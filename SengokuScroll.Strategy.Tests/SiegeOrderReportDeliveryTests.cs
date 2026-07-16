using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class SiegeOrderReportDeliveryTests
{
    [Fact]
    public void DeliverSiegeOrderStartedReport_UsesStrategicReport_NotBattleReport()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var delivery = ctx.Services.GetRequiredService<BattleReportDeliveryHelper>();
        var gameData = ctx.World.GameData;

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, new Point3(4, 4));
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(3, 4));
        attacker.Soldier = 1500;

        gameData.Strongholds[10] = stronghold;
        gameData.Units[1] = attacker;

        delivery.DeliverSiegeOrderStartedReport(
            attacker,
            stronghold,
            UnitSiegeMode.Encircle,
            gameData);

        Assert.DoesNotContain(
            gameData.Messengers.Values,
            m => m.PayloadType == MessengerPayloadType.BattleReport);

        Assert.Contains(
            gameData.Messengers.Values,
            m => m.PayloadType == MessengerPayloadType.StrategicReport && m.ForceId == 1);
    }
}
