using SengokuScroll.Domain.Enums;

namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// 信仰
/// </summary>
public class ReligionDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public int ReligionGroupId { get; set; }

    public required string ReligionGroupName { get; set; }

    /// <summary>
    /// 宗教等级：冗余字段
    /// <para>由<see cref="ReligionGroupDefinition.Level"/>获得</para> 
    /// </summary>
    public Level3 Level { get; set; }

    /// <summary>
    /// 教义修正主义：宗教组对内属性
    /// <para>1=完全正统：完整解读、会认为修正或放弃教义的3为异端</para>
    /// <para>3=严重异端：放弃原始教义、会抵触原教旨主义者</para>
    /// </summary>
    public Level3 DoctrinalDifference { get; set; }

    /// <summary>
    /// 中央化：宗教组对内属性
    /// <para>1=完全地方主义：反对如教会的中央化、会被地方分离主义利用并且抗税降低税收、但也会以地方为主丧失扩张动力</para>
    /// <para>3=完全中央化：完全由宗教领袖或国家领袖控制、会导致国家收入遭到教会截流遏制且民众不满但也会获得扩张动力、可发动十字军或圣战等宗教战争</para>
    /// </summary>
    public Level3 Centralization { get; set; }

    /// <summary>
    /// 排他主义：宗教组对外属性;
    /// <para>0=不排他：认为其他宗教也有真理、会导致信徒对其他宗教宽容但也会缺乏凝聚力和扩张动力</para>
    /// <para>100=完全排他：认为只有本宗教有真理、会导致信徒对其他宗教敌视迫害但也会获得凝聚力和扩张动力</para>
    /// </summary>
    public byte Exclusivism { get; set; }
}
