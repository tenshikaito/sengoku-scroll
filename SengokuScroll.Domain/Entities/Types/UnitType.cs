namespace SengokuScroll.Domain.Entities.Types;

public sealed class UnitType
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

    public int MaintenanceMoney { get; set; }

    public int MaintenanceFood { get; set; }
}
