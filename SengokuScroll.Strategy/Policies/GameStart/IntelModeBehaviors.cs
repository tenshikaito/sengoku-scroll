using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Policies.GameStart;

internal sealed class FullIntelModeBehavior : IIntelModeBehavior
{
    public static readonly FullIntelModeBehavior Instance = new();

    public StrategyIntelMode Mode => StrategyIntelMode.Full;

    public bool RequiresEspionageMask(
        int forceId,
        int playerForceId,
        GameData gameData,
        GameStartOptions options)
        => false;

    public StrategyUnitStateDto ApplyUnitDtoMask(
        StrategyUnitStateDto unit,
        GameWorld world,
        StrategyScenarioMeta meta,
        int playerForceId,
        HashSet<(int X, int Y)> visibleCells,
        StrategyEspionageIntelLedger? espionageLedger,
        GameStartOptions options)
        => unit;

    public StrategyStrongholdStateDto ApplyStrongholdDtoMask(
        StrategyStrongholdStateDto dto,
        StrategyScenarioMeta meta,
        GameData gameData,
        StrategyEspionageIntelLedger? espionageLedger,
        GameStartOptions options)
        => dto;
}

internal sealed class ForceIntelModeBehavior : IIntelModeBehavior
{
    public static readonly ForceIntelModeBehavior Instance = new();

    public StrategyIntelMode Mode => StrategyIntelMode.ForceIntel;

    public bool RequiresEspionageMask(
        int forceId,
        int playerForceId,
        GameData gameData,
        GameStartOptions options)
        => EspionageIntelRules.RequiresEspionageMask(forceId, playerForceId, gameData, options);

    public StrategyUnitStateDto ApplyUnitDtoMask(
        StrategyUnitStateDto unit,
        GameWorld world,
        StrategyScenarioMeta meta,
        int playerForceId,
        HashSet<(int X, int Y)> visibleCells,
        StrategyEspionageIntelLedger? espionageLedger,
        GameStartOptions options)
        => EspionageIntelRules.ApplyUnitMask(
            unit,
            playerForceId,
            world.GameData,
            espionageLedger,
            options);

    public StrategyStrongholdStateDto ApplyStrongholdDtoMask(
        StrategyStrongholdStateDto dto,
        StrategyScenarioMeta meta,
        GameData gameData,
        StrategyEspionageIntelLedger? espionageLedger,
        GameStartOptions options)
        => EspionageIntelRules.ApplyStrongholdMask(
            dto,
            meta.PlayerForceId,
            gameData,
            espionageLedger,
            options);
}

public static class IntelModeBehaviorFactory
{
    public static IIntelModeBehavior Create(StrategyIntelMode mode)
        => mode switch
        {
            StrategyIntelMode.Full => FullIntelModeBehavior.Instance,
            StrategyIntelMode.ForceIntel => ForceIntelModeBehavior.Instance,
            _ => ForceIntelModeBehavior.Instance
        };
}
