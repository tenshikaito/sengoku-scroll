using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Types;
using SengokuScroll.Domain.World;
using SengokuScroll.Strategy.Vision;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Models;

/// <summary>策略世界状态 API 响应（M2-a：供前端地图渲染）。</summary>
public sealed record StrategyWorldStateDto
{
    public required string ScenarioId { get; init; }

    public required StrategyMapStateDto Map { get; init; }

    public required StrategyDateStateDto Date { get; init; }

    public required IReadOnlyList<StrategyForceStateDto> Forces { get; init; }

    public required IReadOnlyList<StrategyStrongholdStateDto> Strongholds { get; init; }

    public required IReadOnlyList<StrategyUnitStateDto> Units { get; init; }

    /// <summary>进行中的地图战场（交战格折叠显示）。</summary>
    public required IReadOnlyList<StrategyBattlefieldStateDto> Battlefields { get; init; }

    /// <summary>迷雾外己方部队摘要（无坐标，供侧栏列表）。</summary>
    public required IReadOnlyList<StrategyUnitRosterEntryDto> OwnUnitRoster { get; init; }

    public required IReadOnlyList<StrategySupplyConvoyStateDto> SupplyConvoys { get; init; }

    public required IReadOnlyList<StrategyMessengerStateDto> Messengers { get; init; }

    /// <summary>将领摘要（统计本势力将领数等）。</summary>
    public required IReadOnlyList<StrategyCharacterSummaryDto> Characters { get; init; }

    /// <summary>在地图上独立行动的将领（溃逃回城等）。</summary>
    public required IReadOnlyList<StrategyMapCharacterStateDto> MapCharacters { get; init; }

    /// <summary>谍报获得的情报条目（非自势力）。</summary>
    public required IReadOnlyList<StrategyEspionageIntelEntryDto> EspionageIntel { get; init; }

    /// <summary>玩家势力视角外交（目标势力 + 关系）。</summary>
    public required IReadOnlyList<StrategyDiplomacyStateDto> Diplomacies { get; init; }

    /// <summary>玩家势力 Id。</summary>
    public required int PlayerForceId { get; init; }

    /// <summary>本局难度（Easy/Normal/...）。</summary>
    public required string Difficulty { get; init; }

    /// <summary>本局固定随机种子（回放/联机预埋）。</summary>
    public required int SimulationSeed { get; init; }

    /// <summary>当主摘要（方针/战报信使出发点）。</summary>
    public required StrategyLordStateDto Lord { get; init; }

    /// <summary>剧本 Master Data 快照（情报系统查阅用）。</summary>
    public required StrategyMasterDataSnapshotDto MasterData { get; init; }

    /// <summary>玩家势力战争迷雾快照（explored / visible / known）。</summary>
    public StrategyVisibilityDto? Visibility { get; init; }

    /// <summary>本局生效的开局选项（难度预设或 Custom 快照）。</summary>
    public GameStartOptionsDto? StartOptions { get; init; }
}

/// <summary>地图尺寸与名称（静态格点数据见 <see cref="StrategyMapMasterDto"/>）。</summary>
public sealed record StrategyMapStateDto
{
    public required string Name { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }
}

/// <summary>地图地标 DTO。</summary>
public sealed record StrategyMapLandmarkDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }
}

/// <summary>地图道路格。</summary>
public sealed record StrategyRoadCellDto
{
    public required int X { get; init; }

    public required int Y { get; init; }

    public required int TypeId { get; init; }

    public required string TypeName { get; init; }

    /// <summary>道路等级 Id（与 typeId 一致）。</summary>
    public required int Level { get; init; }

    /// <summary>移动力加成（AP 减免）。</summary>
    public required int SpeedBonus { get; init; }

    /// <summary>该道路格移动力消耗（AP）。</summary>
    public required int MovementCost { get; init; }
}

/// <summary>当前游戏内日期。</summary>
public sealed record StrategyDateStateDto
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public required int Day { get; init; }
}

/// <summary>势力摘要。</summary>
public sealed record StrategyForceStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int Food { get; init; }

    public required int Money { get; init; }

    /// <summary>Independence | InnerVassal | OuterVassal。</summary>
    public required string Status { get; init; }

    /// <summary>宗主势力 Id；内藩/外藩时有效。</summary>
    public int? SuzerainForceId { get; init; }

    /// <summary>本势力据点数（含旗下内藩）。</summary>
    public required int StrongholdCount { get; init; }

    /// <summary>本势力将领数（含旗下内藩）。</summary>
    public required int CharacterCount { get; init; }

    /// <summary>威望（Domain <see cref="Entities.Force.Prestige"/>）。</summary>
    public required byte Prestige { get; init; }

    /// <summary>正统性（Domain <see cref="Entities.Force.Orthodoxy"/>）。</summary>
    public required byte Orthodoxy { get; init; }

    /// <summary>当主角色驻留据点 Id（<see cref="Character.StrongholdId"/>；无则 0）。</summary>
    public required int LordResidenceStrongholdId { get; init; }

    /// <summary>势力内贡赋欠粮（合；M4-d）。</summary>
    public required int InternalArrearsFoodGo { get; init; }

    /// <summary>势力内贡赋欠钱（文；M4-d）。</summary>
    public required int InternalArrearsMoney { get; init; }

    /// <summary>继承人角色 Id；无则 null。</summary>
    public int? SuccessorId { get; init; }
}

/// <summary>将领摘要（供前端统计与出征编组）。</summary>
public sealed record StrategyCharacterSummaryDto
{
    public required int Id { get; init; }

    public required int ForceId { get; init; }

    public required string Name { get; init; }

    public required int StrongholdId { get; init; }

    /// <summary>直属上司角色 Id；0 表示无。</summary>
    public required int LeaderId { get; init; }

    /// <summary>Map | Stronghold | Unit</summary>
    public required string LocationType { get; init; }

    /// <summary>Idle | UnitAction | Task | Prisoner 等</summary>
    public required string ForceStatus { get; init; }

    public required int Leadership { get; init; }

    public required int Power { get; init; }

    public required int Politics { get; init; }

    public required int Strategy { get; init; }

    public required int Charm { get; init; }

    public required string CultureName { get; init; }

    public required string ReligionName { get; init; }

    /// <summary>仕官年数（在本家势力任职年数；开局为 0）。</summary>
    public required int YearsInForce { get; init; }

    /// <summary>Male | Female</summary>
    public required string Sex { get; init; }

    public required int Age { get; init; }

    public required StrategyCharacterPersonalityDto Personality { get; init; }

    public required StrategyCharacterProficiencyDto Proficiency { get; init; }

    public required bool IsDead { get; init; }

    /// <summary>是否生病。</summary>
    public required bool IsSick { get; init; }

    /// <summary>出身：RoyalFamily | Noble | Landlord | Normal | Slave。</summary>
    public required string BirthType { get; init; }

    /// <summary>任务剩余天数（任务中时有效；未实装时为 null）。</summary>
    public int? TaskRemainingDays { get; init; }

    /// <summary>忠诚度 0–100（暂以情义属性映射）。</summary>
    public required int Loyalty { get; init; }
}

/// <summary>地图上独立行动的将领（溃逃、回城途中等）。</summary>
public sealed record StrategyMapCharacterStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required bool MapVisible { get; init; }
}

/// <summary>谍报情报摘要（前端判断展示精度与过期）。</summary>
public sealed record StrategyEspionageIntelEntryDto
{
    public required string TargetKind { get; init; }

    public required int TargetId { get; init; }

    public required string Scope { get; init; }

    public required string Precision { get; init; }

    public required int ExpiresYear { get; init; }

    public required int ExpiresMonth { get; init; }

    public required int ExpiresDay { get; init; }
}

public sealed record StrategyMasterDataEntryDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? Group { get; init; }

    public string? Description { get; init; }

    public string? Extra { get; init; }

    /// <summary>按字段展开的明细（情报 Master Data 表格列）。</summary>
    public IReadOnlyDictionary<string, string>? Fields { get; init; }
}

