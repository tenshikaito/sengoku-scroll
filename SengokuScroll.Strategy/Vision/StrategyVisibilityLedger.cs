using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Common.Types;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Policies.GameStart;

namespace SengokuScroll.Strategy.Vision;

/// <summary>按势力维护 explored / visible / known 据点，并在 DTO 映射时过滤。</summary>
public sealed class StrategyVisibilityLedger
{
    private readonly Dictionary<int, ForceVisibilityState> byForce = [];
    private readonly Dictionary<int, Dictionary<int, CastleObservation>> castles = [];
    public sealed record CastleObservation(int Id, string Name, int ForceId, Point3 Location,
        byte Defense, int Garrison, int ObservedDay);
    public sealed record ForceSnapshot(int ForceId, int Width, int Height, IReadOnlyList<uint> ExploredBits,
        IReadOnlyList<int> KnownStrongholdIds, IReadOnlyList<CastleObservation> Castles);

    public IReadOnlyList<ForceSnapshot> SnapshotAll() => byForce.OrderBy(p => p.Key).Select(p =>
        new ForceSnapshot(p.Key, p.Value.Width, p.Value.Height, p.Value.PackExploredBits(),
            p.Value.KnownStrongholdIds.Order().ToArray(),
            castles.GetValueOrDefault(p.Key)?.Values.OrderBy(c => c.Id).ToArray() ?? [])).ToArray();

    public void RestoreAll(IReadOnlyList<ForceSnapshot> snapshots)
    {
        foreach (var s in snapshots)
        {
            if (!byForce.TryGetValue(s.ForceId, out var state) || s.Width != state.Width || s.Height != state.Height
                || s.KnownStrongholdIds is null || s.Castles is null)
                throw new InvalidOperationException("Invalid force visibility snapshot");
            ApplySave(s.ForceId, new StrategyVisibilitySaveDto
            { ExploredBits = s.ExploredBits, KnownStrongholdIds = s.KnownStrongholdIds }, s.Width, s.Height);
            castles[s.ForceId] = s.Castles.ToDictionary(c => c.Id);
        }
    }

    // Detached, last-seen values: no references to mutable hidden castles or garrisons.
    public IReadOnlyList<Stronghold> ObserveStrongholds(int forceId, int today)
        => castles.TryGetValue(forceId, out var known) ? known.Values.OrderBy(c => c.Id).Select(c =>
            new Stronghold
            {
                Id = c.Id, Name = c.Name, ForceId = c.ForceId, Location = c.Location,
                Defense = today - c.ObservedDay <= 90 ? c.Defense : (byte)50,
                ForceActor = new() { Name = c.Name, CharacterIds = [], SubUnitIds = [],
                    Soldier = today - c.ObservedDay <= 90 ? c.Garrison : 1000 },
                CivilianActor = new() { Name = "", CharacterIds = [], SubUnitIds = [] },
                MerchantActors = [], ReligionActors = [], Market = new(), HasCoreForceIds = [],
                Agriculture = new(), DefenseFacilityIds = [], EconomyFacilityIds = []
            }).ToArray() : [];

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
        castles.Clear();
        var tileMap = world.GameMapMasterData.TileMap;
        foreach (var forceId in world.GameData.Forces.Keys)
        {
            var state = GetOrCreate(forceId);
            state.EnsureCapacity(tileMap.Width, tileMap.Height);
            state.KnownStrongholdIds.Clear();
            var realmRoot = TributeRoutingHelper.ResolveRealmRootForceId(forceId, world.GameData);
            foreach (var stronghold in world.GameData.Strongholds.Values)
            {
                // AI 掌握剧本公开的城址（战略地图常识），但敌军仍须进入实时视野才可感知。
                // 玩家继续遵循剧本 KnownStrongholdIds 与己方领地配置。
                if ((meta.HasHumanControlConfiguration || forceId == meta.PlayerForceId)
                    && TributeRoutingHelper.ResolveRealmRootForceId(stronghold.ForceId, world.GameData) != realmRoot)
                    continue;

                RegisterKnownStronghold(
                    state,
                    stronghold.Id,
                    stronghold.Location.X,
                    stronghold.Location.Y,
                    tileMap.Width);
            }
        }

