using SengokuScroll.Domain;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Vision;

/// <summary>
/// 谍报情报 DTO 遮蔽：非自势力（含内藩）目标须台账登记后才展示；
/// 视野内也不暴露兵数/内政具体值。
/// </summary>
public static class EspionageIntelRules
{
    private const string UnknownDisplay = "未知";

    /// <summary>是否须谍报 masking（自势力圈内的据点/部队对玩家完全可见）。</summary>
    public static bool RequiresEspionageMask(int forceId, int playerForceId, GameData gameData)
        => !StrategyFogDtoRules.IsOwnRealmForce(forceId, playerForceId, gameData);

    /// <summary>对地图可见的敌方单位应用谍报遮蔽（ForceIntel 模式且非 Full 时调用）。</summary>
    public static StrategyUnitStateDto ApplyUnitMask(
        StrategyUnitStateDto unit,
        int playerForceId,
        GameData gameData,
        StrategyEspionageIntelLedger? ledger)
    {
        if (!RequiresEspionageMask(unit.ForceId, playerForceId, gameData))
            return unit;

        var record = ledger?.TryGet(EspionageIntelTargetKind.Unit, unit.Id);
        if (record is null)
        {
            // 业务：无谍报记录时军事/内政数值均隐藏
            return unit with
            {
                SoldiersDisplay = UnknownDisplay,
                MoraleBand = UnknownDisplay,
                TrainingBand = UnknownDisplay,
                Money = 0,
                Food = 0
            };
        }

        var hasMilitary = record.Scope is EspionageIntelScope.Military or EspionageIntelScope.Both;
        var hasDomestic = record.Scope is EspionageIntelScope.Domestic or EspionageIntelScope.Both;
        var fuzzy = record.Precision == EspionageIntelPrecision.Fuzzy;

        var masked = unit;

        if (!hasMilitary)
        {
            masked = masked with
            {
                SoldiersDisplay = UnknownDisplay,
                MoraleBand = UnknownDisplay,
                TrainingBand = UnknownDisplay
            };
        }
        else if (fuzzy)
        {
            // 业务：模糊谍报只给高/中/低档位，不写具体兵数
            masked = masked with
            {
                SoldiersDisplay = BandFromSoldiers(unit.Soldiers),
                MoraleBand = StrategyIntelMaskRules.MaskMoraleBand(unit.Morale),
                TrainingBand = StrategyIntelMaskRules.MaskTrainingBand(unit.Training)
            };
        }
        else
        {
            // 业务：精确军事谍报 — 清空 band 字段，前端读真实 Soldiers/Morale
            masked = masked with
            {
                SoldiersDisplay = null,
                MoraleBand = null,
                TrainingBand = null
            };
        }

        if (!hasDomestic)
            masked = masked with { Money = 0, Food = 0 };

        return masked;
    }

