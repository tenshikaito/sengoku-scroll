using SengokuScroll.Domain;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Enums;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Data;

/// <summary>剧本加载时为 Master Data 填充默认可浏览快照。</summary>
internal static class StrategyDefaultMasterDataSeed
{
    public const int JapaneseCultureGroupId = 1;
    public const int ChineseCultureGroupId = 2;
    public const int WesternCultureGroupId = 3;

    public const int JapaneseCultureId = 1;
    public const int HanCultureId = 2;
    public const int KoreanCultureId = 3;
    public const int EnglishCultureId = 4;
    public const int DutchCultureId = 5;
    public const int SpanishCultureId = 6;
    public const int PortugueseCultureId = 7;

    public const int ShintoReligionGroupId = 1;
    public const int ChristianityReligionGroupId = 2;
    public const int BuddhismReligionGroupId = 3;
    public const int AtheismReligionGroupId = 4;
    public const int AnimismReligionGroupId = 5;

    public const int ShintoReligionId = 1;
    public const int HokkeReligionId = 2;
    public const int NichirenReligionId = 3;
    public const int JodoReligionId = 4;
    public const int IkkoReligionId = 5;
    public const int CatholicReligionId = 6;
    public const int AnimismReligionId = 7;
    public const int AtheismReligionId = 8;

    public static void Apply(GameWorld world)
    {
        var master = world.GameMasterData;
        if (master.CultureGroups.Count == 0)
            master.CultureGroups = CreateCultureGroups();

        if (master.Cultures.Count == 0)
            master.Cultures = CreateCultures();

        if (master.ReligionGroups.Count == 0)
            master.ReligionGroups = CreateReligionGroups();

        if (master.Religions.Count == 0)
            master.Religions = CreateReligions();

        if (master.StrongholdTypes.Count == 0)
            master.StrongholdTypes = CreateStrongholdTypes();

        if (master.UnitTypes.Count == 0)
            master.UnitTypes = CreateUnitTypes();

        if (master.DefenseFacilityTypes.Count == 0)
            master.DefenseFacilityTypes = StrongholdDefenseRules.CreateDefaultDefenseFacilityTypes();

        SeedMapMasterData(world);
    }

    private static void SeedMapMasterData(GameWorld world)
    {
        var mapMaster = world.GameMapMasterData;
        EnsureEnumTerrains(mapMaster);

        if (mapMaster.Climates.Count == 0)
            mapMaster.Climates = CreateClimates();

        if (mapMaster.TerrainVegatationFeatures.Count == 0)
            mapMaster.TerrainVegatationFeatures = CreateVegetationFeatures();

        if (mapMaster.TerrainSurfaceFeatures.Count == 0)
            mapMaster.TerrainSurfaceFeatures = CreateSurfaceFeatures();
    }

    private static void EnsureEnumTerrains(GameMapMasterData mapMaster)
    {
        foreach (TerrainType type in Enum.GetValues<TerrainType>())
        {
            var id = (int)type + 1;
            if (mapMaster.Terrains.ContainsKey(id))
                continue;

            mapMaster.Terrains[id] = new TerrainDefinition
            {
                Type = type,
                Name = TerrainDisplayName(type),
                Description = TerrainDisplayName(type),
                Altitude = type is TerrainType.Mountain or TerrainType.MountainRange ? 200 : 0,
                MovementCost = DefaultMovementCost(type)
            };
        }
    }

    private static int DefaultMovementCost(TerrainType type)
        => type switch
        {
            TerrainType.Plain => 2,
            TerrainType.Hill => 3,
            TerrainType.Mountain => 5,
            TerrainType.MountainRange => 99,
            TerrainType.Badlands => 4,
            TerrainType.Desert => 5,
            TerrainType.Permafrost => 6,
            TerrainType.River => 3,
            TerrainType.Lake => 99,
            TerrainType.ShallowSea => 4,
            TerrainType.DeepSea => 99,
            _ => 3
        };

    private static string TerrainDisplayName(TerrainType type)
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

    private static Dictionary<int, CultureGroupDefinition> CreateCultureGroups()
        => new()
        {
            [JapaneseCultureGroupId] = new()
            {
                Id = JapaneseCultureGroupId,
                Name = "日本",
                Description = "日本列岛诸文化所属的文化组。"
            },
            [ChineseCultureGroupId] = new()
            {
                Id = ChineseCultureGroupId,
                Name = "中华",
                Description = "东亚大陆诸文化所属的文化组。"
            },
            [WesternCultureGroupId] = new()
            {
                Id = WesternCultureGroupId,
                Name = "西欧",
                Description = "西欧诸文化所属的文化组。"
            }
        };