public sealed record StrategyMasterDataSnapshotDto
{
    public required IReadOnlyList<StrategyMasterDataEntryDto> CultureGroups { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> Cultures { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> ReligionGroups { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> Religions { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> Weathers { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> StrongholdTypes { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> DefenseFacilityTypes { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> UnitTypes { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> CharacterDefinitions { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> Terrains { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> Climates { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> Regions { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> Roads { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> Landmarks { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> TerrainVegetationFeatures { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> TerrainSurfaceFeatures { get; init; }

    public required IReadOnlyList<StrategyMasterDataEntryDto> Enums { get; init; }
}

public sealed record StrategyCharacterPersonalityDto
{
    public int Temper { get; init; }

    public int Courage { get; init; }

    public int Principle { get; init; }

    public int Action { get; init; }

    public int Friendship { get; init; }

    public int Ambition { get; init; }

    public int Hobby { get; init; }

    public int Desire { get; init; }

    public int Drinking { get; init; }

    public int Fortune { get; init; }
}

public sealed record StrategyCharacterProficiencyDto
{
    public int Infantry { get; init; }

    public int Ride { get; init; }

    public int Archery { get; init; }

    public int Firelock { get; init; }

    public int Sealing { get; init; }

    public int Military { get; init; }

    public int Fighting { get; init; }

    public int Spy { get; init; }

    public int Agriculture { get; init; }

    public int Commerce { get; init; }

    public int Construct { get; init; }

    public int Smelt { get; init; }

    public int Eloquence { get; init; }

    public int Court { get; init; }

    public int Sociality { get; init; }

    public int Healing { get; init; }
}

/// <summary>玩家视角外交摘要。</summary>
public sealed record StrategyDiplomacyStateDto
{
    public required int TargetForceId { get; init; }

    /// <summary>Neutral | Allied | Enemy。</summary>
    public required string Relation { get; init; }

    /// <summary>外交关系值（-100 恶劣 ~ 100 亲密）。</summary>
    public required int Relationship { get; init; }

    /// <summary>信赖度（-100 不信任 ~ 100 完全信任）。</summary>
    public required int Trust { get; init; }

    /// <summary>贡赋欠粮（合）。</summary>
    public required int ArrearsFoodGo { get; init; }

    /// <summary>贡赋欠钱（文）。</summary>
    public required int ArrearsMoney { get; init; }
}

/// <summary>据点摘要。</summary>
public sealed record StrategyStrongholdStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    /// <summary>据点类型 Id（平城/平山城/山城）。</summary>
    public required byte TypeId { get; init; }

    /// <summary>据点类型显示名。</summary>
    public required string TypeName { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int Food { get; init; }

    public required int Population { get; init; }

    /// <summary>治安（0–100）。</summary>
    public required int Stability { get; init; }

    /// <summary>民心（0–100，取自民间 Actor）。</summary>
    public required int PopularFeelings { get; init; }

    /// <summary>是否显示「居城」（当主居城或任命领主据点）。</summary>
    public required bool IsLordResidence { get; init; }

    /// <summary>领主角色 Id；0 = 当主直辖。</summary>
    public required int LordId { get; init; }

    /// <summary>是否当主直辖（LordId=0）。</summary>
    public required bool IsDirectRule { get; init; }

    /// <summary>领主显示名；直辖时为势力当主名。</summary>
    public required string LordName { get; init; }

    public string? MayorName { get; init; }

    public required int Morale { get; init; }

    public required int Training { get; init; }

    public required string CultureName { get; init; }

    public required string ReligionName { get; init; }

    public required int Money { get; init; }

    /// <summary>城内驻军士兵数（非地图单位）。</summary>
    public required int GarrisonSoldiers { get; init; }

    /// <summary>城内伤兵数。</summary>
    public required int GarrisonWounded { get; init; }

    public required byte PollTaxRate { get; init; }

    public required byte AgricultureTaxRate { get; init; }

    public required byte CommerceTaxRate { get; init; }

    public required byte TariffTaxRate { get; init; }

    /// <summary>是否史实据点（false = 虚构据点）。</summary>
    public required bool IsHistorical { get; init; }

    /// <summary>城防（城防设施防御值累加）。</summary>
    public required int Defense { get; init; }

    /// <summary>已建城防设施。</summary>
    public required IReadOnlyList<StrategyDefenseFacilityStateDto> DefenseFacilities { get; init; }

    /// <summary>官府奢侈品库存（M4-d）。</summary>
    public required int LuxuryGoods { get; init; }

    /// <summary>经济设施（Market/奢侈品工坊等）。</summary>
    public required IReadOnlyList<StrategyEconomyFacilityStateDto> EconomyFacilities { get; init; }

    /// <summary>当前被进攻状态：Assault=强攻，Encircle=围城；无则 null。</summary>
    public string? SiegeThreat { get; init; }

    /// <summary>迷雾层：Visible | Known | Hidden。</summary>
    public string? VisibilityTier { get; init; }

    /// <summary>谍报军事：兵力档位（未知/高/中/低）；精确谍报时为 null，读 GarrisonSoldiers。</summary>
    public string? EspionageSoldiersBand { get; init; }

    public string? EspionageMoraleBand { get; init; }

    public string? EspionageTrainingBand { get; init; }

    public string? EspionagePopulationBand { get; init; }

    public string? EspionageFoodBand { get; init; }

    public string? EspionageMoneyBand { get; init; }
}

/// <summary>经济设施摘要（M4-d）。</summary>
public sealed record StrategyEconomyFacilityStateDto
{
    public required int TypeId { get; init; }

    public required string Name { get; init; }
}

/// <summary>城防设施摘要。</summary>
public sealed record StrategyDefenseFacilityStateDto
{
    public required int TypeId { get; init; }

    public required string Name { get; init; }

    /// <summary>Castle | Wall | Gate | Moat | Defender</summary>
    public required string Category { get; init; }

    /// <summary>设施等级（1–3）。</summary>
    public required int Level { get; init; }

    /// <summary>该设施城防加成。</summary>
    public required int Defense { get; init; }
}

/// <summary>军事单位摘要。</summary>
public sealed record StrategyUnitStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int Soldiers { get; init; }

    public required int Food { get; init; }

    public required int Ap { get; init; }

    public required int Movement { get; init; }

    public required string Status { get; init; }

    /// <summary>当前战斗/行动方针（UnitDirective 枚举名）。</summary>
    public required string Directive { get; init; }

    /// <summary>剩余移动路径（含当前格），无路径时为空。</summary>
    public required IReadOnlyList<StrategyMapPointDto> Route { get; init; }

    public string? CommanderName { get; init; }

    public int? CommanderId { get; init; }

    public required int Morale { get; init; }

    public required int Training { get; init; }

    public required string CultureName { get; init; }

    public required string ReligionName { get; init; }

    public required int Money { get; init; }

    /// <summary>兵种/备队构成（出征编组时确定；无则空列表）。</summary>
    public required IReadOnlyList<StrategySubUnitStateDto> Composition { get; init; }

    /// <summary>补给三态：Sufficient / Strained / CutOff。</summary>
    public required string SupplyStatus { get; init; }

    /// <summary>携带粮预计可维持天数。</summary>
    public required int FoodDaysRemaining { get; init; }

    /// <summary>在途运输队补给摘要。</summary>
    public required IReadOnlyList<StrategyInTransitSupplyDto> InTransitSupplies { get; init; }

    /// <summary>姿态（Normal / Attacking / Surrounding / …）。</summary>
    public required string Stance { get; init; }

    /// <summary>攻城方式（None / Encircle / Assault）。</summary>
    public required string SiegeMode { get; init; }

    /// <summary>方针目标据点 Id（0=无）。</summary>
    public required int DirectiveTargetId { get; init; }

    public string? TargetStrongholdName { get; init; }

    public int TargetUnitId { get; init; }

    public string? TargetUnitName { get; init; }

    /// <summary>所属地图战场 Id；0 表示未入战。</summary>
    public int BattlefieldId { get; init; }

    /// <summary>是否在地图层渲染（迷雾外己方单位仍可在侧栏列出）。</summary>
    public bool MapVisible { get; init; } = true;

    /// <summary>情报模糊兵数（如 **** / 3***）；为空则显示 <see cref="Soldiers"/>。</summary>
    public string? SoldiersDisplay { get; init; }

    /// <summary>情报模糊士气档（高/中/低）。</summary>
    public string? MoraleBand { get; init; }

    /// <summary>情报模糊训练档（高/中/低）。</summary>
    public string? TrainingBand { get; init; }
}

/// <summary>迷雾外己方部队摘要（不含地图坐标与路径）。</summary>
public sealed record StrategyUnitRosterEntryDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int Soldiers { get; init; }

    public required string Status { get; init; }

    public required string Directive { get; init; }

    public required int Ap { get; init; }

    public required string SupplyStatus { get; init; }

    public string? CommanderName { get; init; }

    /// <summary>当前不在玩家视野内。</summary>
    public required bool OffMap { get; init; }
}

/// <summary>战场内按势力汇总的参战摘要（悬浮框用）。</summary>
public sealed record StrategyBattlefieldParticipantDto
{
    public required int ForceId { get; init; }

    public required string ForceName { get; init; }

    public required int Soldiers { get; init; }

    public required int Morale { get; init; }

    public required int Money { get; init; }

    public required int Food { get; init; }
}

/// <summary>地图交战战场摘要（替代叠军图标显示）。</summary>
public sealed record StrategyBattlefieldStateDto
{
    public required int Id { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    /// <summary>Field | Siege</summary>
    public required string Kind { get; init; }

    public required int StandoffDays { get; init; }

    /// <summary>围城格进攻方式：Assault | Encircle；野战为 null。</summary>
    public string? SiegeThreat { get; init; }

    /// <summary>两侧合计兵力（兼容）。</summary>
    public required int SoldierTotal { get; init; }

    /// <summary>攻方（战争进攻侧）合计兵力；围城格「围」下仅显示此项。</summary>
    public required int AggressorSoldierTotal { get; init; }

    public required IReadOnlyList<StrategyBattlefieldParticipantDto> Participants { get; init; }

    public required IReadOnlyList<int> UnitIds { get; init; }
}

/// <summary>单位在途补给摘要。</summary>
public sealed record StrategyInTransitSupplyDto
{
    public required int ConvoyId { get; init; }

    public required int CargoFoodGo { get; init; }

    public required int EstimatedDays { get; init; }

    public required bool IsDeceived { get; init; }

    public string? OriginStrongholdName { get; init; }
}

/// <summary>单位内子编制（兵种/备队）摘要。</summary>
public sealed record StrategySubUnitStateDto
{
    public required int Id { get; init; }

    public required int TypeId { get; init; }

    public required string TypeName { get; init; }

    public required int Soldiers { get; init; }

    /// <summary>占该单位总兵数百分比（0–100）。</summary>
    public required int RatioPercent { get; init; }

    public int? CommanderId { get; init; }

    public string? CommanderName { get; init; }
}

/// <summary>地图格点坐标。</summary>
public sealed record StrategyMapPointDto
{
    public required int X { get; init; }

    public required int Y { get; init; }
}

/// <summary>路径预览响应。</summary>
public sealed record StrategyPathPreviewDto
{
    public required IReadOnlyList<StrategyMapPointDto> Points { get; init; }
}

/// <summary>瞬间战战前预览（M3-a）。</summary>
public sealed record StrategyBattlePreviewDto
{
    public required int AttackerUnitId { get; init; }

    public required int DefenderUnitId { get; init; }

    public required int TargetX { get; init; }

    public required int TargetY { get; init; }

    public required int AttackerWinRatePercent { get; init; }

    public required int AttackerSoldiers { get; init; }

    public required int DefenderSoldiers { get; init; }

    public required string DefenderName { get; init; }

    public required int EstimatedAttackerLossMin { get; init; }

    public required int EstimatedAttackerLossMax { get; init; }

    public required int EstimatedDefenderLossMin { get; init; }

    public required int EstimatedDefenderLossMax { get; init; }

    public required int ResolutionSeed { get; init; }
}

/// <summary>瞬间战结算结果（M3-a）。</summary>
public sealed record StrategyBattleResultDto
{
    public required bool AttackerWon { get; init; }

    public required int AttackerUnitId { get; init; }

    public required int DefenderUnitId { get; init; }

    public required int AttackerForceId { get; init; }

    public required int DefenderForceId { get; init; }

    public required string AttackerName { get; init; }

    public required string DefenderName { get; init; }

    public required int AttackerSoldiersBefore { get; init; }

    public required int DefenderSoldiersBefore { get; init; }

    public required int AttackerCasualties { get; init; }

    public required int DefenderCasualties { get; init; }

    public required int AttackerSoldiersAfter { get; init; }

    public required int DefenderSoldiersAfter { get; init; }

    public required int AttackerWinRatePercent { get; init; }

    public required int ResolutionSeed { get; init; }

    public required int ResolutionRoll { get; init; }

    /// <summary>FieldBattle | Ambush | Siege</summary>
    public required string EngagementKind { get; init; }

    public required IReadOnlyList<StrategyBattleLogEntryDto> LogEntries { get; init; }

    public required IReadOnlyList<StrategyBattleFactorNoteDto> FactorNotes { get; init; }

    /// <summary>攻方驰援部队名（不含主队）。</summary>
    public IReadOnlyList<string> AttackerReinforcementNames { get; init; } = [];

    /// <summary>守方驰援部队名（不含主队）。</summary>
    public IReadOnlyList<string> DefenderReinforcementNames { get; init; } = [];

    /// <summary>是否劝降成功（零伤亡）。</summary>
    public bool IsSurrendered { get; init; }
}

/// <summary>胜负因素修正明细。</summary>
public sealed record StrategyBattleFactorNoteDto
{
    public required string FactorId { get; init; }

    public required string Label { get; init; }

    public required int AttackerWinRateDelta { get; init; }

    public required int DefenderWinRateDelta { get; init; }

    public string? Detail { get; init; }
}

/// <summary>战斗过程日志条目。</summary>
public sealed record StrategyBattleLogEntryDto
{
    public required int Order { get; init; }

    /// <summary>attacker / defender / system</summary>
    public required string Side { get; init; }

    public required string Phase { get; init; }

    public required string Message { get; init; }
}

/// <summary>瞬间战执行响应：世界状态 + 战斗结果。</summary>
public sealed record StrategyInstantBattleResponseDto
{
    public required StrategyWorldStateDto State { get; init; }

    public required StrategyBattleResultDto Result { get; init; }
}

/// <summary>方针变更响应（M3-b）。</summary>
public sealed record StrategyPolicyChangeResponseDto
{
    public required StrategyWorldStateDto State { get; init; }

    /// <summary>AppliedImmediately | MessengerDispatched</summary>
    public required string Outcome { get; init; }
}

/// <summary>日推进响应：信使抵达等事件；战报详情随 BattleReportArrived 事件送达。</summary>
public sealed record StrategyAdvanceDayResponseDto
{
    public required StrategyWorldStateDto State { get; init; }

    /// <summary>已废弃即时战报通道；保留空列表以兼容旧客户端。详情见 Events[].BattleResult。</summary>
    public required IReadOnlyList<StrategyBattleResultDto> ResolvedBattles { get; init; }

    /// <summary>本日推进期间信使抵达等事件，供左上角消息栏展示。</summary>
    public required IReadOnlyList<StrategyEventDto> Events { get; init; }

    /// <summary>日推进 debug 日志写入路径（启用写文件时）。</summary>
    public string? DayDebugLogPath { get; init; }

    /// <summary>本日 debug 日志条目数（内存缓冲）。</summary>
    public int DayDebugEntryCount { get; init; }
}

/// <summary>当主位置摘要。</summary>
public sealed record StrategyLordStateDto
{
    public required string Name { get; init; }

    /// <summary>领兵时的单位 Id；否则为 null。</summary>
    public int? UnitId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    /// <summary>当主居城名（势力君主名义上的居城据点）。</summary>
    public string? ResidenceStrongholdName { get; init; }
}

/// <summary>运输队摘要（非军事单位，情报字段与 <see cref="StrategyUnitStateDto"/> 对齐）。</summary>
public sealed record StrategySupplyConvoyStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    /// <summary>恒为 false（非军事单位）。</summary>
    public required bool IsMilitary { get; init; }

    public string? CommanderName { get; init; }

    public int? CommanderId { get; init; }

    /// <summary>人夫 + 护卫合计人数。</summary>
    public required int Soldiers { get; init; }

    public required int PorterCount { get; init; }

    public required int EscortSoldierCount { get; init; }

    /// <summary>载粮（合），对应单位 <see cref="StrategyUnitStateDto.Food"/>。</summary>
    public required int Food { get; init; }

    public required int Ap { get; init; }

    public required int Movement { get; init; }

    public required string Status { get; init; }

    public required string Directive { get; init; }

    public required IReadOnlyList<StrategyMapPointDto> Route { get; init; }

    public required int Morale { get; init; }

    public required int Training { get; init; }

    public required string CultureName { get; init; }

    public required string ReligionName { get; init; }

    public required int Money { get; init; }

    public required int TargetUnitId { get; init; }

    public string? TargetUnitName { get; init; }

    public required int OriginStrongholdId { get; init; }

    public string? OriginStrongholdName { get; init; }

    /// <summary>是否处于卸粮后返回出发据点的返程阶段。</summary>
    public required bool IsReturningToOrigin { get; init; }

    /// <summary>兼容旧字段；等同 <see cref="Food"/>。</summary>
    public required int CargoFoodGo { get; init; }
}

/// <summary>信使摘要（非军事单位，情报字段与兵队对齐；编制为 NPC 传令兵/护卫，无总将）。</summary>
public sealed record StrategyMessengerStateDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int ForceId { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    /// <summary>恒为 false（非军事单位）。</summary>
    public required bool IsMilitary { get; init; }

    /// <summary>传令兵 + 护卫合计人数。</summary>
    public required int Soldiers { get; init; }

    public required int CourierCount { get; init; }

    public required int EscortSoldierCount { get; init; }

    public required int Ap { get; init; }

    public required int Movement { get; init; }

    public required string Status { get; init; }

    public required string PayloadType { get; init; }

    public required string Directive { get; init; }

    public required IReadOnlyList<StrategyMapPointDto> Route { get; init; }

    public required int Morale { get; init; }

    public required int Training { get; init; }

    public required string CultureName { get; init; }

    public required string ReligionName { get; init; }

    public required int Money { get; init; }

    public required int TargetUnitId { get; init; }

    public string? TargetUnitName { get; init; }

    public required int OriginStrongholdId { get; init; }

    public string? OriginStrongholdName { get; init; }

    /// <summary>PolicyChange 时在途待生效方针。</summary>
    public string? PendingDirective { get; init; }
}

/// <summary>地图单格坐标（visible 列表等）。</summary>
public sealed record StrategyMapCellDto
{
    public required int X { get; init; }

    public required int Y { get; init; }
}

/// <summary>玩家势力战争迷雾 DTO。</summary>
public sealed record StrategyVisibilityDto
{
    public required string FogMode { get; init; }

    public required string IntelMode { get; init; }

    public required string ControlMode { get; init; }

    public required bool InstantEventMessages { get; init; }

    public required bool AllySharedVision { get; init; }

    public required int MapWidth { get; init; }

    public required int MapHeight { get; init; }

    /// <summary>Explored bitset（32 位字数组，行优先）。</summary>
    public required IReadOnlyList<uint> ExploredBits { get; init; }

    public required IReadOnlyList<StrategyMapCellDto> VisibleCells { get; init; }

    public required IReadOnlyList<int> KnownStrongholdIds { get; init; }
}

/// <summary>开局选项 DTO（供前端展示）。</summary>
public sealed record GameStartOptionsDto
{
    public required string FogMode { get; init; }

    public required string IntelMode { get; init; }

    public required string ControlMode { get; init; }

    public required bool AllySharedVision { get; init; }

    public required bool InstantEventMessages { get; init; }
}

/// <summary>存档中的探索态。</summary>
public sealed record StrategyVisibilitySaveDto
{
    public required IReadOnlyList<uint> ExploredBits { get; init; }

    public required IReadOnlyList<int> KnownStrongholdIds { get; init; }
}

/// <summary>将 <see cref="GameWorld"/> 映射为 API DTO。</summary>
public static class StrategyWorldStateMapper
{
    public static StrategyWorldStateDto ToDto(
        GameWorld world,
        string scenarioId,
        StrategyScenarioMeta meta,
        StrategyVisibilityLedger? visibilityLedger = null,
        StrategyEspionageIntelLedger? espionageLedger = null)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var date = world.GameData.GameDate;
        var options = meta.StartOptions;
        var visibilityState = visibilityLedger?.GetOrCreate(meta.PlayerForceId);
        var intelPolicy = IntelPolicyFactory.Create(options.IntelMode);
        var visibleCells = visibilityState?.VisibleCells ?? [];
        var lordLocation = StrategyLordHelper.ResolveLocation(world.GameData, meta);
        var lordResidenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            meta.PlayerForceId,
            world.GameData,
            meta);
        var lordResidenceName = world.GameData.Strongholds.TryGetValue(lordResidenceId, out var residenceSh)
            ? residenceSh.Name
            : null;

        return new StrategyWorldStateDto
        {
            ScenarioId = scenarioId,
            PlayerForceId = meta.PlayerForceId,
            Difficulty = meta.Difficulty.ToString(),
            SimulationSeed = world.GameData.SimulationSeed,
            Lord = new StrategyLordStateDto
            {
                Name = meta.LordName,
                UnitId = meta.LordUnitId,
                X = lordLocation.X,
                Y = lordLocation.Y,
                ResidenceStrongholdName = lordResidenceName
            },
            Map = new StrategyMapStateDto
            {
                Name = world.GameMapMasterData.Name,
                Width = tileMap.Width,
                Height = tileMap.Height
            },
            Date = new StrategyDateStateDto
            {
                Year = date.Year,
                Month = date.Month,
                Day = date.Day
            },
            Forces = [.. world.GameData.Forces.Values
                .Select(f =>
                {
                    var realmRootId = TributeRoutingHelper.ResolveRealmRootForceId(f.Id, world.GameData);
                    return new StrategyForceStateDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Food = f.Food,
                        Money = f.Money,
                        Status = f.Status.ToString(),
                        SuzerainForceId = f.SuzerainForceId,
                        StrongholdCount = world.GameData.Strongholds.Values.Count(s =>
                            TributeRoutingHelper.ResolveRealmRootForceId(s.ForceId, world.GameData) == realmRootId),
                        CharacterCount = world.GameData.Characters.Values.Count(c =>
                            TributeRoutingHelper.ResolveRealmRootForceId(c.ForceId, world.GameData) == realmRootId),
                        Prestige = f.Prestige,
                        Orthodoxy = f.Orthodoxy,
                        LordResidenceStrongholdId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
                            f.Id,
                            world.GameData,
                            meta),
                        InternalArrearsFoodGo = f.InternalArrearsFoodGo,
                        InternalArrearsMoney = f.InternalArrearsMoney,
                        SuccessorId = f.Successor
                    };
                })
                .OrderBy(f => f.Id)],
            Strongholds = visibilityState is null
                ? [.. world.GameData.Strongholds.Values
                    .Select(s => MapStronghold(s, meta, world.GameData, world.GameMasterData))
                    .OrderBy(s => s.Id)]
                : [.. world.GameData.Strongholds.Values
                    .Select(s => MapStronghold(s, meta, world.GameData, world.GameMasterData))
                    .Select(dto => StrategyFogDtoRules.ApplyStrongholdFog(
                        dto, meta, world.GameData, visibilityState, tileMap.Width))
                    .Where(dto => dto is not null)
                    .Cast<StrategyStrongholdStateDto>()
                    .Select(dto => ApplyStrongholdEspionageMask(dto, meta, world.GameData, espionageLedger))
                    .OrderBy(s => s.Id)],
            Units = MapFoggedUnits(world, meta, visibilityState, intelPolicy, visibleCells, espionageLedger),
            OwnUnitRoster = MapOwnUnitRoster(world, meta, visibilityState),
            Battlefields = [.. world.GameData.Battlefields.Values
                .Where(b => !b.IsClosed)
                .Where(b => visibilityState is null
                    || StrategyFogDtoRules.IsMapEntityVisible(
                        b.Location.X, b.Location.Y, meta, visibilityState))
                .Select(b => MapBattlefield(b, world.GameData))
                .OrderBy(b => b.Id)],
            SupplyConvoys = [.. world.GameData.SupplyConvoys.Values
                .Where(c => visibilityState is null
                    || StrategyFogDtoRules.IsMapMobileEntityVisible(
                        c.Location.X, c.Location.Y, c.ForceId, meta, world.GameData, visibilityState))
                .Select(c => MapConvoy(c, world.GameData))
                .OrderBy(c => c.Id)],
            Messengers = [.. world.GameData.Messengers.Values
                .Where(m => visibilityState is null
                    || StrategyFogDtoRules.IsMapMobileEntityVisible(
                        m.Location.X, m.Location.Y, m.ForceId, meta, world.GameData, visibilityState))
                .Select(m => MapMessenger(m, world.GameData))
                .OrderBy(m => m.Id)],
            Characters = [.. world.GameData.Characters.Values
                .Select(c => MapCharacter(c, world.GameData.GameDate))
                .OrderBy(c => c.Id)],
            MapCharacters = MapMapCharacters(world, meta, visibilityState),
            EspionageIntel = MapEspionageIntel(espionageLedger),
            Diplomacies = MapPlayerDiplomacies(meta.PlayerForceId, world.GameData),
            MasterData = MapMasterData(world.GameMasterData, world.GameMapMasterData),
            Visibility = visibilityLedger?.BuildDto(world, meta),
            StartOptions = StrategyFogDtoRules.ToOptionsDto(options)
        };
    }

    public static StrategyMapMasterDto ToMapMasterDto(GameWorld world, string scenarioId)
    {
        var master = world.GameMapMasterData;
        var tileMap = master.TileMap;

        return new StrategyMapMasterDto
        {
            ScenarioId = scenarioId,
            Name = master.Name,
            Width = tileMap.Width,
            Height = tileMap.Height,
            Terrains = [.. master.Terrains
                .Select(kv => new StrategyTerrainDefDto
                {
                    Id = kv.Key,
                    Key = kv.Value.Description,
                    Name = kv.Value.Name,
                    MovementCost = kv.Value.MovementCost
                })
                .OrderBy(t => t.Id)],
            Regions = [.. master.Regions.Values
                .Select(r => new StrategyRegionDefDto
                {
                    Id = r.Id,
                    Key = r.Description,
                    Name = r.Name
                })
                .OrderBy(r => r.Id)],
            RoadTypes = [.. master.Roads.Values
                .Select(r => new StrategyRoadTypeDefDto
                {
                    Id = r.Id,
                    Key = r.Description,
                    Name = r.Name,
                    SpeedBonus = r.SpeedBonus,
                    MovementCost = r.MovementCostOverride
                })
                .OrderBy(r => r.Id)],
            TerrainIds = MapTerrainIds(tileMap),
            RegionIds = MapRegionIds(tileMap),
            RoadCells = MapRoadCells(world.GameMapData.Roads, master.Roads, tileMap),
            Landmarks = MapLandmarks(master.Landmarks)
        };
    }

    private static Dictionary<string, string> MasterFields(params (string key, string? value)[] pairs)
        => pairs
            .Where(p => !string.IsNullOrWhiteSpace(p.value))
            .ToDictionary(p => p.key, p => p.value!.Trim());

    private static StrategyMasterDataSnapshotDto MapMasterData(
        GameMasterData gameMaster,
        GameMapMasterData mapMaster)
        => new()
        {
            CultureGroups = [.. gameMaster.CultureGroups.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Fields = MasterFields(("description", x.Description))
                })
                .OrderBy(x => x.Id)],
            Cultures = [.. gameMaster.Cultures.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Group = gameMaster.CultureGroups.TryGetValue(x.CultureGroupId, out var g)
                        ? g.Name
                        : null,
                    Description = x.Description,
                    Fields = MasterFields(
                        ("cultureGroup", gameMaster.CultureGroups.TryGetValue(x.CultureGroupId, out var cg)
                            ? cg.Name
                            : null),
                        ("cultureGroupId", x.CultureGroupId.ToString()),
                        ("description", x.Description))
                })
                .OrderBy(x => x.Id)],
            ReligionGroups = [.. gameMaster.ReligionGroups.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Fields = MasterFields(
                        ("level", x.Level.ToString()),
                        ("description", x.Description))
                })
                .OrderBy(x => x.Id)],
            Religions = [.. gameMaster.Religions.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Group = gameMaster.ReligionGroups.TryGetValue(x.ReligionGroupId, out var g)
                        ? g.Name
                        : null,
                    Description = x.Description,
                    Fields = MasterFields(
                        ("religionGroup", x.ReligionGroupName),
                        ("religionGroupId", x.ReligionGroupId.ToString()),
                        ("level", x.Level.ToString()),
                        ("exclusivism", x.Exclusivism.ToString()),
                        ("centralization", x.Centralization.ToString()),
                        ("doctrinalDifference", x.DoctrinalDifference.ToString()),
                        ("description", x.Description))
                })
                .OrderBy(x => x.Id)],
            Weathers = StrategyMasterDataWeatherCatalog.BuildEntries(),
            StrongholdTypes = [.. gameMaster.StrongholdTypes.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Fields = MasterFields(
                        ("category", x.Category.ToString()),
                        ("cultureId", x.CultureId?.ToString()),
                        ("guardianNumber", x.NecessaryGuardianNumber.ToString()),
                        ("cost", x.Cost.ToString()),
                        ("maintenance", x.Maintenance.ToString()),
                        ("description", x.Description))
                })
                .OrderBy(x => x.Id)],
            DefenseFacilityTypes = [.. gameMaster.DefenseFacilityTypes.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Group = x.Category.ToString(),
                    Fields = MasterFields(
                        ("category", x.Category.ToString()),
                        ("level", x.Level.ToString()),
                        ("attack", x.Attack.ToString()),
                        ("defense", x.Defense.ToString()),
                        ("movement", x.Movement.ToString()),
                        ("cost", x.Cost.ToString()),
                        ("maintenance", x.Maintenance.ToString()))
                })
                .OrderBy(x => x.Id)],
            UnitTypes = [.. gameMaster.UnitTypes.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Fields = MasterFields(
                        ("attack", x.Attack.ToString()),
                        ("defense", x.Defense.ToString()),
                        ("attackRange", x.AttackRange.ToString()),
                        ("movement", x.Movement.ToString()),
                        ("cultureId", x.CultureId?.ToString()),
                        ("cost", x.Cost.ToString()),
                        ("maintenanceMoney", x.MaintenanceMoney.ToString()),
                        ("maintenanceFood", x.MaintenanceFood.ToString()),
                        ("description", x.Description))
                })
                .OrderBy(x => x.Id)],
            CharacterDefinitions = [],
            Terrains = [.. mapMaster.Terrains
                .Select(kv => new StrategyMasterDataEntryDto
                {
                    Id = kv.Key,
                    Name = kv.Value.Name,
                    Description = kv.Value.Description,
                    Fields = MasterFields(
                        ("terrainType", kv.Value.Type.ToString()),
                        ("movementCost", kv.Value.MovementCost.ToString()),
                        ("altitude", kv.Value.Altitude.ToString()),
                        ("description", kv.Value.Description))
                })
                .OrderBy(x => x.Id)],
            Climates = [.. mapMaster.Climates.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Fields = MasterFields(
                        ("springTemperature", x.SpringClimate.BaseTemperature.ToString()),
                        ("springWetness", x.SpringClimate.BaseWet.ToString()),
                        ("summerTemperature", x.SummerClimate.BaseTemperature.ToString()),
                        ("summerWetness", x.SummerClimate.BaseWet.ToString()),
                        ("autumnTemperature", x.AutumnClimate.BaseTemperature.ToString()),
                        ("autumnWetness", x.AutumnClimate.BaseWet.ToString()),
                        ("winterTemperature", x.WinterClimate.BaseTemperature.ToString()),
                        ("winterWetness", x.WinterClimate.BaseWet.ToString()),
                        ("description", x.Description))
                })
                .OrderBy(x => x.Id)],
            Regions = [.. mapMaster.Regions.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Fields = MasterFields(("description", x.Description))
                })
                .OrderBy(x => x.Id)],
            Roads = [.. mapMaster.Roads.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Fields = MasterFields(
                        ("speedBonus", x.SpeedBonus.ToString()),
                        ("movementCost", x.MovementCostOverride?.ToString()),
                        ("description", x.Description))
                })
                .OrderBy(x => x.Id)],
            Landmarks = [.. mapMaster.Landmarks.Values
                .Select(x => new StrategyMasterDataEntryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Fields = MasterFields(
                        ("x", x.Location.X.ToString()),
                        ("y", x.Location.Y.ToString()))
                })
                .OrderBy(x => x.Id)],
            TerrainVegetationFeatures = [.. mapMaster.TerrainVegatationFeatures
                .Select(kv => new StrategyMasterDataEntryDto
                {
                    Id = kv.Key,
                    Name = kv.Value.Name,
                    Description = kv.Value.Description,
                    Fields = MasterFields(
                        ("type", kv.Value.Type.ToString()),
                        ("description", kv.Value.Description))
                })
                .OrderBy(x => x.Id)],
            TerrainSurfaceFeatures = [.. mapMaster.TerrainSurfaceFeatures
                .Select(kv => new StrategyMasterDataEntryDto
                {
                    Id = kv.Key,
                    Name = kv.Value.Name,
                    Description = kv.Value.Description,
                    Fields = MasterFields(
                        ("type", kv.Value.Type.ToString()),
                        ("description", kv.Value.Description))
                })
                .OrderBy(x => x.Id)],
            Enums = StrategyMasterDataEnumCatalog.BuildEntries()
        };

    private static StrategyStrongholdStateDto ApplyStrongholdEspionageMask(
        StrategyStrongholdStateDto dto,
        StrategyScenarioMeta meta,
        GameData gameData,
        StrategyEspionageIntelLedger? espionageLedger)
    {
        if (meta.StartOptions.IntelMode == StrategyIntelMode.Full)
            return dto;

        return EspionageIntelRules.ApplyStrongholdMask(
            dto,
            meta.PlayerForceId,
            gameData,
            espionageLedger);
    }

    private static IReadOnlyList<StrategyEspionageIntelEntryDto> MapEspionageIntel(
        StrategyEspionageIntelLedger? espionageLedger)
        => espionageLedger is null
            ? []
            : [.. espionageLedger.Snapshot().Select(r => new StrategyEspionageIntelEntryDto
            {
                TargetKind = r.TargetKind.ToString(),
                TargetId = r.TargetId,
                Scope = r.Scope.ToString(),
                Precision = r.Precision.ToString(),
                ExpiresYear = r.ExpiresDate.Year,
                ExpiresMonth = r.ExpiresDate.Month,
                ExpiresDay = r.ExpiresDate.Day
            })];

    private static IReadOnlyList<StrategyMapCharacterStateDto> MapMapCharacters(
        GameWorld world,
        StrategyScenarioMeta meta,
        ForceVisibilityState? visibilityState)
    {
        var options = meta.StartOptions;
        return [.. world.GameData.Characters.Values
            .Where(c => !c.IsDead && c.LocationType == Character.CharacterLocationType.Map)
            .Select(c =>
            {
                var mapVisible = visibilityState is null
                    || options.FogMode == StrategyFogMode.None
                    || StrategyFogDtoRules.IsMapMobileEntityVisible(
                        c.Location.X,
                        c.Location.Y,
                        c.ForceId,
                        meta,
                        world.GameData,
                        visibilityState);

                return new StrategyMapCharacterStateDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ForceId = c.ForceId,
                    X = c.Location.X,
                    Y = c.Location.Y,
                    MapVisible = mapVisible
                };
            })
            .Where(c => c.MapVisible)
            .OrderBy(c => c.Id)];
    }

    private static StrategyCharacterSummaryDto MapCharacter(Character c, GameDate gameDate)
    {
        var age = Math.Max(0, gameDate.Year - c.Birthday.Year);
        if (gameDate.Month < c.Birthday.Month
            || (gameDate.Month == c.Birthday.Month && gameDate.Day < c.Birthday.Day))
        {
            age = Math.Max(0, age - 1);
        }

        return new StrategyCharacterSummaryDto
        {
            Id = c.Id,
            ForceId = c.ForceId,
            Name = c.Name,
            StrongholdId = c.LocationType == Character.CharacterLocationType.Stronghold
                ? c.LocationStrongholdId
                : c.StrongholdId,
            LeaderId = c.LeaderId,
            LocationType = c.LocationType.ToString(),
            ForceStatus = c.ForceStatus.ToString(),
            Leadership = c.Leadership,
            Power = c.Power,
            Politics = c.Politics,
            Strategy = c.Strategy,
            Charm = c.Charm,
            CultureName = "日本",
            ReligionName = "神道教",
            YearsInForce = 0,
            Sex = c.Sex.ToString(),
            Age = age,
            Personality = new StrategyCharacterPersonalityDto
            {
                Temper = c.Personality.Temper,
                Courage = c.Personality.Courage,
                Principle = c.Personality.Principle,
                Action = c.Personality.Action,
                Friendship = c.Personality.Friendship,
                Ambition = c.Personality.Ambition,
                Hobby = c.Personality.Hobby,
                Desire = c.Personality.Desire,
                Drinking = c.Personality.Drinking,
                Fortune = c.Personality.Fortune
            },
            Proficiency = new StrategyCharacterProficiencyDto
            {
                Infantry = c.Proficiency.Infantry.Level,
                Ride = c.Proficiency.Ride.Level,
                Archery = c.Proficiency.Archery.Level,
                Firelock = c.Proficiency.Firelock.Level,
                Sealing = c.Proficiency.Sealing.Level,
                Military = c.Proficiency.Military.Level,
                Fighting = c.Proficiency.Fighting.Level,
                Spy = c.Proficiency.Spy.Level,
                Agriculture = c.Proficiency.Agriculture.Level,
                Commerce = c.Proficiency.Commerce.Level,
                Construct = c.Proficiency.Construct.Level,
                Smelt = c.Proficiency.Smelt.Level,
                Eloquence = c.Proficiency.Eloquence.Level,
                Court = c.Proficiency.Court.Level,
                Sociality = c.Proficiency.Sociality.Level,
                Healing = c.Proficiency.Healing.Level
            },
            IsDead = c.IsDead,
            IsSick = c.IsSick,
            BirthType = c.Birth.ToString(),
            TaskRemainingDays = null,
            Loyalty = c.Personality.Friendship
        };
    }

    private static IReadOnlyList<StrategyDiplomacyStateDto> MapPlayerDiplomacies(
        int playerForceId,
        GameData gameData)
    {
        if (!gameData.Forces.TryGetValue(playerForceId, out var playerForce))
            return [];

        return [.. playerForce.Diplomacies
            .Select(d => new StrategyDiplomacyStateDto
            {
                TargetForceId = d.TargetForceId,
                Relation = d.Relation.ToString(),
                Relationship = d.Relationship,
                Trust = d.Trust,
                ArrearsFoodGo = d.ArrearsFoodGo,
                ArrearsMoney = d.ArrearsMoney
            })
            .OrderBy(d => d.TargetForceId)];
    }

    private static StrategyMessengerStateDto MapMessenger(Messenger m, GameData gameData)
    {
        gameData.Units.TryGetValue(m.TargetUnitId, out var targetUnit);
        gameData.Strongholds.TryGetValue(m.SourceStrongholdId, out var origin);

        var route = new List<StrategyMapPointDto>
        {
            new() { X = m.Location.X, Y = m.Location.Y }
        };
        foreach (var point in m.RoutePoints)
            route.Add(new StrategyMapPointDto { X = point.X, Y = point.Y });

        var directive = m.PayloadType switch
        {
            MessengerPayloadType.PolicyChange => "PolicyChange",
            MessengerPayloadType.BattleReport => "BattleReport",
            MessengerPayloadType.FalseIntelligence => "FalseIntelligence",
            MessengerPayloadType.StrategicOrder => "StrategicOrder",
            _ => m.PayloadType.ToString()
        };

        return new StrategyMessengerStateDto
        {
            Id = m.Id,
            Name = m.Name,
            ForceId = m.ForceId,
            X = m.Location.X,
            Y = m.Location.Y,
            IsMilitary = false,
            Soldiers = m.CourierCount + m.EscortSoldierCount,
            CourierCount = m.CourierCount,
            EscortSoldierCount = m.EscortSoldierCount,
            Ap = Math.Max(m.RoutePoints.Count, 1),
            Movement = LogisticsConstants.MessengerDailyAp,
            Status = m.Status.ToString(),
            PayloadType = m.PayloadType.ToString(),
            Directive = directive,
            Route = route,
            Morale = 80,
            Training = 70,
            CultureName = "日本",
            ReligionName = "神道教",
            Money = 0,
            TargetUnitId = m.TargetUnitId,
            TargetUnitName = targetUnit?.Name,
            OriginStrongholdId = m.SourceStrongholdId,
            OriginStrongholdName = origin?.Name,
            PendingDirective = m.PendingDirective?.ToString()
        };
    }

    private static StrategySupplyConvoyStateDto MapConvoy(SupplyConvoy c, GameData gameData)
    {
        string? commanderName = null;
        if (c.LeaderId > 0 && gameData.Characters.TryGetValue(c.LeaderId, out var commander))
            commanderName = commander.Name;

        gameData.Units.TryGetValue(c.TargetUnitId, out var targetUnit);
        gameData.Strongholds.TryGetValue(c.OriginStrongholdId, out var origin);

        var route = new List<StrategyMapPointDto>
        {
            new() { X = c.Location.X, Y = c.Location.Y }
        };
        foreach (var point in c.RoutePoints)
            route.Add(new StrategyMapPointDto { X = point.X, Y = point.Y });

        var directive = c.IsReturningToOrigin ? "Retreat" : "Support";

        return new StrategySupplyConvoyStateDto
        {
            Id = c.Id,
            Name = c.Name,
            ForceId = c.ForceId,
            X = c.Location.X,
            Y = c.Location.Y,
            IsMilitary = false,
            CommanderName = commanderName,
            CommanderId = c.LeaderId > 0 ? c.LeaderId : null,
            Soldiers = c.PorterCount + c.EscortSoldierCount,
            PorterCount = c.PorterCount,
            EscortSoldierCount = c.EscortSoldierCount,
            Food = c.CargoFoodGo,
            CargoFoodGo = c.CargoFoodGo,
            Ap = c.Ap,
            Movement = c.Movement > 0 ? c.Movement : LogisticsConstants.ConvoyDailyAp,
            Status = c.Status.ToString(),
            Directive = directive,
            Route = route,
            Morale = 75,
            Training = 65,
            CultureName = "日本",
            ReligionName = "神道教",
            Money = c.CargoMoney,
            TargetUnitId = c.TargetUnitId,
            TargetUnitName = targetUnit?.Name,
            OriginStrongholdId = c.OriginStrongholdId,
            OriginStrongholdName = origin?.Name,
            IsReturningToOrigin = c.IsReturningToOrigin
        };
    }

    private static StrategyBattlefieldStateDto MapBattlefield(Battlefield b, GameData gameData)
    {
        var ids = b.SideAUnitIds.Concat(b.SideBUnitIds).Distinct().ToList();
        var soldiers = 0;
        var aggressorSoldiers = 0;
        var participantBuckets = new Dictionary<int, (int Soldiers, int MoraleWeighted, int Money, int Food)>();

        foreach (var id in ids)
        {
            if (!gameData.Units.TryGetValue(id, out var u) || u.Soldier <= 0)
                continue;

            soldiers += u.Soldier;

            var onAggressorSide = gameData.Wars.TryGetValue(b.WarId, out var war)
                                  && WarRules.IsOnAggressorSide(war, u.ForceId);
            if (onAggressorSide)
                aggressorSoldiers += u.Soldier;

            if (!participantBuckets.TryGetValue(u.ForceId, out var bucket))
                bucket = (0, 0, 0, 0);

            bucket.Soldiers += u.Soldier;
            bucket.MoraleWeighted += u.Morale * u.Soldier;
            bucket.Money += u.Money;
            bucket.Food += u.Food;
            participantBuckets[u.ForceId] = bucket;
        }

        if (b.Kind == BattlefieldKind.Siege
            && b.StrongholdId > 0
            && gameData.Strongholds.TryGetValue(b.StrongholdId, out var siegeTarget))
        {
            var garrisonSoldiers = StrongholdGarrisonRules.GetCityGarrisonSoldiers(siegeTarget);
            if (garrisonSoldiers > 0 || siegeTarget.ForceActor.Money > 0 || siegeTarget.ForceActor.Food > 0)
            {
                if (!participantBuckets.TryGetValue(siegeTarget.ForceId, out var defenderBucket))
                    defenderBucket = (0, 0, 0, 0);

                defenderBucket.Soldiers += garrisonSoldiers;
                defenderBucket.MoraleWeighted += siegeTarget.ForceActor.Morale * Math.Max(1, garrisonSoldiers);
                defenderBucket.Money += siegeTarget.ForceActor.Money;
                defenderBucket.Food += siegeTarget.ForceActor.Food;
                participantBuckets[siegeTarget.ForceId] = defenderBucket;
                soldiers += garrisonSoldiers;
            }
        }

        var participants = participantBuckets
            .OrderByDescending(kv => kv.Value.Soldiers)
            .Select(kv =>
            {
                gameData.Forces.TryGetValue(kv.Key, out var force);
                var morale = kv.Value.Soldiers > 0
                    ? (int)Math.Round(kv.Value.MoraleWeighted / (double)kv.Value.Soldiers)
                    : 0;
                return new StrategyBattlefieldParticipantDto
                {
                    ForceId = kv.Key,
                    ForceName = force?.Name ?? $"势力#{kv.Key}",
                    Soldiers = kv.Value.Soldiers,
                    Morale = morale,
                    Money = kv.Value.Money,
                    Food = kv.Value.Food
                };
            })
            .ToList();

        string? siegeThreat = null;
        if (b.Kind == BattlefieldKind.Siege
            && b.StrongholdId > 0
            && gameData.Strongholds.TryGetValue(b.StrongholdId, out var stronghold))
        {
            siegeThreat = ResolveStrongholdSiegeThreat(stronghold, gameData);
        }

        return new StrategyBattlefieldStateDto
        {
            Id = b.Id,
            X = b.Location.X,
            Y = b.Location.Y,
            Kind = b.Kind.ToString(),
            StandoffDays = b.StandoffDays,
            SiegeThreat = siegeThreat,
            SoldierTotal = soldiers,
            AggressorSoldierTotal = aggressorSoldiers,
            Participants = participants,
            UnitIds = ids
        };
    }

    private static IReadOnlyList<StrategyUnitStateDto> MapFoggedUnits(
        GameWorld world,
        StrategyScenarioMeta meta,
        ForceVisibilityState? visibilityState,
        IIntelPolicy intelPolicy,
        HashSet<(int X, int Y)> visibleCells,
        StrategyEspionageIntelLedger? espionageLedger)
    {
        if (visibilityState is null)
        {
            return [.. world.GameData.Units.Values
                .Where(u => u.IsMilitary && u.Soldier > 0)
                .Select(u => MapUnit(u, meta, world.GameData))
                .OrderBy(u => u.Id)];
        }

        var mapUnits = new List<StrategyUnitStateDto>();
        foreach (var unit in world.GameData.Units.Values.OrderBy(u => u.Id))
        {
            var placement = StrategyFogDtoRules.ClassifyUnit(
                unit, meta, world.GameData, visibilityState);
            if (placement != StrategyFogDtoRules.UnitFogPlacement.Map)
                continue;

            var dto = MapUnit(unit, meta, world.GameData) with { MapVisible = true };
            dto = intelPolicy.ApplyUnitIntelMask(dto, world, meta, meta.PlayerForceId, visibleCells);
            if (meta.StartOptions.IntelMode != StrategyIntelMode.Full)
            {
                dto = EspionageIntelRules.ApplyUnitMask(
                    dto,
                    meta.PlayerForceId,
                    world.GameData,
                    espionageLedger);
            }

            mapUnits.Add(dto);
        }

        return mapUnits;
    }

    private static IReadOnlyList<StrategyUnitRosterEntryDto> MapOwnUnitRoster(
        GameWorld world,
        StrategyScenarioMeta meta,
        ForceVisibilityState? visibilityState)
    {
        if (visibilityState is null)
            return [];

        var roster = new List<StrategyUnitRosterEntryDto>();
        foreach (var unit in world.GameData.Units.Values.OrderBy(u => u.Id))
        {
            var placement = StrategyFogDtoRules.ClassifyUnit(
                unit, meta, world.GameData, visibilityState);
            if (placement != StrategyFogDtoRules.UnitFogPlacement.Roster)
                continue;

            roster.Add(ToRosterEntry(unit, meta, world.GameData));
        }

        return roster;
    }

    private static StrategyUnitRosterEntryDto ToRosterEntry(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        meta.Intel.Units.TryGetValue(unit.Id, out var overlay);
        var commander = overlay?.CommanderName;
        if (unit.LeaderId > 0 && gameData.Characters.TryGetValue(unit.LeaderId, out var commanderCharacter))
            commander = commanderCharacter.Name;
        else if (string.IsNullOrWhiteSpace(commander) && meta.LordUnitId == unit.Id)
            commander = meta.LordName;

        return new StrategyUnitRosterEntryDto
        {
            Id = unit.Id,
            Name = unit.Name,
            ForceId = unit.ForceId,
            X = unit.Location.X,
            Y = unit.Location.Y,
            Soldiers = unit.Soldier,
            Status = unit.Status.ToString(),
            Directive = unit.Directive.ToString(),
            Ap = unit.Ap,
            SupplyStatus = SupplyStatusEvaluator.EvaluateStatus(unit, gameData),
            CommanderName = string.IsNullOrWhiteSpace(commander) ? null : commander,
            OffMap = true
        };
    }

    private static StrategyUnitStateDto MapUnit(Unit u, StrategyScenarioMeta meta, GameData gameData)
    {
        meta.Intel.Units.TryGetValue(u.Id, out var overlay);
        var commander = overlay?.CommanderName;
        if (u.LeaderId > 0 && gameData.Characters.TryGetValue(u.LeaderId, out var commanderCharacter))
            commander = commanderCharacter.Name;
        else if (string.IsNullOrWhiteSpace(commander) && meta.LordUnitId == u.Id)
            commander = meta.LordName;

        return new StrategyUnitStateDto
        {
            Id = u.Id,
            Name = u.Name,
            ForceId = u.ForceId,
            X = u.Location.X,
            Y = u.Location.Y,
            Soldiers = u.Soldier,
            Food = u.Food,
            Ap = u.Ap,
            Movement = u.Movement,
            Status = u.Status.ToString(),
            Directive = u.Directive.ToString(),
            Stance = u.Stance.ToString(),
            SiegeMode = u.SiegeMode.ToString(),
            DirectiveTargetId = u.DirectiveTargetId,
            TargetStrongholdName = ResolveTargetStrongholdName(u, gameData),
            TargetUnitId = u.ActionTarget.UnitId,
            TargetUnitName = ResolveTargetUnitName(u, gameData),
            BattlefieldId = u.BattlefieldId,
            Route = BuildUnitRoute(u),
            CommanderName = string.IsNullOrWhiteSpace(commander) ? null : commander,
            CommanderId = u.LeaderId > 0 ? u.LeaderId : null,
            Morale = u.Morale,
            Training = u.Training,
            CultureName = overlay?.CultureName ?? "日本",
            ReligionName = overlay?.ReligionName ?? "神道教",
            Money = u.Money,
            Composition = MapUnitComposition(u, gameData),
            SupplyStatus = SupplyStatusEvaluator.EvaluateStatus(u, gameData),
            FoodDaysRemaining = SupplyStatusEvaluator.EstimateFoodDaysRemaining(u),
            InTransitSupplies = MapInTransitSupplies(u, gameData)
        };
    }

    private static IReadOnlyList<StrategyInTransitSupplyDto> MapInTransitSupplies(Unit u, GameData gameData)
    {
        return [.. SupplyStatusEvaluator.GetInTransitSummaries(u, gameData)
            .Select(s =>
            {
                gameData.Strongholds.TryGetValue(s.OriginStrongholdId, out var origin);
                return new StrategyInTransitSupplyDto
                {
                    ConvoyId = s.ConvoyId,
                    CargoFoodGo = s.CargoFoodGo,
                    EstimatedDays = s.EstimatedDays,
                    IsDeceived = s.IsDeceived,
                    OriginStrongholdName = origin?.Name
                };
            })];
    }

    private static IReadOnlyList<StrategySubUnitStateDto> MapUnitComposition(Unit u, GameData gameData)
    {
        if (u.SubUnitIds.Count == 0)
            return [];

        var total = Math.Max(u.Soldier, 1);
        var rows = new List<StrategySubUnitStateDto>();

        foreach (var subUnitId in u.SubUnitIds)
        {
            if (!gameData.SubUnits.TryGetValue(subUnitId, out var subUnit))
                continue;

            string? commanderName = null;
            if (subUnit.LeaderId > 0 && gameData.Characters.TryGetValue(subUnit.LeaderId, out var commander))
                commanderName = commander.Name;

            var ratio = (int)Math.Round(subUnit.Soldier * 100.0 / total, MidpointRounding.AwayFromZero);

            rows.Add(new StrategySubUnitStateDto
            {
                Id = subUnit.Id,
                TypeId = subUnit.TypeId,
                TypeName = StrategyTroopTypes.ResolveName(subUnit.TypeId, subUnit.TypeName),
                Soldiers = subUnit.Soldier,
                RatioPercent = ratio,
                CommanderId = subUnit.LeaderId > 0 ? subUnit.LeaderId : null,
                CommanderName = commanderName
            });
        }

        return rows;
    }

    private static IReadOnlyList<StrategyRoadCellDto> MapRoadCells(
        IReadOnlyDictionary<int, byte> roadCells,
        IReadOnlyDictionary<int, RoadDefinition> roads,
        TileMap tileMap)
    {
        var cells = new List<StrategyRoadCellDto>(roadCells.Count);

        foreach (var (index, typeId) in roadCells)
        {
            if (typeId == 0)
                continue;

            var p = tileMap.ToPoint3(index);
            roads.TryGetValue(typeId, out var roadDef);
            var movementCost = roadDef?.MovementCostOverride
                ?? Math.Max(1, 2 - (roadDef?.SpeedBonus ?? 0));

            cells.Add(new StrategyRoadCellDto
            {
                X = p.X,
                Y = p.Y,
                TypeId = typeId,
                TypeName = roadDef?.Name ?? $"道路#{typeId}",
                Level = typeId,
                SpeedBonus = roadDef?.SpeedBonus ?? 0,
                MovementCost = movementCost
            });
        }

        cells.Sort(static (a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
        return cells;
    }

    private static IReadOnlyList<int> MapTerrainIds(TileMap tileMap)
    {
        var ids = new int[tileMap.Length];
        for (var i = 0; i < tileMap.Length; i++)
            ids[i] = tileMap[i].Terrain;

        return ids;
    }

    private static IReadOnlyList<int> MapRegionIds(TileMap tileMap)
    {
        var ids = new int[tileMap.Length];
        for (var i = 0; i < tileMap.Length; i++)
            ids[i] = tileMap[i].Region;

        return ids;
    }

    private static IReadOnlyList<StrategyMapLandmarkDto> MapLandmarks(
        IReadOnlyDictionary<int, Landmark> points)
        => [.. points.Values
            .Select(p => new StrategyMapLandmarkDto
            {
                Id = p.Id,
                Name = p.Name,
                X = p.Location.X,
                Y = p.Location.Y
            })
            .OrderBy(p => p.Id)];

    private static StrategyStrongholdStateDto MapStronghold(
        Stronghold s,
        StrategyScenarioMeta meta,
        GameData gameData,
        GameMasterData masterData)
    {
        meta.Intel.Strongholds.TryGetValue(s.Id, out var overlay);

        var lordName = StrategyStrongholdLordHelper.ResolveStrongholdLordName(s, meta, gameData);
        var isDirectRule = StrategyStrongholdLordHelper.IsDirectRule(s);
        var isLordResidence = StrategyStrongholdLordHelper.IsGovernanceResidence(s, meta, gameData);

        var mayor = overlay?.MayorName;
        if (string.IsNullOrWhiteSpace(mayor) && s.LeaderId > 0
            && gameData.Characters.TryGetValue(s.LeaderId, out var mayorCharacter))
        {
            mayor = mayorCharacter.Name;
        }

        var facilities = MapDefenseFacilities(s, masterData);

        var typeId = s.TypeId != 0 ? s.TypeId : (byte)1;
        masterData.StrongholdTypes.TryGetValue(typeId, out var strongholdType);

        return new StrategyStrongholdStateDto
        {
            Id = s.Id,
            Name = s.Name,
            TypeId = typeId,
            TypeName = strongholdType?.Name ?? (typeId == 1 ? "平城" : $"据点类型#{typeId}"),
            ForceId = s.ForceId,
            X = s.Location.X,
            Y = s.Location.Y,
            Food = s.ForceActor.Food,
            Population = s.Population,
            Stability = s.Stability,
            PopularFeelings = s.CivilianActor.PopularFeelings,
            IsLordResidence = isLordResidence,
            LordId = s.LordId,
            IsDirectRule = isDirectRule,
            LordName = lordName,
            MayorName = string.IsNullOrWhiteSpace(mayor) ? null : mayor,
            Morale = s.ForceActor.Morale,
            Training = s.ForceActor.Training,
            CultureName = overlay?.CultureName ?? "日本",
            ReligionName = overlay?.ReligionName ?? "神道教",
            Money = s.ForceActor.Money,
            GarrisonSoldiers = s.ForceActor.Soldier,
            GarrisonWounded = s.ForceActor.Patient,
            PollTaxRate = s.PollTaxRate,
            AgricultureTaxRate = s.AgricultureTaxRate,
            CommerceTaxRate = s.CommerceTaxRate,
            TariffTaxRate = s.TariffTaxRate,
            IsHistorical = s.IsHistorical,
            Defense = facilities.Sum(f => f.Defense),
            DefenseFacilities = facilities,
            LuxuryGoods = s.ForceActor.LuxuryGoods,
            EconomyFacilities = MapEconomyFacilities(s),
            SiegeThreat = ResolveStrongholdSiegeThreat(s, gameData)
        };
    }

    private static string? ResolveStrongholdSiegeThreat(Stronghold stronghold, GameData gameData)
    {
        if (!gameData.Forces.TryGetValue(stronghold.ForceId, out var holderForce))
            return null;

        string? encircle = null;
        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0 || unit.SiegeMode == UnitSiegeMode.None)
                continue;

            if (!unit.Location.IsSameTile(stronghold.Location))
                continue;

            if (unit.ForceId == stronghold.ForceId)
                continue;

            if (!gameData.Forces.TryGetValue(unit.ForceId, out var unitForce))
                continue;

            if (!DiplomacyRules.IsEnemy(unitForce, holderForce).IsSuccess)
                continue;

            if (unit.SiegeMode == UnitSiegeMode.Assault)
                return "Assault";

            if (unit.SiegeMode == UnitSiegeMode.Encircle)
                encircle = "Encircle";
        }

        foreach (var battlefield in gameData.Battlefields.Values)
        {
            if (battlefield.IsClosed || battlefield.Kind != BattlefieldKind.Siege)
                continue;

            if (battlefield.Location.X != stronghold.Location.X
                || battlefield.Location.Y != stronghold.Location.Y)
                continue;

            return encircle ?? "Assault";
        }

        return encircle;
    }

    private static IReadOnlyList<StrategyEconomyFacilityStateDto> MapEconomyFacilities(Stronghold stronghold)
        => stronghold.EconomyFacilityIds.Count == 0
            ? []
            : [.. stronghold.EconomyFacilityIds.Select(id => new StrategyEconomyFacilityStateDto
            {
                TypeId = id,
                Name = EconomyFacilityRules.ResolveFacilityName(id)
            })];

    private static IReadOnlyList<StrategyDefenseFacilityStateDto> MapDefenseFacilities(
        Stronghold stronghold,
        GameMasterData masterData)
    {
        if (stronghold.DefenseFacilityIds.Count == 0)
            return [];

        var rows = new List<StrategyDefenseFacilityStateDto>();
        foreach (var typeId in stronghold.DefenseFacilityIds)
        {
            if (!masterData.DefenseFacilityTypes.TryGetValue(typeId, out var facilityType))
            {
                rows.Add(new StrategyDefenseFacilityStateDto
                {
                    TypeId = typeId,
                    Name = $"设施 #{typeId}",
                    Category = nameof(DefenseFacilityTypeModel.DefenseFacilityCategory.Defender),
                    Level = 1,
                    Defense = 0
                });
                continue;
            }

            rows.Add(new StrategyDefenseFacilityStateDto
            {
                TypeId = typeId,
                Name = facilityType.Name,
                Category = facilityType.Category.ToString(),
                Level = (int)facilityType.Level,
                Defense = facilityType.Defense
            });
        }

        return rows;
    }

    private static string? ResolveTargetStrongholdName(Unit u, GameData gameData)
    {
        var id = u.ActionTarget.StrongholdId > 0 ? u.ActionTarget.StrongholdId : u.DirectiveTargetId;
        if (id <= 0)
            return null;

        return gameData.Strongholds.TryGetValue(id, out var sh) ? sh.Name : null;
    }

    private static string? ResolveTargetUnitName(Unit u, GameData gameData)
    {
        if (u.ActionTarget.UnitId <= 0)
            return null;

        return gameData.Units.TryGetValue(u.ActionTarget.UnitId, out var target) ? target.Name : null;
    }

    private static IReadOnlyList<StrategyMapPointDto> BuildUnitRoute(Unit u)
    {
        if (u.ActionTarget.RoutePoints.Count == 0)
            return [];

        var route = new List<StrategyMapPointDto>
        {
            new() { X = u.Location.X, Y = u.Location.Y }
        };

        foreach (var point in u.ActionTarget.RoutePoints)
            route.Add(new StrategyMapPointDto { X = point.X, Y = point.Y });

        return route;
    }

    private static bool IsOwnRealmForce(int forceId, int playerForceId, GameData gameData)
        => TributeRoutingHelper.ResolveRealmRootForceId(forceId, gameData)
           == TributeRoutingHelper.ResolveRealmRootForceId(playerForceId, gameData);
}
