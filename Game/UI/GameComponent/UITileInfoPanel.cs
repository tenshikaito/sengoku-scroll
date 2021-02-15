using System.Drawing;
using WinLibrary;
using WinLibrary.Graphic;

namespace Game.UI.GameComponent
{
    public class UITileInfoPanel : GameObject
    {
        private readonly GameSystem gameSystem;

        private readonly SpriteRectangle background;
        private readonly SpriteText tileName1;
        private readonly SpriteText tileName2;
        private readonly SpriteText tileName3;

        public UITileInfoPanel(GameSystem gs, Point position)
        {
            gameSystem = gs;

            var g = gs.gameGraphic;

            background = new SpriteRectangle()
            {
                position = position,
                color = Color.FromArgb(64, Color.Black),
                size = new Size(450, 36),
                isFill = true
            };

            tileName1 = new SpriteText()
            {
                position = new Point(position.X + 16, position.Y + 8),
                color = Color.White,
                font = g.getDefaultFont(),
                fontSize = g.defaultFontSize,
                text = nameof(tileName1)
            };

            tileName2 = new SpriteText()
            {
                position = new Point(position.X + 144, position.Y + 8),
                color = Color.White,
                font = g.getDefaultFont(),
                fontSize = g.defaultFontSize,
                text = nameof(tileName2)
            };

            tileName3 = new SpriteText()
            {
                position = new Point(position.X + 272, position.Y + 8),
                color = Color.White,
                font = g.getDefaultFont(),
                fontSize = g.defaultFontSize,
                text = nameof(tileName3)
            };
        }

        public void setText(string tileName1, string tileName2, string tileName3)
        {
            this.tileName1.text = tileName1 ?? string.Empty;
            this.tileName2.text = tileName2 ?? string.Empty;
            this.tileName3.text = tileName3 ?? string.Empty;
        }

        public override void draw()
        {
            var g = gameSystem.gameGraphic;

            g.drawRectangle(background);
            g.drawText(tileName1);
            g.drawText(tileName2);
            g.drawText(tileName3);
        }
    }
}
