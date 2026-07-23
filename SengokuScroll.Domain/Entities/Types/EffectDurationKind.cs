namespace SengokuScroll.Domain.Entities.Types;

/// <summary>
/// 增减益 / 看法条目的持续类型。
/// UI「程度」列由 <c>FormatDuration</c> 格式化；<see cref="Entities.EntityEffect.ExpiresOn"/> 仅 Temporary 必填。
/// </summary>
public enum EffectDurationKind : byte
{
    /// <summary>永久生效；ExpiresOn 忽略。</summary>
    Permanent = 0,

    /// <summary>长期生效（无固定到期日，事件/和约等可移除）；ExpiresOn 通常为空。</summary>
    LongTerm = 1,

    /// <summary>临时生效；须设置 ExpiresOn，日推进后到期移除（待 System 实装）。</summary>
    Temporary = 2,
}
