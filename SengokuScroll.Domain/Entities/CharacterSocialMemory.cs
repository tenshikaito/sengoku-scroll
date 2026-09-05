namespace SengokuScroll.Domain.Entities;

/// <summary>已发生事件的有限审计记忆，不重复叠加关系效果。Id 在人物内单调递增。</summary>
public sealed record CharacterSocialMemory(long Id, int Day, int OtherCharacterId, string Kind, string Description);
