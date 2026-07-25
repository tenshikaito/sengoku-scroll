using SengokuScroll.Domain;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>技术 Master + 实体条目合并为情报 DTO。</summary>
public static class TechnologyIntelHelper
{
    public static IReadOnlyList<StrategyEntityTechnologyDto> MapEntityTechnologies(
        IReadOnlyList<EntityTechnology> technologies,
        GameMasterData masterData)
    {
        if (technologies.Count == 0)
            return [];

        return [.. technologies
            .OrderBy(t => t.TechnologyId)
            .Select(t =>
            {
                masterData.Technologies.TryGetValue(t.TechnologyId, out var definition);
                return new StrategyEntityTechnologyDto
                {
                    Id = t.TechnologyId,
                    Name = definition?.Name ?? $"技术#{t.TechnologyId}",
                    Category = definition?.Category ?? "—",
                    Target = definition?.Target,
                    Effectivity = definition?.Effectivity,
                    Status = t.Status,
                };
            })];
    }

    public static void SyncStrongholdTechnologiesFromAgriculture(Stronghold stronghold)
    {
        var agriculture = stronghold.Agriculture;
        if (agriculture is null)
            return;

        UpsertTechnology(stronghold.Technologies, technologyId: 1, completed: agriculture.KnowsDoubleCrop);
        UpsertTechnology(stronghold.Technologies, technologyId: 2, completed: agriculture.KnowsTripleCrop);
    }

    private static void UpsertTechnology(
        IList<EntityTechnology> technologies,
        int technologyId,
        bool completed)
    {
        if (!completed)
        {
            for (var i = technologies.Count - 1; i >= 0; i--)
            {
                if (technologies[i].TechnologyId == technologyId)
                    technologies.RemoveAt(i);
            }

            return;
        }

        var existing = technologies.FirstOrDefault(t => t.TechnologyId == technologyId);
        if (existing is null)
        {
            technologies.Add(new EntityTechnology
            {
                TechnologyId = technologyId,
                Status = 1,
            });
            return;
        }

        existing.Status = 1;
    }
}
