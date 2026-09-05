using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Tests;

public sealed class StrategyAiControlRulesTests
{
    [Fact]
    public void Multiplayer_NoConnectedPlayers_DoesNotFallBackToSinglePlayer()
    {
        var meta = new StrategyScenarioMeta { HasHumanControlConfiguration = true };
        Assert.True(StrategyAiControlRules.IsForceAiControlled(meta, meta.PlayerForceId));
    }

    [Fact]
    public void Multiplayer_HumanForcesAreNotAiControlled()
    {
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = 1,
            HumanControlledForceIds = new HashSet<int> { 1, 3 }
        };

        Assert.False(StrategyAiControlRules.IsForceAiControlled(meta, 1));
        Assert.False(StrategyAiControlRules.IsForceAiControlled(meta, 3));
        Assert.True(StrategyAiControlRules.IsForceAiControlled(meta, 2));
    }

    [Fact]
    public void SinglePlayer_UsesCurrentPlayerForceFallback()
    {
        var meta = new StrategyScenarioMeta { PlayerForceId = 2 };

        Assert.False(StrategyAiControlRules.IsForceAiControlled(meta, 2));
        Assert.True(StrategyAiControlRules.IsForceAiControlled(meta, 1));
    }
}
