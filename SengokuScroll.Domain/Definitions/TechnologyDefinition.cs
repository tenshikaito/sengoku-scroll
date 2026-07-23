namespace SengokuScroll.Domain.Definitions;

/// <summary>技术 Master Data（名称/分类/作用对象/效果幅度）。</summary>
public sealed class TechnologyDefinition
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string Category { get; init; }

    /// <summary>影响对象（农业、商业等）。</summary>
    public required string Target { get; init; }

    /// <summary>效果幅度（增减量；具体语义由 Target 决定）。</summary>
    public int Effectivity { get; init; }
}