        foreach (var forceId in meta.HasHumanControlConfiguration ? world.GameData.Forces.Keys.ToArray() : [meta.PlayerForceId])
        foreach (var knownId in meta.KnownStrongholdIds)
        {
            if (!world.GameData.Strongholds.TryGetValue(knownId, out var known))
                continue;

            RegisterKnownStronghold(GetOrCreate(forceId), known.Id, known.Location.X, known.Location.Y, tileMap.Width);
        }

        foreach (var (forceId, state) in byForce)
            castles[forceId] = world.GameData.Strongholds.Values.Where(s => state.KnownStrongholdIds.Contains(s.Id))
                .ToDictionary(s => s.Id, s => new CastleObservation(s.Id, s.Name, s.ForceId, s.Location, 50, 1000, -1000));

        Recompute(world, meta);
    }

    public void Recompute(GameWorld world, StrategyScenarioMeta meta)
    {
        var options = meta.StartOptions;
        var tileMap = world.GameMapMasterData.TileMap;
        var profile = GameStartOptionsProfile.Create(options, meta.Difficulty);
        var forceIds = world.GameData.Forces.Keys.OrderBy(id => id).ToArray();
        var visibleSnapshots = new HashSet<(int X, int Y)>[forceIds.Length];

        void ComputeVisibility(int index)
        {
            var forceId = forceIds[index];
            // 玩家严格遵循开局迷雾；AI 使用势力视野，避免角色迷雾错误复用玩家当主位置。
            var visionPolicy = meta.HasHumanControlConfiguration || forceId == meta.PlayerForceId
                ? profile.Fog.VisionPolicy
                : ForceVisionPolicyInstance;
            var perspective = meta.HasHumanControlConfiguration
                ? StrategyForcePerspective.Create(meta, world.GameData, forceId) : meta;
            visibleSnapshots[index] = visionPolicy.ComputeVisibleTiles(world, perspective, forceId, options);
        }

        // 可见格计算彼此独立且只读世界状态，适合安全并行；应用结果仍固定按 ForceId 顺序进行。
        StrategyParallelWork.ForEachIndex(
            forceIds.Length,
            ComputeVisibility,
            minimumParallelCount: 4);

        for (var index = 0; index < forceIds.Length; index++)
        {
            var forceId = forceIds[index];
            var state = GetOrCreate(forceId);
            state.EnsureCapacity(tileMap.Width, tileMap.Height);
            state.VisibleCells.Clear();
            var visible = visibleSnapshots[index];
            foreach (var cell in visible)
                state.VisibleCells.Add(cell);

            if ((meta.HasHumanControlConfiguration || forceId == meta.PlayerForceId) && profile.Fog.FogDisabled)
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

            foreach (var stronghold in world.GameData.Strongholds.Values)
            {
                if (state.VisibleCells.Contains((stronghold.Location.X, stronghold.Location.Y)))
                {
                    state.KnownStrongholdIds.Add(stronghold.Id);
                    if (!castles.TryGetValue(forceId, out var known)) castles[forceId] = known = [];
                    known[stronghold.Id] = new(stronghold.Id, stronghold.Name, stronghold.ForceId,
                        stronghold.Location, stronghold.Defense, StrongholdGarrisonRules.CountTotalGarrisonAt(stronghold, world.GameData),
                        world.GameData.GameDate.TotalDays);
                }
            }
        }
    }

    private static readonly ForceVisionPolicy ForceVisionPolicyInstance = new();

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
            CharacterSharedVision = options.CharacterSharedVision,
            ShowAllyIntel = options.ShowAllyIntel,
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
