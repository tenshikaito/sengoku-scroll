using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;

namespace SengokuScroll.Strategy.Systems;

/// <summary>已废弃自动踩格占城；占领改由攻城指令与战后结算处理。</summary>
public interface IStrategyStrongholdOccupationSystem : IGameSystem
{
}

public sealed class StrategyStrongholdOccupationSystem : IStrategyStrongholdOccupationSystem
{
    public int Order { get; } = 23;

    /// <inheritdoc />
    public void Update()
    {
        // 业务：占城仅通过攻城指令与战后结算触发，本系统不再处理踩格自动占领。
    }
}
