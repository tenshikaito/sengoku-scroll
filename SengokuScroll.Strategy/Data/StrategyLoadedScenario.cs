using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Data;

/// <summary>剧本加载结果：世界状态 + 元数据。</summary>
public sealed record StrategyLoadedScenario(GameWorld World, StrategyScenarioMeta Meta);