    private static Dictionary<int, CultureDefinition> CreateCultures()
        => new()
        {
            [JapaneseCultureId] = new()
            {
                Id = JapaneseCultureId,
                Name = "日本",
                Description = "日本列岛传统文化。",
                CultureGroupId = JapaneseCultureGroupId
            },
            [HanCultureId] = new()
            {
                Id = HanCultureId,
                Name = "汉",
                Description = "汉族传统文化。",
                CultureGroupId = ChineseCultureGroupId
            },
            [KoreanCultureId] = new()
            {
                Id = KoreanCultureId,
                Name = "朝鲜",
                Description = "朝鲜半岛传统文化。",
                CultureGroupId = ChineseCultureGroupId
            },
            [EnglishCultureId] = new()
            {
                Id = EnglishCultureId,
                Name = "英格兰",
                Description = "英格兰传统文化。",
                CultureGroupId = WesternCultureGroupId
            },
            [DutchCultureId] = new()
            {
                Id = DutchCultureId,
                Name = "荷兰",
                Description = "尼德兰传统文化。",
                CultureGroupId = WesternCultureGroupId
            },
            [SpanishCultureId] = new()
            {
                Id = SpanishCultureId,
                Name = "西班牙",
                Description = "伊比利亚传统文化。",
                CultureGroupId = WesternCultureGroupId
            },
            [PortugueseCultureId] = new()
            {
                Id = PortugueseCultureId,
                Name = "葡萄牙",
                Description = "葡萄牙传统文化。",
                CultureGroupId = WesternCultureGroupId
            }
        };

    private static Dictionary<int, ReligionGroupDefinition> CreateReligionGroups()
        => new()
        {
            [ShintoReligionGroupId] = new()
            {
                Id = ShintoReligionGroupId,
                Name = "神道教",
                Description = "日本本土神道信仰传统。",
                Level = Level3.Medium
            },
            [ChristianityReligionGroupId] = new()
            {
                Id = ChristianityReligionGroupId,
                Name = "基督教",
                Description = "一神教传统，含天主教等派别。",
                Level = Level3.High
            },
            [BuddhismReligionGroupId] = new()
            {
                Id = BuddhismReligionGroupId,
                Name = "佛教",
                Description = "在大东亚广泛传播的佛教诸宗。",
                Level = Level3.High
            },
            [AtheismReligionGroupId] = new()
            {
                Id = AtheismReligionGroupId,
                Name = "无神论",
                Description = "否定超自然信仰的思想传统。",
                Level = Level3.Low
            },
            [AnimismReligionGroupId] = new()
            {
                Id = AnimismReligionGroupId,
                Name = "泛灵教",
                Description = "万物有灵的原始信仰传统。",
                Level = Level3.Low
            }
        };

    private static Dictionary<int, ReligionDefinition> CreateReligions()
        => new()
        {
            [ShintoReligionId] = CreateReligion(
                ShintoReligionId, "神道教", "日本本土多神信仰。", ShintoReligionGroupId, "神道教", 35),
            [HokkeReligionId] = CreateReligion(
                HokkeReligionId, "法华宗", "以《法华经》为中心的佛教宗派。", BuddhismReligionGroupId, "佛教", 25),
            [NichirenReligionId] = CreateReligion(
                NichirenReligionId, "日莲宗", "日莲宗佛教传统。", BuddhismReligionGroupId, "佛教", 30),
            [JodoReligionId] = CreateReligion(
                JodoReligionId, "净土宗", "念佛往生之净土信仰。", BuddhismReligionGroupId, "佛教", 20),
            [IkkoReligionId] = CreateReligion(
                IkkoReligionId, "一向宗", "一向宗门众之信仰。", BuddhismReligionGroupId, "佛教", 45),
            [CatholicReligionId] = CreateReligion(
                CatholicReligionId, "天主教", "罗马公教。", ChristianityReligionGroupId, "基督教", 70),
            [AnimismReligionId] = CreateReligion(
                AnimismReligionId, "泛灵教", "万物有灵之原始信仰。", AnimismReligionGroupId, "泛灵教", 15),
            [AtheismReligionId] = CreateReligion(
                AtheismReligionId, "无神论", "否定超自然存在。", AtheismReligionGroupId, "无神论", 10)
        };

    private static ReligionDefinition CreateReligion(
        int id,
        string name,
        string description,
        int groupId,
        string groupName,
        byte exclusivism)
        => new()
        {
            Id = id,
            Name = name,
            Description = description,
            ReligionGroupId = groupId,
            ReligionGroupName = groupName,
            Level = Level3.Medium,
            DoctrinalDifference = Level3.Medium,
            Centralization = Level3.Medium,
            Exclusivism = exclusivism
        };

    private static Dictionary<int, StrongholdType> CreateStrongholdTypes()
        => new()
        {
            [1] = new()
            {
                Id = 1,
                Name = "平城",
                Description = "建于平地的城郭。",
                Category = StrongholdType.CategoryType.Plain,
                CultureId = JapaneseCultureId
            },
            [2] = new()
            {
                Id = 2,
                Name = "平山城",
                Description = "建于丘陵的城郭。",
                Category = StrongholdType.CategoryType.Hill,
                CultureId = JapaneseCultureId
            },
            [3] = new()
            {
                Id = 3,
                Name = "山城",
                Description = "建于山地的城郭。",
                Category = StrongholdType.CategoryType.Mountain,
                CultureId = JapaneseCultureId
            }
        };

