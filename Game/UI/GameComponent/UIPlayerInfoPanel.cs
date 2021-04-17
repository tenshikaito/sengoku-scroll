using Core.Model;
using System.Drawing;
using WinLibrary;
using WinLibrary.Graphic;

namespace Game.UI.GameComponent
{
    public class UIPlayerInfoPanel : GameObject
    {
        private readonly GameSystem gameSystem;
        private readonly GameWorldSystem gameWorld;

        private readonly SpriteRectangle background;
        private readonly SpriteText stName;
        private readonly SpriteText stForce;
        private readonly SpriteText stHp;

        private Character player => gameWorld.currentPlayer;

        public UIPlayerInfoPanel(GameSystem gs, GameWorldSystem gw, Point position)
        {
            gameSystem = gs;
            gameWorld = gw;

            var g = gs.gameGraphic;

            background = new SpriteRectangle()
            {
                position = position,
                color = Color.FromArgb(64, Color.Black),
                size = new Size(450, 36),
                isFill = true
            };

            stName = new SpriteText()
            {
                position = new Point(position.X + 16, position.Y + 8),
                color = Color.White,
                font = g.getDefaultFont(),
                fontSize = g.defaultFontSize,
                text = nameof(stName)
            };

            stForce = new SpriteText()
            {
                position = new Point(position.X + 144, position.Y + 8),
                color = Color.White,
                font = g.getDefaultFont(),
                fontSize = g.defaultFontSize,
                text = nameof(stForce)
            };

            stHp = new SpriteText()
            {
                position = new Point(position.X + 272, position.Y + 8),
                color = Color.White,
                font = g.getDefaultFont(),
                fontSize = g.defaultFontSize,
                text = nameof(stHp)
            };
        }

        public override void update()
        {
            stName.text = player.name;
            stForce.text = player.force.ToString();
            stHp.text = player.hp.ToString();
        }

        public override void draw()
        {
            var g = gameSystem.gameGraphic;

            g.drawRectangle(background);
            g.drawText(stName);
            g.drawText(stForce);
            g.drawText(stHp);
        }
    }
}
