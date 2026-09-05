namespace SengokuScroll.Strategy.Data.Models;

/// <summary>策略剧本 JSON 根文档（M1-d 小地图格式）。</summary>
public sealed class StrategyScenarioDocument
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public required StrategyMapDefinition Map { get; init; }

    public required StrategyScenarioDefinition Scenario { get; init; }
}

/// <summary>地图静态配置。</summary>
public sealed class StrategyMapDefinition
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    /// <summary>未指定格时使用的地形键（对应 <see cref="Terrains"/> 中的 Key）。</summary>
    public required string DefaultTerrain { get; init; }

    public required List<StrategyTerrainDefinition> Terrains { get; init; }

    /// <summary>
    /// 逐行地形键网格（行优先，长度 = Width × Height）。
    /// 省略则整张地图填充 <see cref="DefaultTerrain"/>。
    /// </summary>
    public List<string>? TerrainGrid { get; init; }

    /// <summary>道路类型模板（可配置移动力增益）。</summary>
    public List<StrategyRoadTypeDefinition> RoadTypes { get; init; } = [];

    /// <summary>道路路径模板（便于编辑器复用）。</summary>
    public List<StrategyRoadTemplateDefinition> RoadTemplates { get; init; } = [];

    /// <summary>引用的道路模板 Id 列表，加载时写入 <see cref="GameMapData.Roads"/>。</summary>
    public List<string> PlacedRoads { get; init; } = [];

    /// <summary>地图区域定义（气候/收粮；格点归属见 <see cref="RegionGrid"/>）。</summary>
    public List<StrategyRegionDefinition> Regions { get; init; } = [];

    /// <summary>
    /// 地图区域键网格（行优先，长度 = Width × Height）。
    /// 省略则整张地图无区域名。
    /// </summary>
    public List<string>? RegionGrid { get; init; }

    /// <summary>地图地标（对应 Domain <see cref="Entities.Landmark"/>）。</summary>
    public List<StrategyMapLandmarkDefinition> Landmarks { get; init; } = [];
}

/// <summary>道路类型（对应 Domain <see cref="Definitions.RoadDefinition"/>）。</summary>
public sealed class StrategyRoadTypeDefinition
{
    public required int Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    /// <summary>相对地形的 AP 减免（越大越快）。</summary>
    public required int SpeedBonus { get; init; }

    /// <summary>可选：道路格固定移动力消耗（覆盖地形−增益）。</summary>
    public int? MovementCost { get; init; }
}

/// <summary>道路模板：一串格点共用同一道路类型。</summary>
public sealed class StrategyRoadTemplateDefinition
{
    public required string Id { get; init; }

    public required int TypeId { get; init; }

    public required List<StrategyMapPointDefinition> Points { get; init; }
}

/// <summary>地图格点（JSON）。</summary>
public sealed class StrategyMapPointDefinition
{
    public required int X { get; init; }

    public required int Y { get; init; }
}

/// <summary>地图区域定义（对应 Domain <see cref="Definitions.RegionDefinition"/>）。</summary>
public sealed class StrategyRegionDefinition
{
    public required int Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    /// <summary>作型：Single | Double | Triple；省略则 Single。</summary>
    public string? CropPattern { get; init; }

    /// <summary>收粮事件；省略则按 CropPattern 使用默认日历。</summary>
    public List<StrategyHarvestEventDefinition>? HarvestEvents { get; init; }
}

/// <summary>收粮事件 JSON。</summary>
public sealed class StrategyHarvestEventDefinition
{
    public required int Month { get; init; }

    public int Day { get; init; } = 1;

    public int ShareBasisPoints { get; init; } = 10_000;
}

/// <summary>地图地标（非 playable 据点）。</summary>
public sealed class StrategyMapLandmarkDefinition
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }
}

/// <summary>地形类型定义。</summary>
public sealed class StrategyTerrainDefinition
{
    public required int Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public required int MovementCost { get; init; }
}

/// <summary>剧本开局运行时实体配置。</summary>
public sealed class StrategyScenarioDefinition
{
    public required StrategyDateDefinition StartDate { get; init; }

    /// <summary>玩家操控势力 Id。</summary>
    public int PlayerForceId { get; init; } = 1;

    /// <summary>难度：Easy | Normal | Hard | Custom；省略则 Normal。</summary>
    public string? Difficulty { get; init; }

