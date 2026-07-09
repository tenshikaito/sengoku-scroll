namespace SengokuScroll.Domain.Entities;

/// <summary>
/// Ô­ÁÏ
/// </summary>
public class MaterialResource
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int Rarity { get; set; }
}
