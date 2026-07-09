namespace SengokuScroll.Domain.Entities;


public class StrongholdActor : Actor
{
    public required List<int> CharacterIds { get; set; }

    /// <summary>
    /// 农业产出
    /// </summary>
    public int AgricultureProduction { get; set; }

    /// <summary>
    /// 商业产出
    /// </summary>
    public int CommerceProduction { get; set; }
}
