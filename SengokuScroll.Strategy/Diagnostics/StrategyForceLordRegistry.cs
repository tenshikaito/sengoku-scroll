using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>运行时当主角色 Id（继承后覆盖剧本初始映射）。</summary>
public sealed class StrategyForceLordRegistry
{
    private readonly Dictionary<int, int> lordCharacterByForce = new();

    public void Initialize(StrategyScenarioMeta meta)
    {
        lordCharacterByForce.Clear();
        foreach (var (forceId, characterId) in meta.ForceLordCharacterIds)
            lordCharacterByForce[forceId] = characterId;
    }

    public bool TryGetLordCharacterId(int forceId, out int characterId)
        => lordCharacterByForce.TryGetValue(forceId, out characterId);

    public void SetLordCharacterId(int forceId, int characterId)
        => lordCharacterByForce[forceId] = characterId;
}
