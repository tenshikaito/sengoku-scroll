using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Vision;

/// <summary>按势力维护 explored / visible / known 据点，并在 DTO 映射时过滤。</summary>
public sealed class StrategyVisibilityLedger
{
    private readonly Dictionary<int, ForceVisibilityState> byForce = [];

    public ForceVisibilityState GetOrCreate(int forceId)
    {
        if (!byForce.TryGetValue(forceId, out var state))
        {
            state = new ForceVisibilityState();
            byForce[forceId] = state;
        }

        return state;
    }

    public void Initialize(GameWorld world, StrategyScenarioMeta meta)
    {
        byForce.Clear();
        var tileMap = world.GameMapMasterData.TileMap;
        var playerForceId = meta.PlayerForceId;
        var state = GetOrCreate(playerForceId);
        state.EnsureCapacity(tileMap.Width, tileMap.Height);
        state.KnownStrongholdIds.Clear();

        var realmRoot = TributeRoutingHelper.ResolveRealmRootForceId(playerForceId, world.GameData);
        foreach (var stronghold in world.GameData.Strongholds.Values)
        {
            if (TributeRoutingHelper.ResolveRealmRootForceId(stronghold.ForceId, world.GameData) != realmRoot)
                continue;

            RegisterKnownStronghold(state, stronghold.Id, stronghold.Location.X, stronghold.Location.Y, tileMap.Width);
        }

        foreach (var knownId in meta.KnownStrongholdIds)
        {
            if (!world.GameData.Strongholds.TryGetValue(knownId, out var known))
                continue;

            RegisterKnownStronghold(state, known.Id, known.Location.X, known.Location.Y, tileMap.Width);
        }

        Recompute(world, meta);
    }

    public void Recompute(GameWorld world, StrategyScenarioMeta meta)
    {
        var options = meta.StartOptions;
        var tileMap = world.GameMapMasterData.TileMap;
        var playerForceId = meta.PlayerForceId;
        var state = GetOrCreate(playerForceId);
        state.EnsureCapacity(tileMap.Width, tileMap.Height);
        state.VisibleCells.Clear();

        var policy = VisionPolicyFactory.Create(options.FogMode);
        var visible = policy.ComputeVisibleTiles(world, meta, playerForceId, options);
        foreach (var cell in visible)
            state.VisibleCells.Add(cell);

        if (options.FogMode == StrategyFogMode.None)
        {
            for (var y = 0; y < tileMap.Height; y++)
            {
                for (var x = 0; x < tileMap.Width; x++)
                    state.MarkExplored(x, y, tileMap.Width);
            }
        }
        else
        {
            state.MarkExplored(visible, tileMap.Width);
            foreach (var strongholdId in state.KnownStrongholdIds)
            {
                if (!world.GameData.Strongholds.TryGetValue(strongholdId, out var sh))
                    continue;

                state.MarkExplored(sh.Location.X, sh.Location.Y, tileMap.Width);
            }
        }
    }

    public void RegisterKnownStronghold(
        ForceVisibilityState state,
        int strongholdId,
        int x,
        int y,
        int mapWidth)
    {
        state.KnownStrongholdIds.Add(strongholdId);
        state.MarkExplored(x, y, mapWidth);
    }

    public StrategyVisibilityDto BuildDto(GameWorld world, StrategyScenarioMeta meta)
    {
        var options = meta.StartOptions;
        var tileMap = world.GameMapMasterData.TileMap;
        var state = GetOrCreate(meta.PlayerForceId);

        return new StrategyVisibilityDto
        {
            FogMode = options.FogMode.ToString(),
            IntelMode = options.IntelMode.ToString(),
            ControlMode = options.ControlMode.ToString(),
            InstantEventMessages = options.InstantEventMessages,
            AllySharedVision = options.AllySharedVision,
            MapWidth = tileMap.Width,
            MapHeight = tileMap.Height,
            ExploredBits = state.PackExploredBits(),
            VisibleCells = state.VisibleCells
                .Select(c => new StrategyMapCellDto { X = c.X, Y = c.Y })
                .OrderBy(c => c.Y)
                .ThenBy(c => c.X)
                .ToList(),
            KnownStrongholdIds = state.KnownStrongholdIds.OrderBy(id => id).ToList()
        };
    }

    public StrategyVisibilitySaveDto Capture(int playerForceId, int mapWidth, int mapHeight)
    {
        var state = GetOrCreate(playerForceId);
        return new StrategyVisibilitySaveDto
        {
            ExploredBits = state.PackExploredBits(),
            KnownStrongholdIds = state.KnownStrongholdIds.OrderBy(id => id).ToList()
        };
    }

    public void ApplySave(int playerForceId, StrategyVisibilitySaveDto save, int mapWidth, int mapHeight)
    {
        var state = GetOrCreate(playerForceId);
        state.EnsureCapacity(mapWidth, mapHeight);
        state.KnownStrongholdIds.Clear();
        foreach (var id in save.KnownStrongholdIds)
            state.KnownStrongholdIds.Add(id);

        state.UnpackExploredBits(save.ExploredBits, mapWidth, mapHeight);
    }

    public bool IsVisible(int forceId, int x, int y)
        => GetOrCreate(forceId).VisibleCells.Contains((x, y));

    public bool IsExplored(int forceId, int x, int y, int mapWidth)
        => GetOrCreate(forceId).IsExplored(x, y, mapWidth);

    public bool IsKnownStronghold(int forceId, int strongholdId)
        => GetOrCreate(forceId).KnownStrongholdIds.Contains(strongholdId);
}
