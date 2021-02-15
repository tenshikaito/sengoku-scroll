using Library;
using System;
using System.Drawing;

namespace WinLibrary.Graphic
{
    public class SpriteBase : TreeObject<SpriteBase>, IDisposable
    {
        public Point position { get; set; }

        public Point displayPosition
            => isRoot ? position : new Point(position.X + parent.position.X, position.Y + parent.position.Y);

        public Color color { get; set; }

        public SpriteBase()
        {
        }

        public SpriteBase(SpriteBase parent) : base(parent)
        {
        }
    }
}
