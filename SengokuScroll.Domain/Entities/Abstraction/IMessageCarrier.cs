using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Entities.Abstraction;

/// <summary>
/// 在地图格间携带文书（方针、战报、谍报等）的载体抽象；由单位护送编制或匿名角色实现。
/// </summary>
public interface IMessageCarrier : IMapObject, IHasForce
{
    MessageCarrierKind CarrierKind { get; }

    MessagePayload Payload { get; }

    MessageCarrierStatus Status { get; }

    Queue<Point3> RoutePoints { get; }
}
