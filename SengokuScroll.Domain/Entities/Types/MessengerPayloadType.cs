namespace SengokuScroll.Domain.Entities.Types;

/// <summary>信使携带的信息类型。</summary>
public enum MessengerPayloadType
{
    /// <summary>变更部队战斗/攻城方针。</summary>
    PolicyChange,

    /// <summary>远程战略指令（进攻目标、驻留区域等）。</summary>
    StrategicOrder,

    /// <summary>战斗结果战报；到达后玩家方可查看详情。</summary>
    BattleReport,

    /// <summary>战略情报（溃灭、占城等）；抵达当主后解锁消息详情。</summary>
    StrategicReport,

    /// <summary>向运输队传递假情报，使其改向或停留（谍报玩法）。</summary>
    FalseIntelligence
}
