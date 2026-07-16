using System.Net;
using System.Net.Http.Json;
using SengokuScroll.Strategy.Models;
using SengokuScroll.WebApi.Models;

namespace SengokuScroll.WebApi.Tests;

/// <summary>策略 API 端到端集成测试（M2-a）。</summary>
public class StrategyApiTests : IClassFixture<StrategyWebApplicationFactory>
{
    private readonly HttpClient client;

    public StrategyApiTests(StrategyWebApplicationFactory factory)
        => client = factory.CreateClient();

    [Fact]
    public async Task LoadScenario_ReturnsWorldState()
    {
        var response = await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest
        {
            ScenarioId = "mini_kanto"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var state = await response.Content.ReadFromJsonAsync<StrategyWorldStateDto>();
        Assert.NotNull(state);
        Assert.Equal("mini_kanto", state.ScenarioId);
        Assert.Equal(1560, state.Date.Year);
        Assert.Equal(10, state.Map.Width);
        Assert.Equal(2, state.Forces.Count);
        Assert.Equal(10, state.Strongholds.Count);
        Assert.Equal(2, state.Units.Count);
    }

    [Fact]
    public async Task MoveAndAdvanceDay_ReturnsUpdatedStateInResponse()
    {
        await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        var moveResponse = await client.PostAsJsonAsync("/strategy/units/1/move", new MoveUnitRequest { X = 3, Y = 4 });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var afterMove = await moveResponse.Content.ReadFromJsonAsync<StrategyWorldStateDto>();
        Assert.NotNull(afterMove);
        Assert.Equal("Moving", afterMove.Units.First(u => u.Id == 1).Status);
        Assert.True(afterMove.Units.First(u => u.Id == 1).Route.Count >= 2);

        var advanceResponse = await client.PostAsync("/strategy/advance-day", null);
        var afterDay = await advanceResponse.Content.ReadFromJsonAsync<StrategyAdvanceDayResponseDto>();
        var odaUnit = afterDay!.State.Units.First(u => u.Id == 1);

        Assert.Equal(3, odaUnit.X);
        Assert.Equal(4, odaUnit.Y);
        Assert.Equal(1560, afterDay.State.Date.Year);
    }

    [Fact]
    public async Task MoveThroughFriendlyStronghold_1_2_To_3_2_AdvancesOneDay()
    {
        await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        // 先移动到 (1,2)，复现用户场景
        var toStart = await client.PostAsJsonAsync("/strategy/units/1/move", new MoveUnitRequest { X = 1, Y = 2 });
        Assert.Equal(HttpStatusCode.OK, toStart.StatusCode);
        for (var i = 0; i < 5; i++)
        {
            var day = await client.PostAsync("/strategy/advance-day", null);
            Assert.Equal(HttpStatusCode.OK, day.StatusCode);
            var s = await day.Content.ReadFromJsonAsync<StrategyAdvanceDayResponseDto>();
            if (s!.State.Units.First(u => u.Id == 1).X == 1 && s.State.Units.First(u => u.Id == 1).Y == 2)
                break;
        }

        var moveResponse = await client.PostAsJsonAsync("/strategy/units/1/move", new MoveUnitRequest { X = 3, Y = 2 });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
        var afterOrder = await moveResponse.Content.ReadFromJsonAsync<StrategyWorldStateDto>();
        var ordered = afterOrder!.Units.First(u => u.Id == 1);
        Assert.Equal("Moving", ordered.Status);
        Assert.Equal(1, ordered.X);
        Assert.Equal(2, ordered.Y);

        var advance1 = await client.PostAsync("/strategy/advance-day", null);
        Assert.Equal(HttpStatusCode.OK, advance1.StatusCode);
        var afterDay1 = await advance1.Content.ReadFromJsonAsync<StrategyAdvanceDayResponseDto>();
        var mid = afterDay1!.State.Units.First(u => u.Id == 1);
        Assert.Equal(2, mid.X);
        Assert.Equal(2, mid.Y);
        Assert.Equal("Moving", mid.Status);

        var advance2 = await client.PostAsync("/strategy/advance-day", null);
        var afterDay2 = await advance2.Content.ReadFromJsonAsync<StrategyAdvanceDayResponseDto>();
        var odaUnit = afterDay2!.State.Units.First(u => u.Id == 1);

        Assert.Equal(3, odaUnit.X);
        Assert.Equal(2, odaUnit.Y);

        var trace = await client.GetFromJsonAsync<List<StrategyMovementTraceEntryDto>>("/strategy/debug/movement-trace");
        Assert.NotNull(trace);
        Assert.Contains(trace, e => e.Phase == "MoveEval" && e.Detail != null && e.Detail.Contains("ApNotEnough"));
        Assert.Contains(trace, e => e.Phase == "MoveDone" && e.ToX == 3 && e.ToY == 2);
    }

    [Fact]
    public async Task PreviewPath_ReturnsPointsFromUnitToTarget()
    {
        await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        var response = await client.PostAsJsonAsync("/strategy/units/1/preview-path", new MoveUnitRequest { X = 5, Y = 4 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = await response.Content.ReadFromJsonAsync<StrategyPathPreviewDto>();
        Assert.NotNull(preview);
        Assert.True(preview!.Points.Count >= 2);
        Assert.Equal(4, preview.Points[0].X);
        Assert.Equal(4, preview.Points[0].Y);
    }

    [Fact]
    public async Task PreviewPath_FromRelay_StartsAtRelayNotUnit()
    {
        await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        var response = await client.PostAsJsonAsync(
            "/strategy/units/1/preview-path",
            new MoveUnitRequest { X = 5, Y = 4, FromX = 4, FromY = 5 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = await response.Content.ReadFromJsonAsync<StrategyPathPreviewDto>();
        Assert.NotNull(preview);
        Assert.True(preview!.Points.Count >= 2);
        Assert.Equal(4, preview.Points[0].X);
        Assert.Equal(5, preview.Points[0].Y);
    }

    [Fact]
    public async Task PreviewPath_WithVia_PassesThroughRelay()
    {
        await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        var response = await client.PostAsJsonAsync(
            "/strategy/units/1/preview-path",
            new MoveUnitRequest
            {
                X = 0,
                Y = 0,
                Via = [new MapPointRequest { X = 3, Y = 1 }],
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = await response.Content.ReadFromJsonAsync<StrategyPathPreviewDto>();
        Assert.NotNull(preview);
        Assert.Contains(preview!.Points, p => p.X == 3 && p.Y == 1);
        Assert.Equal(4, preview.Points[0].X);
        Assert.Equal(4, preview.Points[0].Y);
    }

    [Fact]
    public async Task GetState_OnStartup_ReturnsDefaultScenarioWithoutExplicitLoad()
    {
        var state = await client.GetFromJsonAsync<StrategyWorldStateDto>("/strategy/state");

        Assert.NotNull(state);
        Assert.Equal("mini_kanto", state.ScenarioId);
        Assert.Equal(1560, state.Date.Year);
        Assert.Equal(2, state.Units.Count);
    }

    [Fact]
    public async Task GetState_AfterLoad_MatchesLoadResponseShape()
    {
        var loadResponse = await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest
        {
            ScenarioId = "mini_kanto"
        });
        var fromLoad = await loadResponse.Content.ReadFromJsonAsync<StrategyWorldStateDto>();

        var stateResponse = await client.GetFromJsonAsync<StrategyWorldStateDto>("/strategy/state");

        Assert.Equal(fromLoad!.Date, stateResponse!.Date);
        Assert.Equal(fromLoad.Units.Count, stateResponse.Units.Count);
    }

    [Fact]
    public async Task SetUnitDirective_FromLord_DispatchesMessenger()
    {
        await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        var state = await client.GetFromJsonAsync<StrategyWorldStateDto>("/strategy/state");
        var unit = state!.Units.First(u => u.Id == 1);

        var response = await client.PostAsJsonAsync(
            $"/strategy/units/{unit.Id}/directive",
            new SetUnitDirectiveRequest { Directive = "Retreat" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<StrategyPolicyChangeResponseDto>();
        Assert.NotNull(payload);
        Assert.Equal("MessengerDispatched", payload!.Outcome);
        Assert.Single(payload.State.Messengers);
        Assert.Equal("Retreat", payload.State.Messengers[0].PendingDirective);
        Assert.Equal(state.Lord.X, payload.State.Messengers[0].X);
        Assert.Equal(state.Lord.Y, payload.State.Messengers[0].Y);
    }

    [Fact]
    public async Task AttackOrder_ThenAdvanceDay_ResolvesBattle()
    {
        await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        var orderResponse = await client.PostAsJsonAsync(
            "/strategy/units/1/attack-order",
            new MoveUnitRequest { X = 5, Y = 4 });
        Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);

        var advanceResponse = await client.PostAsJsonAsync("/strategy/advance-day", new { });
        Assert.Equal(HttpStatusCode.OK, advanceResponse.StatusCode);

        var payload = await advanceResponse.Content.ReadFromJsonAsync<StrategyAdvanceDayResponseDto>();
        Assert.NotNull(payload);
        Assert.Empty(payload!.ResolvedBattles);
        Assert.Contains(
            payload.State.Messengers,
            m => string.Equals(m.PayloadType, "BattleReport", StringComparison.OrdinalIgnoreCase));
        Assert.True(payload.State.Units.First(u => u.Id == 1).Soldiers < 3000);
    }

    [Fact]
    public async Task InstantBattle_AdjacentEnemies_ReducesSoldiers()
    {
        await client.PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        var previewResponse = await client.PostAsJsonAsync(
            "/strategy/units/1/preview-battle",
            new MoveUnitRequest { X = 5, Y = 4 });
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var preview = await previewResponse.Content.ReadFromJsonAsync<StrategyBattlePreviewDto>();
        Assert.NotNull(preview);
        Assert.Equal(2, preview!.DefenderUnitId);
        Assert.InRange(preview.AttackerWinRatePercent, 5, 95);

        var battleResponse = await client.PostAsJsonAsync(
            "/strategy/units/1/instant-battle",
            new MoveUnitRequest { X = 5, Y = 4 });
        Assert.Equal(HttpStatusCode.OK, battleResponse.StatusCode);

        var battlePayload = await battleResponse.Content.ReadFromJsonAsync<StrategyInstantBattleResponseDto>();
        Assert.NotNull(battlePayload);
        var state = battlePayload!.State;
        var attacker = state.Units.First(u => u.Id == 1);
        var defender = state.Units.First(u => u.Id == 2);
        Assert.True(attacker.Soldiers < 100);
        Assert.True(defender.Soldiers < 80);
        Assert.Equal(5, attacker.Ap);
        Assert.True(battlePayload.Result.AttackerCasualties >= 0);
        Assert.True(battlePayload.Result.DefenderCasualties >= 0);
    }
}
