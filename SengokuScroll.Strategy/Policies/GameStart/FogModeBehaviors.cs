using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Policies.GameStart;

/// <summary>势力/角色迷雾共用的 DTO 过滤逻辑。</summary>
internal abstract class FogEnabledModeBehavior : IFogModeBehavior
{
    public abstract StrategyFogMode Mode { get; }
    public bool FogDisabled => false;
    public abstract IVisionPolicy VisionPolicy { get; }

    public StrategyFogDtoRules.UnitFogPlacement ClassifyUnit(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility)
    {
        if (!unit.IsMilitary || unit.Soldier <= 0)
            return StrategyFogDtoRules.UnitFogPlacement.Exclude;

        var visible = visibility.VisibleCells.Contains((unit.Location.X, unit.Location.Y));
        if (StrategyFogDtoRules.IsDirectPlayerForce(unit.ForceId, meta.PlayerForceId))
            return visible
                ? StrategyFogDtoRules.UnitFogPlacement.Map
                : StrategyFogDtoRules.UnitFogPlacement.Roster;

        if (!visible)
            return StrategyFogDtoRules.UnitFogPlacement.Exclude;

        return StrategyFogDtoRules.UnitFogPlacement.Map;
    }

    public StrategyStrongholdStateDto? ApplyStrongholdFog(
        StrategyStrongholdStateDto dto,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility,
        int mapWidth)
    {
        if (StrategyFogDtoRules.IsDirectPlayerForce(dto.ForceId, meta.PlayerForceId))
            return dto with { VisibilityTier = "Visible" };

        if (gameData is not null
            && StrategyFogDtoRules.IsOwnRealmForce(dto.ForceId, meta.PlayerForceId, gameData))
            return dto with { VisibilityTier = "Visible" };

        if (visibility.VisibleCells.Contains((dto.X, dto.Y)))
            return dto with { VisibilityTier = "Visible" };

        if (visibility.KnownStrongholdIds.Contains(dto.Id))
            return MaskAsKnownStronghold(dto);

        if (visibility.IsExplored(dto.X, dto.Y, mapWidth))
            return MaskAsKnownStronghold(dto);

        return null;
    }

    public bool IsMapEntityVisible(
        int x,
        int y,
        StrategyScenarioMeta meta,
        ForceVisibilityState visibility)
        => visibility.VisibleCells.Contains((x, y));

    public virtual GameStartOptions ApplyConstraints(GameStartOptions options) => options;

    private static StrategyStrongholdStateDto MaskAsKnownStronghold(StrategyStrongholdStateDto dto)
        => dto with
        {
            VisibilityTier = "Known",
            Food = 0,
            Population = 0,
            GarrisonSoldiers = 0,
            GarrisonWounded = 0,
            MilitiaSoldiers = 0,
            TotalSoldiers = 0,
            Technologies = [],
            LaborCapacity = 0,
            LaborAvailable = 0,
            MilitiaAway = 0,
            LaborRatioPercent = 0,
            EffectiveCropPattern = AgricultureCropRules.Single,
            EarlyCropProgressPercent = 0,
            LateCropProgressPercent = 0,
            ThirdCropProgressPercent = 0,
            GarrisonTroopPools = [],
            StandingGarrisonUnits = [],
            CropCycles = [],
            AgricultureProductionPotential = 0,
            KnowsDoubleCrop = false,
            KnowsTripleCrop = false,
            CityActors = [],
            Money = 0,
            Morale = 0,
            Training = 0,
            Defense = 0,
            DefenseFacilities = [],
            EconomyFacilities = [],
            SiegeThreat = null
        };
}

internal sealed class NoFogModeBehavior : IFogModeBehavior
{
    public static readonly NoFogModeBehavior Instance = new();

    public StrategyFogMode Mode => StrategyFogMode.None;
    public bool FogDisabled => true;
    public IVisionPolicy VisionPolicy { get; } = new NoFogVisionPolicy();

    public StrategyFogDtoRules.UnitFogPlacement ClassifyUnit(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility)
        => unit.IsMilitary && unit.Soldier > 0
            ? StrategyFogDtoRules.UnitFogPlacement.Map
            : StrategyFogDtoRules.UnitFogPlacement.Exclude;

    public StrategyStrongholdStateDto? ApplyStrongholdFog(
        StrategyStrongholdStateDto dto,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility,
        int mapWidth)
        => dto with { VisibilityTier = "Visible" };

    public bool IsMapEntityVisible(
        int x,
        int y,
        StrategyScenarioMeta meta,
        ForceVisibilityState visibility)
        => true;

    public GameStartOptions ApplyConstraints(GameStartOptions options) => options;
}

internal sealed class ForceFogModeBehavior : FogEnabledModeBehavior
{
    public static readonly ForceFogModeBehavior Instance = new();

    public override StrategyFogMode Mode => StrategyFogMode.Force;
    public override IVisionPolicy VisionPolicy { get; } = new ForceVisionPolicy();
}

internal sealed class CharacterFogModeBehavior : FogEnabledModeBehavior
{
    public static readonly CharacterFogModeBehavior Instance = new();

    public override StrategyFogMode Mode => StrategyFogMode.Character;
    public override IVisionPolicy VisionPolicy { get; } = new CharacterVisionPolicy();

    public override GameStartOptions ApplyConstraints(GameStartOptions options)
    {
        if (options.FogMode != StrategyFogMode.Character)
            return options;

        return options with
        {
            AllySharedVision = false,
            CharacterSharedVision = false,
            ControlMode = StrategyControlMode.DirectiveOnly
        };
    }
}

public static class FogModeBehaviorFactory
{
    public static IFogModeBehavior Create(StrategyFogMode mode)
        => mode switch
        {
            StrategyFogMode.None => NoFogModeBehavior.Instance,
            StrategyFogMode.Force => ForceFogModeBehavior.Instance,
            StrategyFogMode.Character => CharacterFogModeBehavior.Instance,
            _ => ForceFogModeBehavior.Instance
        };
}
