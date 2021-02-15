using Library;

namespace Core.Model
{
    /// <summary>地形样式，同一种类型可能有不同样式</summary>
    public class Terrain : BaseObject
    {
        public int id;

        public string name;

        public int apCost;

        public int imageId;

        public bool isSurface;

        public bool isGrass;

        public bool isForest;

        public bool isWaste;

        public bool isDesert;

        public bool isMarsh;

        public bool isRock;

        public bool isHill;

        public bool isMountain;

        public bool isHarbour;

        public bool isWater;

        public bool isFreshWater;

        public bool isDeepWater;
    }
}
