//using SengokuScroll.Domain.Rules.Abstraction;
//using SengokuScroll.Core.Models.Abstraction;
//using SengokuScroll.Common.Types;

//namespace SengokuScroll.Domain.Rules;

//public class EnterStrongholdRuleEvaluator : RulesValidatorBase<EnterStrongholdContext>
//{
//    protected override IGameRule<EnterStrongholdContext>[] Rules =>
//    [
//        new CheckOutOfBoundRule<EnterStrongholdContext>(),
//        new CheckMovementApRule<EnterStrongholdContext>(),
//    ];

//    public GameValueResult Validate(IGameWorldContext ctx, IMovable movable, Point2 location)
//        => Validate(new EnterStrongholdContext(ctx, movable, location));
//}

//public readonly struct EnterStrongholdContext(
//    IGameWorldContext world,
//    IMovable movable,
//    Point2 location) : ITargetLocationContext
//{
//    public IGameWorldContext World { get; } = world;

//    public IMovable Movable { get; } = movable;

//    public Point2 TargetLocation { get; } = location;

//    public ITerrain? Terrain { get; } = world.GetTerrainOrDefault(location);

//    public IHasLocation Source => Movable;
//}
