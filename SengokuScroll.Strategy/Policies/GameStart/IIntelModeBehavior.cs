using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Policies.GameStart;

/// <summary>情报档位行为（谍报 masking + 单位 DTO 掩码）。</summary>
public interface IIntelModeBehavior
{
    StrategyIntelMode Mode { get; }

    bool RequiresEspionageMask(
        int forceId,
        int playerForceId,
        GameData gameData,
        GameStartOptions options);

    StrategyUnitStateDto ApplyUnitDtoMask(
        StrategyUnitStateDto unit,
        GameWorld world,
        StrategyScenarioMeta meta,
        int playerForceId,
        HashSet<(int X, int Y)> visibleCells,
        StrategyEspionageIntelLedger? espionageLedger,
        GameStartOptions options);

    StrategyStrongholdStateDto ApplyStrongholdDtoMask(
        StrategyStrongholdStateDto dto,
        StrategyScenarioMeta meta,
        GameData gameData,
        StrategyEspionageIntelLedger? espionageLedger,
        GameStartOptions options);
}
