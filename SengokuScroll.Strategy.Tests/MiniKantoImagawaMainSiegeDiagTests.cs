using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class MiniKantoImagawaMainSiegeDiagTests
{
    [Fact]
    public void DumpDailyState()
    {
        using var ctx = MiniKantoSiegeScenarioHelper.CreateImagawaMainVsMikawaContext(maxTilesMovedPerDay: 1);
        var mikawa = MiniKantoSiegeScenarioHelper.MikawaMinato(ctx.World.GameData);

        for (var day = 1; day <= 8; day++)
        {
            MiniKantoSiegeScenarioHelper.AdvanceDay(ctx);
            var main = MiniKantoSiegeScenarioHelper.ImagawaMain(ctx.World.GameData);
            var garrison = StrongholdGarrisonRules.FindGarrisonUnit(mikawa, ctx.World.GameData);
            var bfs = ctx.World.GameData.Battlefields.Values.Where(b => !b.IsClosed).ToList();
            var msgs = ctx.World.GameData.MessageCarriers.Values.Count(m => m.Payload.Type == MessagePayloadType.BattleReport);

            _output.WriteLine(
                $"D{day} main=({main.Location.X},{main.Location.Y}) status={main.Status} siege={main.SiegeMode} " +
                $"stance={main.Stance} bf={main.BattlefieldId} garrison={garrison?.Soldier} city={mikawa.ForceActor.Soldier} " +
                $"morale={mikawa.ForceActor.Morale} openBf={bfs.Count} msgs={msgs}");
            foreach (var bf in bfs)
                _output.WriteLine($"  bf#{bf.Id} kind={bf.Kind} @({bf.Location.X},{bf.Location.Y})");
        }
    }

    private readonly ITestOutputHelper _output;
    public MiniKantoImagawaMainSiegeDiagTests(ITestOutputHelper output) => _output = output;
}
