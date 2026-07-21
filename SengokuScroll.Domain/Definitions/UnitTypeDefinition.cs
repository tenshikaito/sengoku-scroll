namespace SengokuScroll.Domain.Definitions;

public sealed class UnitTypeDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public int Attack { get; set; }

    public int Defense { get; set; }

    public int AttackRange { get; set; }

    public int Movement { get; set; }

    /// <summary>
    /// 文化特有类型
    /// </summary>
    public int? CultureId { get; set; }

    public int Cost { get; set; }

    /// <summary>战略地图视野半径（曼哈顿距离 |dx|+|dy| ≤ range）。</summary>
    public int SightRange { get; set; } = 2;

    public int MaintenanceMoney { get; set; }

    public int MaintenanceFood { get; set; }
}
