namespace SengokuScroll.Domain.Entities;

/// <summary>实体已掌握/研究中的技术条目。</summary>
public sealed class EntityTechnology
{
    public int TechnologyId { get; set; }

    /// <summary>0=研究中；1=已完成。</summary>
    public byte Status { get; set; }
}
