using System.Drawing;

namespace WinLibrary.Graphic
{
    public class SpriteText : SpriteBase
    {
        public string text;

        public Font font;

        public float fontSize;

        public SpriteText()
        {
        }

        public SpriteText(SpriteBase parent) : base(parent)
        {
        }
    }
}
