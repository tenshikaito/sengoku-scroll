using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Domain.Systems;

/// <summary>气候/天气日推进占位；影响视野与移动 debuff 的规则表预留中。</summary>
public interface IClimateSystem : IGameSystem
{
}

/// <summary>
/// Domain 层气候占位（Order=1，日循环最先）。天气对 sight/movement 的修正尚未接入日更链。
/// </summary>
public class ClimateSystem : IClimateSystem
{
    /// <summary>日循环最先执行，便于后续系统读取当日天气快照。</summary>
    public int Order { get; } = 1;

    /// <inheritdoc />
    public void Update()
    {
        // 业务：天气日更未实装；见 strategy-fog-of-war-design.md「天气 debuff」。
    }
}
