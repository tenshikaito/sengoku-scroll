using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Force;

namespace SengokuScroll.Strategy.Tests;

public class PeaceSettlementTests
{
    [Fact]
    public void Preview_RejectsTermsWhoseCombinedCostExceedsOneHundred()
    {
        var world = BuildWorld();
        var data = world.GameData;
        var terms = new PeaceSettlementTerms
        {
            CededStrongholdIds = [2],
            ReparationsMoney = 9_000,
            DemandOuterVassalage = true,
        };

        Assert.False(PeaceSettlementRules.TryBuildPreview(
            data, 1, 2, terms, 50, out _, out var error));
        Assert.Equal("PeaceTermsExceedMaximumWarScore", error?.Code);
    }

    [Fact]
    public void Execute_AppliesLandMoneyAndVassalageThenEndsWarWithTruce()
    {
        var world = BuildWorld();
        var data = world.GameData;
        var war = Assert.Single(data.Wars.Values);
        WarRules.AddWarScore(war, 1, 2, 100, data.GameDate, "TestVictory");
        var proposerMoneyBefore = data.Strongholds[1].ForceActor.Money;

        var terms = new PeaceSettlementTerms
        {
            CededStrongholdIds = [2],
            ReparationsMoney = 100,
            DemandOuterVassalage = true,
        };

        Assert.True(PeaceSettlementRules.TryBuildPreview(
            data, 1, 2, terms, 40, out var preview, out var previewError));
        Assert.Null(previewError);
        Assert.True(preview.CanForceAcceptance);
        Assert.Equal(100, preview.AcceptanceChancePercent);
        Assert.InRange(preview.RequiredWarScore, 1, 100);

        Assert.True(PeaceSettlementActions.TryExecute(
            data, world.GameMasterData, 1, 2, terms, out var error));
        Assert.Null(error);
        Assert.Equal(1, data.Strongholds[2].ForceId);
        Assert.Equal(proposerMoneyBefore, data.Strongholds[1].ForceActor.Money);
        Assert.Equal(1_100, data.Strongholds[2].ForceActor.Money);
        Assert.Equal(1_600, data.Forces[1].Money);
        Assert.Equal(8_900, data.Strongholds[3].ForceActor.Money);
        Assert.True(war.IsEnded);
        Assert.Equal(ForceStatus.OuterVassal, data.Forces[2].Status);
        Assert.Equal(1, data.Forces[2].SuzerainForceId);

        var diplomacy = Assert.Single(data.Forces[1].Diplomacies, d => d.TargetForceId == 2);
        Assert.True(diplomacy.IsTruce);
        Assert.Equal(ForceDiplomacyActions.DefaultTruceDays, diplomacy.TrucePeriod);
    }

    private static GameWorld BuildWorld()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var data = world.GameData;
        data.Forces[1].Status = ForceStatus.Independence;
        data.Forces[2] = StrategyTestWorldBuilder.CreateTestForce(2);
        data.Forces[2].Status = ForceStatus.Independence;

        data.Strongholds[1] = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0));
        data.Strongholds[1].ForceActor.Money = 500;
        data.Strongholds[2] = StrategyTestWorldBuilder.CreateTestStronghold(2, 2, new Point3(4, 0));
        data.Strongholds[2].ForceActor.Money = 1_000;
        data.Strongholds[2].Scale = 10;
        data.Strongholds[3] = StrategyTestWorldBuilder.CreateTestStronghold(3, 2, new Point3(5, 0));
        data.Strongholds[3].ForceActor.Money = 9_000;

        ForceEconomyActions.SyncForceTreasuryFromStrongholds(data.Forces[1], data);
        ForceEconomyActions.SyncForceTreasuryFromStrongholds(data.Forces[2], data);
        Assert.True(ForceDiplomacyActions.TrySetRelation(
            data, 1, 2, Diplomacy.DiplomacyRelation.Enemy, out _));
        return world;
    }
}
