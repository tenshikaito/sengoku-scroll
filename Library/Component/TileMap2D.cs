using Library.Model;
using System;

namespace Library.Component
{
    public class TileMap2D
    {
        public int column => size.column;
        public int row => size.row;
        public int count => size.count;

        public Size size;

        public TileMap2D(Size s) => size = s;

        public bool isOutOfBounds(int index) => index < 0 || index >= count;

        public bool isOutOfBounds(MapPoint p) => p.x < 0 || p.y < 0 || p.x >= column || p.y >= row;

        public int getIndex(MapPoint p) => p.y * column + p.x;

        public MapPoint getPoint(int index) => new MapPoint((int)getX(index), (int)getY(index));

        public int? getX(int index) => isOutOfBounds(index) ? (int?)null : index % column;

        public int? getY(int index) => isOutOfBounds(index) ? (int?)null : index / column;

        public void checkBound(ref MapPoint p)
        {
            if (p.x < 0) p.x = 0;
            else if (p.x >= column) p.x = column - 1;
            if (p.y < 0) p.y = 0;
            else if (p.y >= row) p.y = row - 1;
        }

        public void eachRectangle(MapPoint p, Size s, Action<MapPoint> each)
        {
            MapPoint tp;
            Size ts;
            for (tp.y = p.y, ts.row = p.y + s.row, ts.column = p.x + s.column; tp.y < ts.row; ++tp.y)
            {
                for (tp.x = p.x; tp.x < ts.column; ++tp.x)
                {
                    if (isOutOfBounds(tp)) continue;

                    each(tp);
                }
            }
        }

        public void eachRangedRectangle(MapPoint p, Size radius, Action<MapPoint> each)
        {
            MapPoint tp;
            Size ts;
            for (tp.y = p.y - radius.row, ts.row = p.y + radius.row, ts.column = p.x + radius.column; tp.y <= ts.row; ++tp.y)
            {
                for (tp.x = p.x - radius.column; tp.x <= ts.column; ++tp.x)
                {
                    if (isOutOfBounds(tp)) continue;

                    each(tp);
                }
            }
        }

        public struct Size
        {
            public int row;
            public int column;

            public int count => row * column;

            public Size(int value) => row = column = value;

            public Size(int row, int column)
            {
                this.row =  row;
                this.column = column;
            }
        }
    }
}
