using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.GameWorldSnapshot;

namespace SengokuScroll.Application.Extensions;

public static class GameWorldSnapshotExtensions
{
    public static GameWorldSnapshot ToSnapshot(this GameWorld gameWorld)
    {
        var gameMapView = new GameMapViewModel()
        {
            Characters = new(gameWorld.GameMapData.Characters),
            Strongholds = new(gameWorld.GameMapData.Strongholds),
            Units = new(gameWorld.GameMapData.Units),
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