    private static Dictionary<int, UnitTypeDefinition> CreateUnitTypes()
        => new()
        {
            [StrategyTroopTypes.Ashigaru] = new()
            {
                Id = StrategyTroopTypes.Ashigaru,
                Name = "足轻",
                Description = "基础步兵。",
                Attack = 10,
                Defense = 8,
                Movement = 4,
                SightRange = StrategyTroopSightRanges.Resolve(StrategyTroopTypes.Ashigaru),
                CultureId = JapaneseCultureId
            },
            [StrategyTroopTypes.Archer] = new()
            {
                Id = StrategyTroopTypes.Archer,
                Name = "弓兵",
                Description = "远程弓射部队。",
                Attack = 9,
                Defense = 6,
                AttackRange = 2,
                Movement = 4,
                SightRange = StrategyTroopSightRanges.Resolve(StrategyTroopTypes.Archer),
                CultureId = JapaneseCultureId
            },
            [StrategyTroopTypes.Cavalry] = new()
            {
                Id = StrategyTroopTypes.Cavalry,
                Name = "骑兵",
                Description = "骑马突击部队。",
                Attack = 12,
                Defense = 7,
                Movement = 6,
                SightRange = StrategyTroopSightRanges.Resolve(StrategyTroopTypes.Cavalry),
                CultureId = JapaneseCultureId
            },
            [StrategyTroopTypes.Matchlock] = new()
            {
                Id = StrategyTroopTypes.Matchlock,
                Name = "铁炮",
                Description = "火绳枪部队。",
                Attack = 11,
                Defense = 5,
                SightRange = StrategyTroopSightRanges.Resolve(StrategyTroopTypes.Matchlock),
                AttackRange = 2,
                Movement = 3,
                CultureId = JapaneseCultureId
            }
        };

    private static Dictionary<int, ClimateDefinition> CreateClimates()
        => new()
        {
            [1] = new()
            {
                Id = 1,
                Name = "温带季风",
                Description = "四季分明，雨热同期。",
                SpringClimate = new ClimateFactor { BaseTemperature = Level5.Medium, BaseWet = Level5.Medium },
                SummerClimate = new ClimateFactor { BaseTemperature = Level5.Higher, BaseWet = Level5.Higher },
                AutumnClimate = new ClimateFactor { BaseTemperature = Level5.Medium, BaseWet = Level5.Medium },
                WinterClimate = new ClimateFactor { BaseTemperature = Level5.Lower, BaseWet = Level5.Lower }
            },
            [2] = new()
            {
                Id = 2,
                Name = "亚热带湿润",
                Description = "暖湿，冬季偏温和。",
                SpringClimate = new ClimateFactor { BaseTemperature = Level5.Higher, BaseWet = Level5.Higher },
                SummerClimate = new ClimateFactor { BaseTemperature = Level5.Higher, BaseWet = Level5.Higher },
                AutumnClimate = new ClimateFactor { BaseTemperature = Level5.Medium, BaseWet = Level5.Medium },
                WinterClimate = new ClimateFactor { BaseTemperature = Level5.Medium, BaseWet = Level5.Medium }
            },
            [3] = new()
            {
                Id = 3,
                Name = "寒带",
                Description = "冬季漫长且寒冷。",
                SpringClimate = new ClimateFactor { BaseTemperature = Level5.Lower, BaseWet = Level5.Lower },
                SummerClimate = new ClimateFactor { BaseTemperature = Level5.Medium, BaseWet = Level5.Medium },
                AutumnClimate = new ClimateFactor { BaseTemperature = Level5.Lower, BaseWet = Level5.Lower },
                WinterClimate = new ClimateFactor { BaseTemperature = Level5.Lower, BaseWet = Level5.Lower }
            }
        };

    private static Dictionary<int, TerrainVegetationFeatureDefinition> CreateVegetationFeatures()
        => new()
        {
            [1] = new()
            {
                Type = TerrainFeatureType.Grass,
                Name = "草地",
                Description = "地表生命覆盖以草本为主，利于牧畜。"
            },
            [2] = new()
            {
                Type = TerrainFeatureType.Forest,
                Name = "树林",
                Description = "木本植被覆盖，阻碍骑兵与器械。"
            }
        };

    private static Dictionary<int, TerrainSurfaceFeatureDefinition> CreateSurfaceFeatures()
        => new()
        {
            [1] = new()
            {
                Type = TerrainFeatureType.Grass,
                Name = "裸地",
                Description = "缺乏植被覆盖的裸露地表。"
            },
            [2] = new()
            {
                Type = TerrainFeatureType.Forest,
                Name = "石地",
                Description = "岩石裸露或碎石覆盖的地表。"
            }
        };
}
