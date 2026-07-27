using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Tests;

/// <summary>后勤计算公式（<see cref="LogisticsCalculator"/>）的单元测试。</summary>
public class LogisticsCalculatorTests
{
    [Fact]
    public void DailyTransitConsumption_MatchesHistoricalBaseline()
    {
        var consumption = LogisticsCalculator.CalculateDailyTransitConsumption(
            LogisticsConstants.DefaultPorterCount,
            LogisticsConstants.DefaultEscortSoldierCount);

        Assert.Equal(135, consumption);
    }

    [Fact]
    public void UnitDailyFoodConsumption_ScalesWithSoldiers()
    {
        Assert.Equal(300, LogisticsCalculator.CalculateUnitDailyFoodConsumption(100));
    }

    [Fact]
    public void CivilianDailyFoodConsumption_ScalesWithPopulation()
    {
        Assert.Equal(2000, LogisticsCalculator.CalculateCivilianDailyFoodConsumption(1000));
    }
}

/// <summary>运输 Unit 日更行为（<see cref="TransportUnitActions"/>）的单元测试。</summary>
public class TransportUnitActionsTests
{
    [Fact]
    public void ApplyDailyTransitConsumption_ReducesCargo()
    {
        var transport = Fixtures.StrategyTestWorldBuilder.CreateTestTransportUnit(1, 1, new Point3(0, 0));
        transport.TransportOriginStrongholdId = 1;
        transport.TransportTargetUnitId = 1;
        transport.Food = 5000;

        TransportUnitActions.ApplyDailyTransitConsumption(transport);

        Assert.Equal(4865, transport.Food);
    }
}

/// <summary>信使制度规则与假情报行为（<see cref="MessageCarrierRules"/> / <see cref="MessageCarrierActions"/>）的单元测试。</summary>
public class MessageCarrierRulesAndActionsTests
{
    [Fact]
    public void RequiresMessenger_WhenSameTile_ReturnsFalse()
    {
        var p = new Point3(3, 4);
        Assert.False(MessageCarrierRules.RequiresInTransitDelivery(p, p));
    }

    [Fact]
    public void RequiresMessenger_WhenDifferentTile_ReturnsTrue()
    {
        Assert.True(MessageCarrierRules.RequiresInTransitDelivery(new Point3(1, 1), new Point3(2, 2)));
    }

    [Fact]
    public void ApplyFalseIntelligence_DeceivesTransportUnit()
    {
        var transport = Fixtures.StrategyTestWorldBuilder.CreateTestTransportUnit(1, 1, new Point3(0, 0));
        transport.TransportOriginStrongholdId = 1;
        transport.TransportTargetUnitId = 2;
        transport.Food = 1000;
        transport.ActionTarget.RoutePoints = new Queue<Point2>([new Point2(1, 0), new Point2(2, 0)]);

        var carrier = new Domain.Entities.MessageCarrier
        {
            Id = 1,
            Name = "测试假情报载体",
            ForceId = 2,
            Location = new Point3(5, 5),
            SourceStrongholdId = 9,
            Status = MessageCarrierStatus.Moving,
            RoutePoints = new Queue<Point3>(),
            Payload = new MessagePayload
            {
                Type = MessagePayloadType.FalseIntelligence,
                TargetConvoyId = 1
            }
        };

        MessageCarrierActions.ApplyFalseIntelligence(transport, carrier);

        Assert.True(transport.IsDeceived);
        Assert.Equal(LogisticsConstants.FalseIntelligenceHoldDays, transport.DeceivedHoldDaysRemaining);
        Assert.Empty(transport.ActionTarget.RoutePoints);
    }
}
