using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain;

/// <summary>运行时世界状态：势力、据点、单位、运输队、信使等。</summary>
public class GameData
{
    /// <summary>当前游戏内日期。</summary>
    public GameDate GameDate { get; set; }

    /// <summary>全部势力，Key 为势力 Id。</summary>
    public required Dictionary<int, Force> Forces { get; init; } = [];

    /// <summary>全部据点，Key 为据点 Id。</summary>
    public required Dictionary<int, Stronghold> Strongholds { get; init; } = [];

    /// <summary>全部军事单位，Key 为单位 Id。</summary>
    public required Dictionary<int, Unit> Units { get; init; } = [];

    /// <summary>全部子编制（兵种/备队），Key 为 SubUnit Id。</summary>
    public required Dictionary<int, SubUnit> SubUnits { get; init; } = [];

    /// <summary>全部角色，Key 为角色 Id。</summary>
    public required Dictionary<int, Character> Characters { get; set; } = [];

    /// <summary>地图上所有运输队，Key 为运输队 Id。</summary>
    public required Dictionary<int, SupplyConvoy> SupplyConvoys { get; init; } = [];

    /// <summary>地图上所有信使，Key 为信使 Id。</summary>
    public required Dictionary<int, Messenger> Messengers { get; init; } = [];
}
