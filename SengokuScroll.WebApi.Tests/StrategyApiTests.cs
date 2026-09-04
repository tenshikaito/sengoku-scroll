using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Models;
using SengokuScroll.WebApi.Models;

namespace SengokuScroll.WebApi.Tests;

/// <summary>策略 API 端到端集成测试（M2-a）。</summary>
public class StrategyApiTests : IClassFixture<StrategyWebApplicationFactory>
{
    private readonly HttpClient client;

    public StrategyApiTests(StrategyWebApplicationFactory factory)
        => client = factory.CreateClient();

    private static CancellationToken TestCancellation
        => TestContext.Current.CancellationToken;

    private Task<HttpResponseMessage> GetAsync(string uri)
        => client.GetAsync(uri, TestCancellation);

    private Task<T?> GetFromJsonAsync<T>(string uri)
        => client.GetFromJsonAsync<T>(uri, TestCancellation);

    private Task<HttpResponseMessage> PostAsync(string uri, HttpContent? content)
        => client.PostAsync(uri, content, TestCancellation);

    private Task<HttpResponseMessage> PostAsJsonAsync<T>(string uri, T value)
        => client.PostAsJsonAsync(uri, value, TestCancellation);

    private static Task<T?> ReadFromJsonAsync<T>(HttpContent content)
        => content.ReadFromJsonAsync<T>(TestCancellation);

    [Fact]
    public async Task GetMapMaster_ReturnsTerrainGridAndLandmarks()
    {
        await PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        var response = await GetAsync("/strategy/map");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var map = await ReadFromJsonAsync<StrategyMapMasterDto>(response.Content);
        Assert.NotNull(map);
        Assert.Equal("mini_kanto", map.ScenarioId);
        Assert.Equal(400, map.TerrainIds.Count);
        Assert.Equal(400, map.RegionIds.Count);
        Assert.Contains(map.Terrains, t => t.Key == "forest");
        Assert.True(map.RoadCells.Count >= 12);
        Assert.Equal(3, map.Landmarks.Count);
    }

    [Fact]
    public async Task LoadScenario_ReturnsWorldState()
    {
        var response = await LoadEasyScenarioAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var state = await ReadFromJsonAsync<StrategyWorldStateDto>(response.Content);
        Assert.NotNull(state);
        Assert.Equal("mini_kanto", state.ScenarioId);
        Assert.Equal(1560, state.Date.Year);
        Assert.Equal(20, state.Map.Width);
        Assert.Equal(16, state.Forces.Count);
        Assert.Equal(12, state.Strongholds.Count);
        Assert.Equal(3, state.Units.Count);
        Assert.Equal("Ongoing", state.CampaignStatus.State);
        Assert.Equal(12, state.CampaignStatus.TotalStrongholdCount);
    }

