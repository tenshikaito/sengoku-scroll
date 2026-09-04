using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Domain.Systems;

/// <summary>经济日推进占位接口；策略模式由 <see cref="SengokuScroll.Strategy.Systems.StrategyEconomySystem"/> 实装。</summary>
public interface IEconomySystem : IGameSystem
{

}

/// <summary>
/// Domain 层经济系统占位（RPG 模式可在此挂接日产/税）。
/// 大战略的全部经济逻辑在 Strategy 层 <see cref="SengokuScroll.Strategy.Systems.StrategyEconomySystem"/>。
/// </summary>
public class EconomySystem : IEconomySystem
{
    /// <summary>与市场系统同级 Order，便于 Strategy 层按相同顺序替换。</summary>
    public int Order { get; } = 10;

    /// <inheritdoc />
    public void Update()
    {
        // 业务：Domain 默认无日更；具体模式在 Strategy/RPG 子类中实现。
    }
}