    /// <summary>开局已知位置的据点 Id 列表（非己方也可 gray 显示）。</summary>
    public List<int> KnownStrongholdIds { get; init; } = [];

    /// <summary>本局固定随机种子；0 或省略则开局按剧本 Id+起始日期派生。</summary>
    public int SimulationSeed { get; init; }

    /// <summary>当主配置；省略则默认玩家势力首个据点。</summary>
    public StrategyLordDefinition? Lord { get; init; }

    public required List<StrategyForceDefinition> Forces { get; init; }

    public required List<StrategyStrongholdDefinition> Strongholds { get; init; }

    public List<StrategyCharacterDefinition> Characters { get; init; } = [];

    public required List<StrategyUnitDefinition> Units { get; init; }

    /// <summary>开局显式外交关系（双向写入；省略则仅应用默认规则）。</summary>
    public List<StrategyDiplomacyDefinition> Diplomacies { get; init; } = [];

    /// <summary>剧本级规则选项（行政效率损耗等）。</summary>
    public StrategyScenarioGameOptions GameOptions { get; init; } = new();

    /// <summary>本局兵种主数据；省略或空则加载后由默认种子填充。</summary>
    public List<StrategyUnitTypeDefinition>? UnitTypes { get; init; }
}

/// <summary>剧本兵种类型（对应 Domain <see cref="Definitions.UnitTypeDefinition"/>）。</summary>
public sealed class StrategyUnitTypeDefinition
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public int Attack { get; init; }

    public int Defense { get; init; }

    public int AttackRange { get; init; }

    public int Movement { get; init; }

    public int SightRange { get; init; } = 2;

    public int? CultureId { get; init; }

    public int Cost { get; init; }

    public int MaintenanceMoney { get; init; }

    public int MaintenanceFood { get; init; }
}

/// <summary>剧本开局外交配置。</summary>
public sealed class StrategyDiplomacyDefinition
{
    public required int ForceId { get; init; }

    public required int TargetForceId { get; init; }

    /// <summary>Neutral | Allied | Enemy。</summary>
    public required string Relation { get; init; }
}

/// <summary>当主开局位置：领兵时在部队，否则在据点。</summary>
public sealed class StrategyLordDefinition
{
    public string Name { get; init; } = "当主";

    public int? UnitId { get; init; }

    public int? StrongholdId { get; init; }
}

/// <summary>游戏内日期（年月日）。</summary>
public sealed class StrategyDateDefinition
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public required int Day { get; init; }
}

/// <summary>势力开局配置。</summary>
public sealed class StrategyForceDefinition
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public int Food { get; init; }

    public int Money { get; init; }

    /// <summary>势力当主角色 Id（Character.Id）。</summary>
    public int LordCharacterId { get; init; }

    /// <summary>Independence | InnerVassal | OuterVassal；省略为 Independence。</summary>
    public string Status { get; init; } = "Independence";

    /// <summary>宗主势力 Id；内藩/外藩时有效。</summary>
    public int? SuzerainForceId { get; init; }

    /// <summary>为 true 时不参与开局默认互设敌对外交（与其他势力无外交条目）。</summary>
    public bool ExcludeFromDefaultDiplomacy { get; init; }
}

/// <summary>据点开局配置。</summary>
public sealed class StrategyStrongholdDefinition
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public int Food { get; init; }

    public int Population { get; init; } = 5000;

    /// <summary>领主角色 Id；0 = 当主直辖。</summary>
    public int LordId { get; init; }

    /// <summary>兼容旧剧本：按名匹配领主（LordId 未指定时）。</summary>
    public string? LordName { get; init; }

    public string? MayorName { get; init; }

    public int Morale { get; init; } = 80;

    public int Training { get; init; } = 65;

    public string CultureName { get; init; } = "日本";

    public string ReligionName { get; init; } = "神道教";

    public int Money { get; init; }

    /// <summary>城内驻军士兵数（非地图单位）。</summary>
    public int GarrisonSoldiers { get; init; } = 0;

    /// <summary>人头税率（%）。</summary>
    public byte PollTaxRate { get; init; } = 10;

    /// <summary>农业税率（%）。</summary>
    public byte AgricultureTaxRate { get; init; } = 25;

    /// <summary>商业税率（%）。</summary>
    public byte CommerceTaxRate { get; init; } = 12;

    /// <summary>关税率（%）。</summary>
    public byte TariffTaxRate { get; init; } = 8;

    /// <summary>治安（0–100）；省略时默认 50。</summary>
    public byte Stability { get; init; }

    /// <summary>民心（0–100）；省略时默认 50。</summary>
    public byte PopularFeelings { get; init; }

    /// <summary>城防设施类型 Id 列表；省略时按人口分配默认设施。</summary>
    public List<int> DefenseFacilityIds { get; init; } = [];

    /// <summary>经济设施类型 Id 列表；省略时按商业值推断（Market/奢侈品工坊）。</summary>
    public List<int> EconomyFacilityIds { get; init; } = [];

    /// <summary>城防（0–100）；已废弃，仅兼容旧剧本，实际城防由设施累加。</summary>
    public byte Defense { get; init; }

    /// <summary>是否史实据点；省略时按同格是否存在地图地标推断。</summary>
    public bool? IsHistorical { get; init; }
}

