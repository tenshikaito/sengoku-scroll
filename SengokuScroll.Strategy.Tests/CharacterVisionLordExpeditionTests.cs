using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Tests;

/// <summary>
/// 角色视野：当主居城出征后，视野随当主所在部队移动而扩展。
/// </summary>
public class CharacterVisionLordExpeditionTests
{
    private const int KiyosuX = 2;
    private const int KiyosuY = 8;
    private const int NobunagaCharacterId = 1;

    [Fact]
    public void CharacterFog_Day1DeployNobunagaAtKiyosu_Day2MoveEast_ExpandsVisionRight()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario(
            "mini_kanto",
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Hard }).IsSuccess);

        var day1 = host.GetState().Value!;
        Assert.Equal("Character", day1.StartOptions?.FogMode);
        Assert.Equal(KiyosuX, day1.Lord.X);
        Assert.Equal(KiyosuY, day1.Lord.Y);
        Assert.False(IsCellVisible(day1, 5, KiyosuY));

        var deploy = host.DeployFromStronghold(
            1,
            "织田信长队",
            NobunagaCharacterId,
            [
                new StrategyDeployCompositionEntry
                {
                    TypeId = StrategyTroopTypes.Ashigaru,
                    TypeName = "足轻",
                    Soldiers = 500
                }
            ]);
        Assert.True(deploy.IsSuccess);

        var world = GetWorld(host);
        var nobunagaUnit = world.GameData.Units.Values.Single(u =>
            u.LeaderId == NobunagaCharacterId
            && u.Location.X == KiyosuX
            && u.Location.Y == KiyosuY);
        Assert.Equal("织田信长队", nobunagaUnit.Name);

        var afterDeploy = host.GetState().Value!;
        Assert.Equal(KiyosuX, afterDeploy.Lord.X);
        Assert.Equal(KiyosuY, afterDeploy.Lord.Y);
        Assert.False(IsCellVisible(afterDeploy, 5, KiyosuY));

        Assert.True(host.OrderUnitMove(nobunagaUnit.Id, new Point2(KiyosuX + 1, KiyosuY)).IsSuccess);

        var advance = host.AdvanceDay();
        Assert.True(advance.IsSuccess);

        var day2 = advance.Value!.State;
        Assert.Equal("Character", day2.StartOptions?.FogMode);
        Assert.Equal(KiyosuX + 1, day2.Lord.X);
        Assert.Equal(KiyosuY, day2.Lord.Y);
        Assert.True(IsCellVisible(day2, 5, KiyosuY));
        Assert.False(IsCellVisible(day1, 5, KiyosuY));
        Assert.False(IsCellVisible(day2, 0, KiyosuY));
    }

    private static bool IsCellVisible(StrategyWorldStateDto state, int x, int y)
        => state.Visibility?.VisibleCells.Any(c => c.X == x && c.Y == y) == true;

    private static GameWorld GetWorld(StrategySimulationHost host)
    {
        var field = typeof(StrategySimulationHost).GetField(
            "simulation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var scope = field!.GetValue(host)!;
        return (GameWorld)scope.GetType().GetProperty("World")!.GetValue(scope)!;
    }
}
