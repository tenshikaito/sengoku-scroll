using SengokuScroll.Domain.Behaviors.Actions;
using SengokuScroll.Domain.Contexts;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Domain.Systems;

public interface IUnitSystem : IGameSystem
{

}

public class UnitSystem(
    IGameContext context,
    UnitMoveAction moveAction)
    : IUnitSystem
{
    public int Order { get; } = 20;

    public void Update()
    {
        UpdateMove();
    }

    private void UpdateMove()
    {
        // 如果角色在移动状态
        foreach (var o in context.GameWorldContext.EachUnit().Where(oo => oo.Status == UnitStatus.Moving))
        {
            moveAction.Update(o);
        }
    }
}