/// <summary>剧本角色（M3-b：指挥官/领主/代官等）。</summary>
public sealed class StrategyCharacterDefinition
{
    public StrategyPersonalityDefinition? Personality { get; init; }
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    /// <summary>驻留据点；省略则随绑定单位/据点推断。</summary>
    public int? StrongholdId { get; init; }

    public int? LeaderId { get; init; }

    public int? FatherId { get; init; }

    public int? MotherId { get; init; }

    public int? SpouseId { get; init; }

    public int? MasterId { get; init; }

    public List<int>? EnemyIds { get; init; }

    public string? Description { get; init; }

    public string? Portrait { get; init; }

    public string? Sex { get; init; }

    public int? BirthYear { get; init; }

    public int? BirthMonth { get; init; }

    public int? BirthDay { get; init; }

    public int? CultureId { get; init; }

    public int? ReligionId { get; init; }

    public int? Leadership { get; init; }

    public int? Power { get; init; }

    public int? Politics { get; init; }

    public int? Strategy { get; init; }

    public int? Charm { get; init; }
}

/// <summary>游戏性格设定，非历史评价。缺省维度保持 50；Action 越高越慎重。</summary>
public sealed class StrategyPersonalityDefinition
{
    public int Temper { get; init; } = 50;
    public int Courage { get; init; } = 50;
    public int Principle { get; init; } = 50;
    public int Action { get; init; } = 50;
    public int Friendship { get; init; } = 50;
    public int Ambition { get; init; } = 50;
    public int Hobby { get; init; } = 50;
    public int Desire { get; init; } = 50;
    public int Drinking { get; init; } = 50;
    public int Fortune { get; init; } = 50;
}

/// <summary>军事单位开局配置。</summary>
public sealed class StrategyUnitDefinition
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public int Soldiers { get; init; } = 100;

    public int Food { get; init; } = 1000;

    public int Movement { get; init; } = 10;

    /// <summary>总将角色 Id；出征编组时确定，省略则按 <see cref="CommanderName"/> 匹配或自动创建。</summary>
    public int? CommanderId { get; init; }

    public string? CommanderName { get; init; }

    public int Morale { get; init; } = 75;

    public int Training { get; init; } = 70;

    public string CultureName { get; init; } = "日本";

    public string ReligionName { get; init; } = "神道教";

    public int Money { get; init; }

    /// <summary>
    /// 出征编组时的兵种/备队构成；省略则整支单位视为单编制（M3 兼容）。
    /// 各段可选队将见 <see cref="StrategySubUnitCompositionDefinition.CommanderId"/>。
    /// </summary>
    public List<StrategySubUnitCompositionDefinition> Composition { get; init; } = [];

    /** 开局方针（UnitDirective 枚举名）；驻军建议 Support。 */
    public string? Directive { get; init; }
}

/// <summary>单位内子编制（兵种/备队）开局配置。</summary>
public sealed class StrategySubUnitCompositionDefinition
{
    /// <summary>子编制 Id；省略则自动分配。</summary>
    public int? Id { get; init; }

    /// <summary>兵种类型 Id（1 足轻、2 弓兵、3 骑兵、4 铁炮）。</summary>
    public required int TypeId { get; init; }

    public string? TypeName { get; init; }

    public required int Soldiers { get; init; }

    /// <summary>队将角色 Id；省略则归总将统辖。</summary>
    public int? CommanderId { get; init; }

    public string? CommanderName { get; init; }
}
