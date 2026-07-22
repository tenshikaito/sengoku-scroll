using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Policies.GameStart;

/// <summary>地图视野模式行为（视野计算 + 迷雾 DTO 规则 + 选项约束）。</summary>
public interface IFogModeBehavior
{
    StrategyFogMode Mode { get; }

    bool FogDisabled { get; }

    IVisionPolicy VisionPolicy { get; }

    StrategyFogDtoRules.UnitFogPlacement ClassifyUnit(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility);

    StrategyStrongholdStateDto? ApplyStrongholdFog(
        StrategyStrongholdStateDto dto,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility,
        int mapWidth);

    bool IsMapEntityVisible(
        int x,
        int y,
        StrategyScenarioMeta meta,
        ForceVisibilityState visibility);

    /// <summary>修正与当前迷雾模式冲突的开局选项（如角色视野强制关闭同盟共享视野）。</summary>
    GameStartOptions ApplyConstraints(GameStartOptions options);
}
