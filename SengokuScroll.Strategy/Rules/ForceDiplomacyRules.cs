using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using static SengokuScroll.Domain.Entities.Diplomacy;
using static SengokuScroll.Domain.Entities.Force;

namespace SengokuScroll.Strategy.Rules;

/// <summary>外交/外政指令校验。</summary>
public static class ForceDiplomacyRules
{
    public static bool TryGetForce(GameData gameData, int forceId, out Force force)
        => gameData.Forces.TryGetValue(forceId, out force!);

    public static bool IsIndependentOrOuterVassal(Force force)
        => force.Status is ForceStatus.Independence or ForceStatus.OuterVassal;

    public static bool CanPlayerOrderDiplomacy(StrategyScenarioMeta meta, int actingForceId)
        => actingForceId == meta.PlayerForceId;

    public static bool CanImposeVassalage(Force suzerain, Force target)
        => suzerain.Status == ForceStatus.Independence
           && target.Status == ForceStatus.Independence
           && target.SuzerainForceId is null;

    public static bool CanSubmitVassalage(Force submitter, Force suzerain)
        => submitter.Status == ForceStatus.Independence
           && suzerain.Status == ForceStatus.Independence;

    public static bool CanReleaseVassal(Force suzerain, Force vassal)
        => vassal.SuzerainForceId == suzerain.Id
           && vassal.Status is ForceStatus.OuterVassal or ForceStatus.InnerVassal;

    public static bool CanDeclareIndependence(Force vassal)
        => vassal.Status is ForceStatus.OuterVassal or ForceStatus.InnerVassal
           && vassal.SuzerainForceId is not null;

    public static bool CanAppointInnerVassal(Force suzerain, Force target)
        => suzerain.Status == ForceStatus.Independence
           && target.Status == ForceStatus.Independence
           && target.Id != suzerain.Id;

    public static bool CanRevokeInnerVassal(Force suzerain, Force vassal)
        => vassal.Status == ForceStatus.InnerVassal && vassal.SuzerainForceId == suzerain.Id;
}
