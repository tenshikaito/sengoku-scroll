using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain;

public class GameMasterData
{
    /// <summary>
    /// 技能熟练度最大等级
    /// </summary>
    public byte ProficiencyMaxValue = 5;

    /// <summary>
    /// 技能熟练度升级最大经验值
    /// </summary>
    public byte ProficiencyExpMaxValue = 100;

    public required Dictionary<int, CultureGroupDefinition> CultureGroups { get; set; }

    public required Dictionary<int, CultureDefinition> Cultures { get; set; }

    public required Dictionary<int, ReligionGroupDefinition> ReligionGroups { get; set; }

    public required Dictionary<int, ReligionDefinition> Religions { get; set; }

    public required Dictionary<int, StrongholdType> StrongholdTypes { get; set; }

    public required Dictionary<int, DefenseFacilityTypeModel> DefenseFacilityTypes { get; set; }

    public required Dictionary<int, UnitTypeDefinition> UnitTypes { get; set; }

    public required Dictionary<int, CharacterDefinition> Characters { get; set; }


}
