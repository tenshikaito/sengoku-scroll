using System.Drawing;

namespace WinLibrary.Graphic
{
    public class Sprite : SpriteBase
    {
        public Image bitmap { get; set; }

        public Rectangle? sourceRectangle { get; set; }

        public Sprite()
        {

        }
    }
}
