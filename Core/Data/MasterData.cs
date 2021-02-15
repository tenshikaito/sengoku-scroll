using Core.Model;
using System.Collections.Generic;

namespace Core.Data
{
    public class MasterData
    {
        public Dictionary<int, Region> region;
        public Dictionary<int, Culture> culture;
        public Dictionary<int, Religion> religion;
        public Dictionary<int, Road> road;

        public Dictionary<int, Terrain> tileMapTerrain;
        //public Dictionary<int, Stronghold.Type> strongholdType;

        public Dictionary<int, TerrainImage> terrainImage;

        public TileMapImageInfo tileMapViewImageInfo;
        public TileMapImageInfo tileMapDetailImageInfo;
    }
}
