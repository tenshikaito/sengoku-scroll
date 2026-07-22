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
        // 默认运输队：50 人夫 + 20 护卫，参照战国兵站日耗基准
        var consumption = LogisticsCalculator.CalculateDailyTransitConsumption(
            LogisticsConstants.DefaultPorterCount,
            LogisticsConstants.DefaultEscortSoldierCount);

        // 50×1.5 + 20×3 = 75+60 = 135 合/日
        Assert.Equal(135, consumption);
    }

    [Fact]
    public void UnitDailyFoodConsumption_ScalesWithSoldiers()
    {
        // 100 兵 × 3 合/人/日 = 300 合
        Assert.Equal(300, LogisticsCalculator.CalculateUnitDailyFoodConsumption(100));
    }

    [Fact]
    public void CivilianDailyFoodConsumption_ScalesWithPopulation()
    {
        // 1000 民 × 2 合/人/日 = 2000 合
        Assert.Equal(2000, LogisticsCalculator.CalculateCivilianDailyFoodConsumption(1000));
    }
}

/// <summary>运输队日更行为（<see cref="SupplyConvoyActions"/>）的单元测试。</summary>
public class SupplyConvoyActionsTests
{
    [Fact]
    public void ApplyDailyTransitConsumption_ReducesCargo()
    {
        // 一支载粮 5000 合的运输队，编成与默认常量一致
        var convoy = new Domain.Entities.SupplyConvoy
        {
            Id = 1,
            Name = "测试粮运队",
            ForceId = 1,
            Location = new Point3(0, 0),
            OriginStrongholdId = 1,
            TargetUnitId = 1,
            CargoFoodGo = 5000,
            PorterCount = 50,
            EscortSoldierCount = 20,
            Status = SupplyConvoyStatus.Moving
        };

        SupplyConvoyActions.ApplyDailyTransitConsumption(convoy);

        // 扣除 135 合在途自耗
        Assert.Equal(4865, convoy.CargoFoodGo);
    }
}

/// <summary>信使制度规则与假情报行为（<see cref="MessageCarrierRules"/> / <see cref="MessageCarrierActions"/>）的单元测试。</summary>
public class MessageCarrierRulesAndActionsTests
{
    [Fact]
    public void RequiresMessenger_WhenSameTile_ReturnsFalse()
    {
        // 同格（含同在据点内）免信使
        var p = new Point3(3, 4);
        Assert.False(MessageCarrierRules.RequiresInTransitDelivery(p, p));
    }

    [Fact]
    public void RequiresMessenger_WhenDifferentTile_ReturnsTrue()
    {
        // 异格必须信使传递
        Assert.True(MessageCarrierRules.RequiresInTransitDelivery(new Point3(1, 1), new Point3(2, 2)));
    }

    [Fact]
    public void ApplyFalseIntelligence_DeceivesConvoy()
    {
        // 在途运输队，原路径 2 格
        var convoy = new Domain.Entities.SupplyConvoy
        {
            Id = 1,
            Name = "测试粮运队",
            ForceId = 1,
            Location = new Point3(0, 0),
            OriginStrongholdId = 1,
            TargetUnitId = 2,
            CargoFoodGo = 1000,
            PorterCount = 10,
            EscortSoldierCount = 5,
            Status = SupplyConvoyStatus.Moving,
            RoutePoints = new Queue<Point3>([new Point3(1, 0), new Point3(2, 0)])
        };

        // 敌方信使投递假情报，目标为该运输队
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

        MessageCarrierActions.ApplyFalseIntelligence(convoy, carrier);

        Assert.True(convoy.IsDeceived);
        Assert.Equal(SupplyConvoyStatus.Deceived, convoy.Status);
        Assert.Equal(LogisticsConstants.FalseIntelligenceHoldDays, convoy.DeceivedHoldDaysRemaining);
        Assert.Empty(convoy.RoutePoints);
        Assert.Equal(carrier.Location, convoy.DeceivedRedirect);
    }
}