    /// <summary>对可见敌方据点应用谍报遮蔽（内政/军事分 scope 控制）。</summary>
    public static StrategyStrongholdStateDto ApplyStrongholdMask(
        StrategyStrongholdStateDto dto,
        int playerForceId,
        GameData gameData,
        StrategyEspionageIntelLedger? ledger)
    {
        if (!RequiresEspionageMask(dto.ForceId, playerForceId, gameData))
            return dto;

        var record = ledger?.TryGet(EspionageIntelTargetKind.Stronghold, dto.Id);
        if (record is null)
            return MaskStrongholdUnknown(dto);

        var hasMilitary = record.Scope is EspionageIntelScope.Military or EspionageIntelScope.Both;
        var hasDomestic = record.Scope is EspionageIntelScope.Domestic or EspionageIntelScope.Both;
        var fuzzy = record.Precision == EspionageIntelPrecision.Fuzzy;

        if (!hasMilitary && !hasDomestic)
            return MaskStrongholdUnknown(dto);

        if (fuzzy)
        {
            // 业务：模糊谍报 — 数值清零，仅通过 Espionage*Band 传档位
            return dto with
            {
                GarrisonSoldiers = 0,
                GarrisonWounded = 0,
                Morale = 0,
                Training = 0,
                Defense = 0,
                Population = 0,
                Food = 0,
                Money = 0,
                Stability = 0,
                PopularFeelings = 0,
                PollTaxRate = 0,
                AgricultureTaxRate = 0,
                CommerceTaxRate = 0,
                TariffTaxRate = 0,
                DefenseFacilities = [],
                EconomyFacilities = [],
                EspionageSoldiersBand = hasMilitary ? BandFromSoldiers(dto.GarrisonSoldiers) : UnknownDisplay,
                EspionageMoraleBand = hasMilitary ? StrategyIntelMaskRules.MaskMoraleBand(dto.Morale) : UnknownDisplay,
                EspionageTrainingBand = hasMilitary ? StrategyIntelMaskRules.MaskTrainingBand(dto.Training) : UnknownDisplay,
                EspionagePopulationBand = hasDomestic ? BandFromPopulation(dto.Population) : UnknownDisplay,
                EspionageFoodBand = hasDomestic ? BandFromFood(dto.Food) : UnknownDisplay,
                EspionageMoneyBand = hasDomestic ? BandFromMoney(dto.Money) : UnknownDisplay
            };
        }

        // 业务：精确谍报 — 按 scope 保留真实字段，未覆盖 scope 的 band 标「未知」
        return dto with
        {
            GarrisonSoldiers = hasMilitary ? dto.GarrisonSoldiers : 0,
            GarrisonWounded = hasMilitary ? dto.GarrisonWounded : 0,
            Morale = hasMilitary ? dto.Morale : 0,
            Training = hasMilitary ? dto.Training : 0,
            Defense = hasMilitary ? dto.Defense : 0,
            Population = hasDomestic ? dto.Population : 0,
            Food = hasDomestic ? dto.Food : 0,
            Money = hasDomestic ? dto.Money : 0,
            Stability = hasDomestic ? dto.Stability : 0,
            PopularFeelings = hasDomestic ? dto.PopularFeelings : 0,
            PollTaxRate = hasDomestic ? dto.PollTaxRate : (byte)0,
            AgricultureTaxRate = hasDomestic ? dto.AgricultureTaxRate : (byte)0,
            CommerceTaxRate = hasDomestic ? dto.CommerceTaxRate : (byte)0,
            TariffTaxRate = hasDomestic ? dto.TariffTaxRate : (byte)0,
            DefenseFacilities = hasMilitary ? dto.DefenseFacilities : [],
            EconomyFacilities = hasDomestic ? dto.EconomyFacilities : [],
            EspionageSoldiersBand = hasMilitary ? null : UnknownDisplay,
            EspionageMoraleBand = hasMilitary ? null : UnknownDisplay,
            EspionageTrainingBand = hasMilitary ? null : UnknownDisplay,
            EspionagePopulationBand = hasDomestic ? null : UnknownDisplay,
            EspionageFoodBand = hasDomestic ? null : UnknownDisplay,
            EspionageMoneyBand = hasDomestic ? null : UnknownDisplay
        };
    }

    private static StrategyStrongholdStateDto MaskStrongholdUnknown(StrategyStrongholdStateDto dto)
        => dto with
        {
            Food = 0,
            Population = 0,
            GarrisonSoldiers = 0,
            GarrisonWounded = 0,
            Money = 0,
            Morale = 0,
            Training = 0,
            Defense = 0,
            Stability = 0,
            PopularFeelings = 0,
            PollTaxRate = 0,
            AgricultureTaxRate = 0,
            CommerceTaxRate = 0,
            TariffTaxRate = 0,
            DefenseFacilities = [],
            EconomyFacilities = [],
            EspionageSoldiersBand = UnknownDisplay,
            EspionageMoraleBand = UnknownDisplay,
            EspionageTrainingBand = UnknownDisplay,
            EspionagePopulationBand = UnknownDisplay,
            EspionageFoodBand = UnknownDisplay,
            EspionageMoneyBand = UnknownDisplay
        };

    // 业务：模糊档位阈值（与据点规模/储粮习惯对齐）
    private static string BandFromSoldiers(int soldiers)
        => soldiers >= 5000 ? "高" : soldiers >= 1500 ? "中" : "低";

    private static string BandFromPopulation(int population)
        => population >= 50_000 ? "高" : population >= 30_000 ? "中" : "低";

    private static string BandFromFood(int food)
        => food >= 20_000 ? "高" : food >= 8000 ? "中" : "低";

    private static string BandFromMoney(int money)
        => money >= 5000 ? "高" : money >= 1500 ? "中" : "低";
}
