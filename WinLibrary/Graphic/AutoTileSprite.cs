using System.Drawing;
using static WinLibrary.Graphic.AutoTileCalculator;

namespace WinLibrary.Graphic
{
    public class AutoTileSprite
    {
        private readonly int tileWidth;
        private readonly int tileHeight;
        private readonly int detailTileWidth;
        private readonly int detailTileHeight;

        public Sprite sprite1 = new Sprite();
        public Sprite sprite2 = new Sprite();
        public Sprite sprite3 = new Sprite();
        public Sprite sprite4 = new Sprite();

        public Image bitmap
        {
            get => sprite1.bitmap;
            set
            {
                sprite1.bitmap = value;
                sprite2.bitmap = value;
                sprite3.bitmap = value;
                sprite4.bitmap = value;
            }
        }

        public Point position
        {
            get => sprite1.position;
            set
            {
                sprite1.position = value;
                sprite2.position = new Point(value.X + detailTileWidth, value.Y);
                sprite3.position = new Point(value.X, value.Y + detailTileHeight);
                sprite4.position = new Point(value.X + detailTileWidth, value.Y + detailTileHeight);
            }
        }

        public Color color
        {
            get => sprite1.color;
            set
            {
                sprite1.color = value;
                sprite2.color = value;
                sprite3.color = value;
                sprite4.color = value;
            }
        }

        public AutoTileSprite(int tileWidth, int tileHeight)
        {
            this.tileWidth = tileWidth;
            this.tileHeight = tileHeight;

            detailTileWidth = tileWidth / 2;
            detailTileHeight = tileHeight / 2;
        }

        public void refresh(Image bitmap, Point p, byte different)
        {
            fillSprite(bitmap, p, sprite1, cTopLeft, different);
            fillSprite(bitmap, p, sprite2, cTopRight, different);
            fillSprite(bitmap, p, sprite3, cBottomLeft, different);
            fillSprite(bitmap, p, sprite4, cBottomRight, different);
        }

        private void fillSprite(Image bitmap, Point p, Sprite s, ICalculator c, byte different)
        {
            var (x, y) = c.calculate(p.X, p.Y, tileWidth, tileHeight, detailTileWidth, detailTileHeight, different);

            s.bitmap = bitmap;
            s.sourceRectangle = new Rectangle(x, y, detailTileWidth, detailTileHeight);
        }
    }
}
