using System.Drawing;
using WinLibrary.Graphic;

namespace Game.Scene
{
    public class SceneWaiting : SceneBase
    {
        private readonly SpriteText spriteText = new SpriteText();

        public SceneWaiting(GameSystem gs, string text) : base(gs)
        {
            setText(text);
        }

        public void setText(string text)
        {
            var gs = gameSystem;
            var g = gs.gameGraphic;

            spriteText.text = text;
            spriteText.font = g.getDefaultFont();
            spriteText.fontSize = spriteText.font.Size;
            spriteText.color = Color.White;

            var size = gs.gameGraphic.measureDefaultText(spriteText);

            spriteText.position = new Point()
            {
                X = (gs.option.screenWidth - (int)size.Width) / 2,
                Y = gs.option.screenHeight - (int)size.Height / 2 - 100
            };
        }

        public override void draw()
        {
            gameSystem.gameGraphic.drawText(spriteText);
        }
    }
}
