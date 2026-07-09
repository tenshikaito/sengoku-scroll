using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain.Entities;

public class Actor : IHasForce
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public ActorType Type { get; set; }

    public int ForceId { get; set; }

    public int StrongholdId { get; set; }

    public int LeaderId { get; set; }

    public int CultureId { get; set; }

    public int RegligionId { get; set; }

    public int Money { get; set; }

    public int Food { get; set; }

    public int Wood { get; set; }

    public int Iron { get; set; }

    public int Copper { get; set; }

    public int Horse { get; set; }

    public int Matchlock { get; set; }

    public int Cannon { get; set; }

    public int Boat { get; set; }

    public int Ship { get; set; }

    public int Fleet { get; set; }

    /// <summary>
    /// 民心
    /// </summary>
    public byte PopularFeelings { get; set; }

    /// <summary>
    /// 兵数
    /// </summary>
    public int Soldier { get; set; }

    /// <summary>
    /// 训练度
    /// </summary>
    public byte Training { get; set; }

    /// <summary>
    /// 士气
    /// </summary>
    public byte Morale { get; set; }

    /// <summary>
    /// 伤兵
    /// </summary>
    public int Patient { get; set; }

    public required List<int> SubUnitIds { get; set; }

    public GameDate LastAiCheckDate { get; set; }

    public static implicit operator int(Actor actor) => actor.Id;
}
