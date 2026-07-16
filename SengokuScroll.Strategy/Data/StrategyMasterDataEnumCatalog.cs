using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Enums;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Definitions.CharacterDefinition;

namespace SengokuScroll.Strategy.Data;

/// <summary>业务相关枚举 Master Data 快照。</summary>
internal static class StrategyMasterDataEnumCatalog
{
    public static IReadOnlyList<StrategyMasterDataEntryDto> BuildEntries()
    {
        var entries = new List<StrategyMasterDataEntryDto>();
        var id = 1;

        void AddEnum<TEnum>(string category, Func<TEnum, string> labelFn) where TEnum : struct, Enum
        {
            foreach (TEnum value in Enum.GetValues<TEnum>())
            {
                var name = value.ToString();
                entries.Add(new StrategyMasterDataEntryDto
                {
                    Id = id++,
                    Name = labelFn(value),
                    Fields = new Dictionary<string, string>
                    {
                        ["category"] = category,
                        ["member"] = name,
                        ["value"] = Convert.ToInt32(value).ToString()
                    }
                });
            }
        }

        AddEnum<TerrainType>("TerrainType", TerrainLabel);
        AddEnum<Level3>("Level3", Level3Label);
        AddEnum<Level5>("Level5", Level5Label);
        AddEnum<TerrainFeatureType>("TerrainFeatureType", TerrainFeatureLabel);
        AddEnum<SexType>("CharacterSexType", SexLabel);
        AddEnum<CharacterType>("CharacterType", CharacterTypeLabel);
        AddEnum<BirtyType>("CharacterBirthType", BirthTypeLabel);
        AddEnum<StrongholdType.CategoryType>("StrongholdCategoryType", StrongholdCategoryLabel);
        AddEnum<DefenseFacilityTypeModel.DefenseFacilityCategory>(
            "DefenseFacilityCategory",
            DefenseFacilityCategoryLabel);

        return entries;
    }

    private static string TerrainLabel(TerrainType type)
        => type switch
        {
            TerrainType.Plain => "平地",
            TerrainType.Hill => "丘陵",
            TerrainType.Mountain => "山地",
            TerrainType.MountainRange => "山脉",
            TerrainType.Badlands => "荒地",
            TerrainType.Desert => "沙漠",
            TerrainType.Permafrost => "冰原",
            TerrainType.River => "河流",
            TerrainType.Lake => "湖泊",
            TerrainType.ShallowSea => "浅海",
            TerrainType.DeepSea => "深海",
            _ => type.ToString()
        };

    private static string Level3Label(Level3 level)
        => level switch
        {
            Level3.Low => "低",
            Level3.Medium => "中",
            Level3.High => "高",
            _ => level.ToString()
        };

    private static string Level5Label(Level5 level)
        => level switch
        {
            Level5.Lowest => "最低",
            Level5.Lower => "较低",
            Level5.Medium => "中",
            Level5.Higher => "较高",
            Level5.Highest => "最高",
            _ => level.ToString()
        };

    private static string TerrainFeatureLabel(TerrainFeatureType type)
        => type switch
        {
            TerrainFeatureType.Grass => "草地",
            TerrainFeatureType.Forest => "树林",
            _ => type.ToString()
        };

    private static string SexLabel(SexType type)
        => type switch
        {
            SexType.Male => "男",
            SexType.Female => "女",
            _ => type.ToString()
        };

    private static string CharacterTypeLabel(CharacterType type)
        => type switch
        {
            CharacterType.AI => "AI",
            CharacterType.Player => "玩家",
            _ => type.ToString()
        };

    private static string BirthTypeLabel(BirtyType type)
        => type switch
        {
            BirtyType.Slave => "奴隶",
            BirtyType.Normal => "平民",
            BirtyType.Landlord => "豪强士族",
            BirtyType.Noble => "贵族",
            BirtyType.RoyalFamily => "皇族",
            _ => type.ToString()
        };

    private static string StrongholdCategoryLabel(StrongholdType.CategoryType type)
        => type switch
        {
            StrongholdType.CategoryType.Plain => "平城",
            StrongholdType.CategoryType.Hill => "平山城",
            StrongholdType.CategoryType.Mountain => "山城",
            _ => type.ToString()
        };

    private static string DefenseFacilityCategoryLabel(
        DefenseFacilityTypeModel.DefenseFacilityCategory type)
        => type switch
        {
            DefenseFacilityTypeModel.DefenseFacilityCategory.Castle => "城堡",
            DefenseFacilityTypeModel.DefenseFacilityCategory.Wall => "城墙",
            DefenseFacilityTypeModel.DefenseFacilityCategory.Gate => "城门",
            DefenseFacilityTypeModel.DefenseFacilityCategory.Moat => "护城河",
            DefenseFacilityTypeModel.DefenseFacilityCategory.Defender => "防御设施",
            _ => type.ToString()
        };
}
