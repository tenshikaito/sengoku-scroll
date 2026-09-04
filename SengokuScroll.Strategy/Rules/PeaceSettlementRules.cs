using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Entities.Force;

namespace SengokuScroll.Strategy.Rules;

/// <summary>和谈条款合法性、战争分数成本与接受率。</summary>
public static class PeaceSettlementRules
{
    public const int OuterVassalageWarScoreCost = 80;

    public static PeaceSettlementTerms ToDomainTerms(StrategyPeaceTermsDto? dto)
        => new()
        {
            CededStrongholdIds = dto?.CededStrongholdIds
                .Where(id => id > 0)
                .Distinct()
                .OrderBy(id => id)
                .ToList() ?? [],
            ReparationsMoney = Math.Max(0, dto?.ReparationsMoney ?? 0),
            DemandOuterVassalage = dto?.DemandOuterVassalage ?? false,
        };

    public static bool TryBuildPreview(
        GameData gameData,
        int proposerForceId,
        int targetForceId,
        PeaceSettlementTerms terms,
        int baseAcceptanceChance,
        out StrategyPeaceSettlementPreviewDto preview,
        out GameError? error)
    {
        preview = null!;
        error = null;

        var war = WarRules.FindActiveWarBetween(proposerForceId, targetForceId, gameData);
        if (war is null)
        {
            error = GameError.DiplomacyError.NotEnemyForce;
            return false;
        }

        if (!gameData.Forces.TryGetValue(proposerForceId, out var proposer)
            || !gameData.Forces.TryGetValue(targetForceId, out var target))
        {
            error = GameError.DiplomacyError.InvalidForce;
            return false;
        }

        if (terms.ReparationsMoney < 0)
        {
            error = "InvalidPeaceReparations";
            return false;
        }

        var targetStrongholds = gameData.Strongholds.Values
            .Where(s => s.ForceId == targetForceId)
            .ToList();
        var ceded = new List<Stronghold>();
        foreach (var id in terms.CededStrongholdIds.Distinct())
        {
            var stronghold = targetStrongholds.FirstOrDefault(s => s.Id == id);
            if (stronghold is null)
            {
                error = "InvalidPeaceStronghold";
                return false;
            }

            ceded.Add(stronghold);
        }

        if (ceded.Count >= targetStrongholds.Count && ceded.Count > 0)
        {
            error = "PeaceMustLeaveStronghold";
            return false;
        }

        ForceEconomyActions.SyncForceTreasuryFromStrongholds(target, gameData);
        if (terms.ReparationsMoney > target.Money)
        {
            error = "InsufficientPeaceReparations";
            return false;
        }

        if (terms.ReparationsMoney > 0
            && !gameData.Strongholds.Values.Any(s => s.ForceId == proposerForceId))
        {
            error = "PeaceTreasuryUnavailable";
            return false;
        }

        if (terms.DemandOuterVassalage
            && !ForceDiplomacyRules.CanImposeVassalage(proposer, target))
        {
            error = "InvalidPeaceVassalage";
            return false;
        }

        var costs = new List<StrategyPeaceTermCostDto>();
        foreach (var stronghold in ceded)
        {
            var cost = CalculateStrongholdCost(stronghold);
            costs.Add(new StrategyPeaceTermCostDto
            {
                Kind = "CedeStronghold",
                Label = $"割让 {stronghold.Name}",
                WarScoreCost = cost,
            });
        }

        if (terms.ReparationsMoney > 0)
        {
            var cost = CalculateReparationsCost(terms.ReparationsMoney, target.Money);
            costs.Add(new StrategyPeaceTermCostDto
            {
                Kind = "Reparations",
                Label = $"赔款 {terms.ReparationsMoney:N0} 文",
                WarScoreCost = cost,
            });
        }

        if (terms.DemandOuterVassalage)
        {
            costs.Add(new StrategyPeaceTermCostDto
            {
                Kind = "OuterVassalage",
                Label = "成为外藩",
                WarScoreCost = OuterVassalageWarScoreCost,
            });
        }

        var required = costs.Sum(x => x.WarScoreCost);
        if (required > WarRules.MaximumWarScore)
        {
            error = "PeaceTermsExceedMaximumWarScore";
            return false;
        }

        var proposerScore = WarRules.GetWarScoreForForce(war, proposerForceId);
        var forced = proposerScore >= WarRules.MaximumWarScore;
        var chance = forced
            ? 100
            : Math.Clamp(baseAcceptanceChance + proposerScore - required, 5, 95);

        preview = new StrategyPeaceSettlementPreviewDto
        {
            WarId = war.Id,
            ProposerWarScore = proposerScore,
            RequiredWarScore = required,
            AcceptanceChancePercent = chance,
            CanForceAcceptance = forced,
            IsWhitePeace = terms.IsWhitePeace,
            TermCosts = costs,
        };
        return true;
    }

    public static int CalculateStrongholdCost(Stronghold stronghold)
        => Math.Clamp(10 + (Math.Max(1, (int)stronghold.Scale) - 1) * 20 / 29, 10, 30);

    public static int CalculateReparationsCost(int amount, int availableTreasury)
    {
        if (amount <= 0)
            return 0;

        var denominator = Math.Max(1, availableTreasury);
        return Math.Clamp((int)Math.Ceiling(amount * 30d / denominator), 1, 30);
    }
}
