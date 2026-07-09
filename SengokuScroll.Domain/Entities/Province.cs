namespace SengokuScroll.Domain.Entities;

public sealed class Province
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// 所属势力
    /// </summary>
    public int ForceId { get; set; }

    /// <summary>
    /// 省会
    /// </summary>
    public int StrongholdId { get; set; }

    public required List<int> StrongholdIds { get; set; }
}
