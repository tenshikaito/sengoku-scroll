using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.GameWorldSnapshot;

namespace SengokuScroll.Application.Extensions;

/// <summary>将运行中 <see cref="GameWorld"/> 转为只读快照（地图索引 + 角色视图）。</summary>
public static class GameWorldSnapshotExtensions
{
    /// <summary>捕获当前世界状态供 UI/存档序列化。</summary>
    public static GameWorldSnapshot ToSnapshot(this GameWorld gameWorld)
    {
        var gameMapView = new GameMapViewModel()
        {
            Characters = new(gameWorld.GameMapData.Characters),
            Strongholds = new(gameWorld.GameMapData.Strongholds),
            Units = gameWorld.GameMapData.Units.ToDictionary(
                kv => kv.Key,
                kv => new List<int>(kv.Value)),
        };

        var gameDataView = new GameDataViewModel()
        {
            Characters = gameWorld.GameData.Characters.ToDictionary(o => o.Key, o => ToCharacterViewModel(o.Value))
        };

        return new GameWorldSnapshot
        {
            GameMapView = gameMapView,
            GameMapData = gameWorld.GameMapMasterData,
            GameMasterData = gameWorld.GameMasterData,
            GameDataView = gameDataView
        };
    }

    private static CharacterViewModel ToCharacterViewModel(Character o)
        => new()
        {
            Id = o.Id,
            Name = o.Name,
            LocationType = o.LocationType,
            Location = o.Location
        };
}
