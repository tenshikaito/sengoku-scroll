namespace SengokuScroll.Domain;

public sealed class GameError(string code, params object[] data)
{
    public string Code { get; } = code;

    public object[]? Data { get; } = data;

    public static implicit operator GameError(string code) => new(code);

    /// <summary>
    /// 超出地图范围
    /// </summary>
    public static GameError OutOfMapBoundError { get; } = new(nameof(OutOfMapBoundError));

    /// <summary>
    /// 行动力不足
    /// </summary>
    public static GameError ApNotEnough { get; } = new(nameof(ApNotEnough));

    /// <summary>
    /// 目标位置与当前位置不相邻
    /// </summary>
    public static GameError TargetLocationNotAdjacent { get; } = new(nameof(TargetLocationNotAdjacent));

    /// <summary>
    /// 找不到数据
    /// </summary>
    public static GameError DataNotFound { get; } = new(nameof(DataNotFound));

    public static class ForceError
    {
        /// <summary>
        /// 没有势力
        /// </summary>
        public static GameError ForceNotFound { get; } = new(nameof(ForceNotFound));
    }

    public static class StrongholdError
    {
        /// <summary>
        /// 没有据点
        /// </summary>
        public static GameError StrongholdNotFound { get; } = new(nameof(StrongholdNotFound));

        /// <summary>当主居城不可任命外臣领主，须保持直辖。</summary>
        public static GameError CannotAppointLordToResidence { get; } = new(nameof(CannotAppointLordToResidence));
    }

    public static class CharacterError
    {
        /// <summary>
        /// 没有角色
        /// </summary>
        public static GameError CharacterNotFound { get; } = new(nameof(CharacterNotFound));
    }

    public static class UnitError
    {
        /// <summary>
        /// 没有单位
        /// </summary>
        public static GameError UnitNotFound { get; } = new(nameof(UnitNotFound));

        /// <summary>没有找到攻击目标</summary>
        public static GameError AttackTargetNotFound { get; } = new(nameof(AttackTargetNotFound));

        /// <summary>指令模式下仅当主所在格部队可直接操作。</summary>
        public static GameError UnitNotDirectlyControllable { get; } = new(nameof(UnitNotDirectlyControllable));
    }

    public static class MovementError
    {
        /// <summary>
        /// 指定位置已经存在单位
        /// </summary>
        public static GameError UnitAlreadyExistsInTile { get; } = new(nameof(UnitAlreadyExistsInTile));
        /// <summary>
        /// 不能移动到指定位置
        /// </summary>
        public static GameError CannotMoveToTile { get; } = new(nameof(CannotMoveToTile));

        /// <summary>据点被封锁/包围，须确认强行出入。</summary>
        public static GameError StrongholdBlockaded { get; } = new(nameof(StrongholdBlockaded));
    }

    public static class DiplomacyError
    {
        /// <summary>
        /// 当前势力是自势力
        /// </summary>
        public static GameError SelfForce { get; } = new(nameof(SelfForce));
        /// <summary>
        /// 当前势力不是自势力
        /// </summary>
        public static GameError NotSelfForce { get; } = new(nameof(NotSelfForce));
        /// <summary>
        /// 当前势力是同盟势力
        /// </summary>
        public static GameError AllyForce { get; } = new(nameof(AllyForce));
        /// <summary>
        /// 当前势力不是同盟势力
        /// </summary>
        public static GameError NotAllyForce { get; } = new(nameof(NotAllyForce));
        /// <summary>
        /// 当前势力是敌对势力
        /// </summary>
        public static GameError EnemyForce { get; } = new(nameof(EnemyForce));
        /// <summary>
        /// 当前势力是敌对势力
        /// </summary>
        public static GameError NotEnemyForce { get; } = new(nameof(NotEnemyForce));
        /// <summary>
        /// 当前势力是停战势力
        /// </summary>
        public static GameError TruceForce { get; } = new(nameof(TruceForce));
        /// <summary>
        /// 当前势力不是停战势力
        /// </summary>
        public static GameError NotTruceForce { get; } = new(nameof(NotTruceForce));
        /// <summary>
        /// 当前势力无效
        /// </summary>
        public static GameError InvalidForce { get; } = new(nameof(InvalidForce));
    }

    public static class DomesticError
    {
        /// <summary>当主不在居城，无法下达据点内政指令。</summary>
        public static GameError LordNotAtResidence { get; } = new(nameof(LordNotAtResidence));

        /// <summary>已任命领主领地，税率由领主自决，当主不可干涉。</summary>
        public static GameError AppointedLordTerritory { get; } = new(nameof(AppointedLordTerritory));

        /// <summary>将领不在当主居城，无法任命为领主。</summary>
        public static GameError CharacterNotAtResidence { get; } = new(nameof(CharacterNotAtResidence));

        /// <summary>该将领已担任据点领主，不能兼任代官。</summary>
        public static GameError CharacterIsStrongholdLord { get; } = new(nameof(CharacterIsStrongholdLord));

        /// <summary>当主不能担任据点代官。</summary>
        public static GameError CharacterIsForceLord { get; } = new(nameof(CharacterIsForceLord));
    }
}
