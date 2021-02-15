using System.Drawing;

namespace WinLibrary.Graphic
{
    public class SpriteRectangle : SpriteBase
    {
        public Size size;
        public float boundSize = 1;
        public bool isFill;

        public Rectangle rectangle => new Rectangle(displayPosition, size);

        public SpriteRectangle()
        {
        }

        public SpriteRectangle(SpriteBase parent) : base(parent)
        {
        }
    }
}