    [Fact]
    public async Task LoadScenario_SpectatorMode_EnablesAllForcesAiAndCampaignStatus()
    {
        var response = await PostAsJsonAsync(
            "/strategy/load",
            new LoadScenarioRequest
            {
                ScenarioId = "mini_kanto",
                Difficulty = "Normal",
                AllForcesAiControlled = true,
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await ReadFromJsonAsync<StrategyWorldStateDto>(response.Content);
        Assert.NotNull(state);
        Assert.True(state.AllForcesAiControlled);
        Assert.Equal("Spectating", state.CampaignStatus.State);
        Assert.NotNull(state.CampaignStatus.LeadingForceId);
        Assert.Equal(12, state.Strongholds.Count);
        Assert.Equal(3, state.Units.Count);
        Assert.Null(state.Visibility);
    }

    [Fact]
    public async Task CharacterInteraction_Talk_UpdatesBidirectionalRelationshipAndConsumesAp()
    {
        var load = await LoadEasyScenarioAsync();
        var before = await ReadFromJsonAsync<StrategyWorldStateDto>(load.Content);
        Assert.NotNull(before);
        var lordId = Assert.IsType<int>(before.Lord.CharacterId);
        var lord = Assert.Single(before.Characters, character => character.Id == lordId);
        var target = before.Characters.First(character =>
            character.Id != lordId
            && character.LocationType == "Stronghold"
            && character.StrongholdId == lord.StrongholdId);

        var response = await PostAsJsonAsync(
            $"/strategy/characters/{lordId}/interact",
            new CharacterInteractionRequest
            {
                TargetCharacterId = target.Id,
                Interaction = "Talk",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = await ReadFromJsonAsync<StrategyWorldStateDto>(response.Content);
        Assert.NotNull(after);
        var afterLord = Assert.Single(after.Characters, character => character.Id == lordId);
        var afterTarget = Assert.Single(after.Characters, character => character.Id == target.Id);
        Assert.Equal(before.Lord.Ap - CharacterSocialActions.TalkApCost, after.Lord.Ap);
        Assert.Contains(afterLord.CharacterRelationships, relationship =>
            relationship.TargetCharacterId == target.Id && relationship.Relationship > 0);
        Assert.Contains(afterTarget.CharacterRelationships, relationship =>
            relationship.TargetCharacterId == lordId && relationship.Relationship > 0);
    }

    [Fact]
    public async Task PeacePreview_ReturnsWarScoreCostAndAcceptanceChance()
    {
        await LoadEasyScenarioAsync();
        var warResponse = await PostAsJsonAsync(
            "/strategy/diplomacy/relation",
            new SetDiplomacyRelationRequest { TargetForceId = 2, Relation = "Enemy" });
        Assert.Equal(HttpStatusCode.OK, warResponse.StatusCode);

        var previewResponse = await PostAsJsonAsync(
            "/strategy/diplomacy/peace/preview",
            new PeaceSettlementRequest
            {
                CharacterId = 4,
                TargetForceId = 2,
                CededStrongholdIds = [],
                ReparationsMoney = 0,
                DemandOuterVassalage = false,
            });

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await ReadFromJsonAsync<StrategyPeaceSettlementPreviewDto>(previewResponse.Content);
        Assert.NotNull(preview);
        Assert.Equal(0, preview.RequiredWarScore);
        Assert.Equal(0, preview.ProposerWarScore);
        Assert.InRange(preview.AcceptanceChancePercent, 5, 95);
        Assert.True(preview.IsWhitePeace);
    }

    [Fact]
    public async Task MoveAndAdvanceDay_ReturnsUpdatedStateInResponse()
    {
        await LoadEasyScenarioAsync();

        var moveResponse = await PostAsJsonAsync("/strategy/units/1/move", new MoveUnitRequest { X = 9, Y = 8 });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var afterMove = await ReadFromJsonAsync<StrategyWorldStateDto>(moveResponse.Content);
        Assert.NotNull(afterMove);
        Assert.Equal("Moving", afterMove.Units.First(u => u.Id == 1).Status);
        Assert.True(afterMove.Units.First(u => u.Id == 1).Route.Count >= 2);

        var advanceResponse = await PostAsync("/strategy/advance-day", null);
        var afterDay = await ReadFromJsonAsync<StrategyAdvanceDayResponseDto>(advanceResponse.Content);
        var odaUnit = afterDay!.State.Units.First(u => u.Id == 1);

        Assert.Equal(9, odaUnit.X);
        Assert.Equal(8, odaUnit.Y);
        Assert.Equal("Waiting", odaUnit.Status);
        Assert.Equal(1560, afterDay.State.Date.Year);
    }

    [Fact]
    public async Task AdvanceDays_AdvancesBatchAndBuildsOneFinalState()
    {
        await LoadEasyScenarioAsync();
        var response = await PostAsJsonAsync(
            "/strategy/advance-days",
            new AdvanceDaysRequest { Days = 7 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadFromJsonAsync<StrategyAdvanceDayResponseDto>(response.Content);
        Assert.NotNull(payload);
        Assert.Equal(7, payload.DaysAdvanced);
        Assert.Equal(8, payload.State.Date.Day);
    }

    [Fact]
    public async Task MoveThroughFriendlyStronghold_ReachesTarget()
    {
        await LoadEasyScenarioAsync();

        // 从 (8,8) 向 (1,8) 行军，路线会穿过己方清洲据点 (2,8)。
        var previewResponse = await PostAsJsonAsync(
            "/strategy/units/1/preview-path",
            new MoveUnitRequest { X = 1, Y = 8 });
        var preview = await ReadFromJsonAsync<StrategyPathPreviewDto>(previewResponse.Content);
        Assert.Contains(preview!.Points, point => point.X == 2 && point.Y == 8);

        var moveResponse = await PostAsJsonAsync("/strategy/units/1/move", new MoveUnitRequest { X = 1, Y = 8 });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
        StrategyUnitStateDto? odaUnit = null;
        for (var dayIndex = 0; dayIndex < 12; dayIndex++)
        {
            var day = await PostAsync("/strategy/advance-day", null);
            Assert.Equal(HttpStatusCode.OK, day.StatusCode);
            var payload = await ReadFromJsonAsync<StrategyAdvanceDayResponseDto>(day.Content);
            odaUnit = payload!.State.Units.First(u => u.Id == 1);
            if (odaUnit.X == 1 && odaUnit.Y == 8)
                break;
        }

        Assert.NotNull(odaUnit);
        Assert.Equal(1, odaUnit!.X);
        Assert.Equal(8, odaUnit.Y);
        Assert.Equal("Waiting", odaUnit.Status);

    }

    [Fact]
    public async Task PreviewPath_ReturnsPointsFromUnitToTarget()
    {
        await LoadEasyScenarioAsync();

        var response = await PostAsJsonAsync("/strategy/units/1/preview-path", new MoveUnitRequest { X = 9, Y = 8 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = await ReadFromJsonAsync<StrategyPathPreviewDto>(response.Content);
        Assert.NotNull(preview);
        Assert.True(preview!.Points.Count >= 2);
        Assert.Equal(8, preview.Points[0].X);
        Assert.Equal(8, preview.Points[0].Y);
    }

    [Fact]
    public async Task PreviewPath_FromRelay_StartsAtRelayNotUnit()
    {
        await LoadEasyScenarioAsync();

        var response = await PostAsJsonAsync(
            "/strategy/units/1/preview-path",
            new MoveUnitRequest { X = 9, Y = 8, FromX = 8, FromY = 9 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = await ReadFromJsonAsync<StrategyPathPreviewDto>(response.Content);
        Assert.NotNull(preview);
        Assert.True(preview!.Points.Count >= 2);
        Assert.Equal(8, preview.Points[0].X);
        Assert.Equal(9, preview.Points[0].Y);
    }

    [Fact]
    public async Task PreviewPath_WithVia_PassesThroughRelay()
    {
        await LoadEasyScenarioAsync();

        var response = await PostAsJsonAsync(
            "/strategy/units/1/preview-path",
            new MoveUnitRequest
            {
                X = 10,
                Y = 8,
                Via = [new MapPointRequest { X = 9, Y = 8 }],
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = await ReadFromJsonAsync<StrategyPathPreviewDto>(response.Content);
        Assert.NotNull(preview);
        Assert.Contains(preview!.Points, p => p.X == 9 && p.Y == 8);
        Assert.Equal(8, preview.Points[0].X);
        Assert.Equal(8, preview.Points[0].Y);
    }

    [Fact]
    public async Task GetState_OnStartup_ReturnsDefaultScenarioWithoutExplicitLoad()
    {
        // 独立宿主验证“首次启动”，避免同类其它集成测试已显式加载剧本而污染单例状态。
        await using var freshFactory = new StrategyWebApplicationFactory();
        using var freshClient = freshFactory.CreateClient();
        var state = await freshClient.GetFromJsonAsync<StrategyWorldStateDto>(
            "/strategy/state",
            TestCancellation);

        Assert.NotNull(state);
        Assert.Equal("mini_kanto", state.ScenarioId);
        Assert.Equal(1560, state.Date.Year);
        Assert.Single(state.Units);
    }

    [Fact]
    public async Task GetState_AfterLoad_MatchesLoadResponseShape()
    {
        var loadResponse = await PostAsJsonAsync("/strategy/load", new LoadScenarioRequest
        {
            ScenarioId = "mini_kanto"
        });
        var fromLoad = await ReadFromJsonAsync<StrategyWorldStateDto>(loadResponse.Content);

        var stateResponse = await GetFromJsonAsync<StrategyWorldStateDto>("/strategy/state");

        Assert.Equal(fromLoad!.Date, stateResponse!.Date);
        Assert.Equal(fromLoad.Units.Count, stateResponse.Units.Count);
    }

    [Fact]
    public async Task SetUnitDirective_FromLord_DispatchesMessenger()
    {
        await PostAsJsonAsync("/strategy/load", new LoadScenarioRequest { ScenarioId = "mini_kanto" });

        var state = await GetFromJsonAsync<StrategyWorldStateDto>("/strategy/state");
        var unit = state!.Units.First(u => u.Id == 1);

        var response = await PostAsJsonAsync(
            $"/strategy/units/{unit.Id}/directive",
            new SetUnitDirectiveRequest { Directive = "Retreat" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadFromJsonAsync<StrategyPolicyChangeResponseDto>(response.Content);
        Assert.NotNull(payload);
        Assert.Equal("CarrierDispatched", payload!.Outcome);
        Assert.Single(payload.State.MessageCarriers);
        Assert.Equal("Retreat", payload.State.MessageCarriers[0].PendingDirective);
        Assert.Equal(state.Lord.X, payload.State.MessageCarriers[0].X);
        Assert.Equal(state.Lord.Y, payload.State.MessageCarriers[0].Y);
    }

    [Fact]
    public async Task AttackOrder_ThenAdvanceDay_ResolvesBattle()
    {
        await PlaceEnemyAdjacentAsync();

        var orderResponse = await PostAsJsonAsync(
            "/strategy/units/1/attack-order",
            new MoveUnitRequest { X = 9, Y = 8 });
        Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);

        var advanceResponse = await PostAsJsonAsync("/strategy/advance-day", new { });
        Assert.Equal(HttpStatusCode.OK, advanceResponse.StatusCode);

        var payload = await ReadFromJsonAsync<StrategyAdvanceDayResponseDto>(advanceResponse.Content);
        Assert.NotNull(payload);
        Assert.Empty(payload!.ResolvedBattles);
        Assert.Contains(
            payload.State.MessageCarriers,
            m => string.Equals(m.PayloadType, "BattleReport", StringComparison.OrdinalIgnoreCase));
        Assert.True(payload.State.Units.First(u => u.Id == 1).Soldiers < 3000);
    }

    [Fact]
    public async Task InstantBattle_AdjacentEnemies_ReducesSoldiers()
    {
        await PlaceEnemyAdjacentAsync();

        var previewResponse = await PostAsJsonAsync(
            "/strategy/units/1/preview-battle",
            new MoveUnitRequest { X = 9, Y = 8 });
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var preview = await ReadFromJsonAsync<StrategyBattlePreviewDto>(previewResponse.Content);
        Assert.NotNull(preview);
        Assert.Equal(2, preview!.DefenderUnitId);
        Assert.InRange(preview.AttackerWinRatePercent, 5, 95);

        var battleResponse = await PostAsJsonAsync(
            "/strategy/units/1/instant-battle",
            new MoveUnitRequest { X = 9, Y = 8 });
        Assert.Equal(HttpStatusCode.OK, battleResponse.StatusCode);

        var battlePayload = await ReadFromJsonAsync<StrategyInstantBattleResponseDto>(battleResponse.Content);
        Assert.NotNull(battlePayload);
        var state = battlePayload!.State;
        var attacker = state.Units.First(u => u.Id == 1);
        var defender = state.Units.First(u => u.Id == 2);
        Assert.True(attacker.Soldiers < 3000);
        Assert.True(defender.Soldiers < 2400);
        Assert.True(attacker.Ap < 5);
        Assert.True(battlePayload.Result.AttackerCasualties >= 0);
        Assert.True(battlePayload.Result.DefenderCasualties >= 0);
    }

    private Task<HttpResponseMessage> LoadEasyScenarioAsync()
        => PostAsJsonAsync("/strategy/load", new LoadScenarioRequest
        {
            ScenarioId = "mini_kanto",
            Difficulty = "Easy",
        });

    private async Task PlaceEnemyAdjacentAsync()
    {
        var loadResponse = await LoadEasyScenarioAsync();
        Assert.Equal(HttpStatusCode.OK, loadResponse.StatusCode);

        var exported = await GetFromJsonAsync<StrategySaveExportResponse>("/strategy/save");
        Assert.NotNull(exported);

        var document = JsonNode.Parse(exported!.Json)!.AsObject();
        var enemy = document["units"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(unit => unit["id"]!.GetValue<int>() == 2);
        enemy["x"] = 9;
        enemy["y"] = 8;
        enemy["route"] = new JsonArray(new JsonObject { ["x"] = 9, ["y"] = 8 });

        // V2 存档以完整运行时快照为准；同步改动该快照以构造相邻敌军场景。
        var runtimeUnits = document["runtimeState"]!["Units"]!.AsObject();
        var runtimeEnemy = runtimeUnits["2"]!.AsObject();
        runtimeEnemy["Location"] = new JsonObject
        {
            ["x"] = 9,
            ["y"] = 8,
            ["z"] = 0
        };

        var restoreResponse = await PostAsJsonAsync(
            "/strategy/restore-save",
            new StrategyRestoreSaveRequest { Json = document.ToJsonString() });
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
    }
}
