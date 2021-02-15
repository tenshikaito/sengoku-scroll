namespace Core.Model
{
    public struct MapTile
    {
        public byte terrain;
        public byte? terrainSurface;
        /// <summary>地区</summary>
        public byte region;
        public TileFunctionType? functionType;
    }

    public enum TileFunctionType : byte
    {
        none = 0,
        agriculture = 1,
        industry = 2,
        commerce = 3,
        residence = 4
    }
}
