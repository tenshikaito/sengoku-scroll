namespace SengokuScroll.Domain.Entities;

/// <summary>最近一次行为评估，不是已发生事件；按行为类型覆盖，最多 5 类。</summary>
public sealed record CharacterDecisionRecord(int Day, string Behavior, int TargetCharacterId,
    int Score, int Threshold, string Outcome, IReadOnlyList<CharacterDecisionFactor> Factors);

public sealed record CharacterDecisionFactor(string Name, int Value);
