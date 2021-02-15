using Core.Model;
using Library.Component;
using Library.Model;
using System;
using System.Collections.Generic;

namespace Core.Data
{
    public class TileMap : TileMap2D
    {
        public byte[] terrain;
        public byte[] region;

        public Dictionary<int, byte> terrainSurface;

        public Dictionary<int, int> territory;
        public Dictionary<int, int> road;

        public Dictionary<int, int> stronghold;
        public Dictionary<int, int> unit;

        public Dictionary<int, TileFunctionType> tileFunctionType;

        public MapTile this[int index] => new MapTile()
        {
            terrain = terrain[index],
            terrainSurface = terrainSurface.TryGetValue(index, out var ts) ? (byte?)ts : null,
            region = region[index],
            functionType = tileFunctionType.TryGetValue(index, out var ft) ? (TileFunctionType?)ft : null
        };

        public MapTile this[MapPoint p] => this[getIndex(p)];

        public TileMap(Size s) : base(s)
        {
        }

        public void each(Action<int, MapTile> foreachCallback) => each(0, count, foreachCallback);

        public void each(int startIndex, int length, Action<int, MapTile> foreachCallback)
        {
            for (int i = startIndex, l = count; i < length; ++i)
            {
                if (i >= l) break;
                foreachCallback(i, this[i]);
            }
        }

        public void setTerrain(MapPoint p, byte id, bool isSurface = true)
        {
            var index = getIndex(p);

            if (isSurface) terrainSurface[index] = id;

            else terrain[index] = id;
        }
    }
}
