using Core.Data;
using Library.Extension;
using Library.Model;
using Library.Network;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Core.Helper
{
    public static class GameExtension
    {
        public static string toCommandString(this BaseData gc, string name) => $"{name}:{gc.toJson()}";

        public static (string name, string data) fromCommandString(this string text)
        {
            var arr = text.Split(':');

            return (arr[0], text[(text.IndexOf(':') + 1)..]);
        }

        public static T? getId<T>(this Dictionary<int, T> map, TileMap tm, MapPoint p) where T : struct
        {
            if (tm.isOutOfBounds(p)) return null;

            if (map.TryGetValue(tm.getIndex(p), out var value)) return value;

            return null;
        }

        public static List<TileAnimationFrame> toAnimationFrameList(this string text)
            => string.IsNullOrWhiteSpace(text)
            ? null
            : text.Replace(Environment.NewLine, string.Empty).Trim(';').Split(';').Select(o =>
            {
                var arr = o.Split(':');
                var vertex = arr[1].Split(',');

                return new TileAnimationFrame()
                {
                    fileName = arr[0],
                    vertex = new Point(int.Parse(vertex[0]), int.Parse(vertex[1]))
                };
            }).ToList();

        public static string toString(this List<TileAnimationFrame> list)
            => string.Join(
                ";" + Environment.NewLine,
                list == null
                ? Array.Empty<string>()
                : list.Select(o => $"{o.fileName}:{o.vertex.X},{o.vertex.Y}").ToArray());
    }
}
