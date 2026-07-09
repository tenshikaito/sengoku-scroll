using SengokuScroll.Common.Types;

namespace SengokuScroll.Domain.Entities;

public sealed class Road
{
    public int Id { get; set; }

    public int TypeId { get; set; }

    /// <summary>
    /// 可以加速的方向
    /// </summary>
    public CardinalPattern AllowedDirection { get; set; }

    /// <summary>
    /// 海拔
    /// </summary>
    /// <remarks>表示道路的海拔高度、如果是隧道则为0、如果是桥梁则为0以上的值等</remarks>
    public int? Altitude { get; set; }
}
