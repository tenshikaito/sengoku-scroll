using SengokuScroll.Domain.Enums;

namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// 宗教
/// </summary>
public class ReligionGroupDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    /// <summary>
    /// 宗教等级
    /// <para>1=低级宗教：多为多神教、较为原始、没有教义没有理论、类似泛灵教或自然崇拜</para>
    /// <para>3=高级宗教：多为一神教、有较为完善的教义和理论、如基督教或佛教</para>
    /// <remarks>高级宗教对低级宗教传播有优势、反之亦然</remarks>
    /// </summary>
    public Level3 Level { get; set; }
}
