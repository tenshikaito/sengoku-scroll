using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Domain;

public class GameWorldSnapshot
{
    public required GameMapMasterData GameMapData { get; set; }

    public required GameMapViewModel GameMapView { get; set; }

    public required GameMasterData GameMasterData { get; set; }

    public required GameDataViewModel GameDataView { get; set; }

    public class GameMapViewModel
    {
        public required Dictionary<int, int> Characters { get; set; }

        public required Dictionary<int, int> Strongholds { get; set; }

        public required Dictionary<int, List<int>> Units { get; set; }
    }

    public class GameDataViewModel
    {
        public required Dictionary<int, CharacterViewModel> Characters { get; set; }
    }

    public class CharacterViewModel
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public Character.CharacterLocationType LocationType { get; set; }

        public Point3 Location { get; set; }
    }
}
