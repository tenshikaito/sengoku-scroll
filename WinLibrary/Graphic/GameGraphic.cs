using System.Collections.Generic;
using System.Drawing;

namespace WinLibrary.Graphic
{
    public class GameGraphic
    {
        public Graphics g;

        public string defaultFontName;
        public float defaultFontSize;

        private readonly Dictionary<Color, SolidBrush> solidBrushMap = new Dictionary<Color, SolidBrush>();

        private readonly Dictionary<string, Pen> penMap = new Dictionary<string, Pen>();

        private readonly Dictionary<string, Font> fontMap = new Dictionary<string, Font>();

        public GameGraphic()
        {

        }

        private Pen getPen(Color c, float width)
        {
            var key = $"{c}_{width}";

            if (!penMap.TryGetValue(key, out var value)) penMap[key] = value = new Pen(c, width);

            return value;
        }

        private SolidBrush getSolidBrush(Color c)
        {
            if (!solidBrushMap.TryGetValue(c, out var value)) solidBrushMap[c] = value = new SolidBrush(c);

            return value;
        }

        private Font getFont(string fontName, float fontSize, FontStyle fs = FontStyle.Regular)
        {
            var key = $"{fontName}_{fontSize}_{fs}";

            if (!fontMap.TryGetValue(key, out var value)) fontMap[key] = value = new Font(fontName, fontSize, fs);

            return value;
        }

        public Font getDefaultFont() => getFont(defaultFontName, defaultFontSize);

        public void drawRectangle(SpriteRectangle s)
        {
            if (s.isFill) g.FillRectangle(getSolidBrush(s.color), new RectangleF(s.position, s.size));
            else g.DrawRectangle(getPen(s.color, s.boundSize), s.rectangle);
        }

        public SizeF measureDefaultText(SpriteText s) => g.MeasureString(s.text, s.font);

        public void drawText(SpriteText s) => g.DrawString(s.text, s.font, getSolidBrush(s.color), s.displayPosition);

        public void drawSprite(AutoTileSprite ts)
        {
            drawSprite(ts.sprite1);
            drawSprite(ts.sprite2);
            drawSprite(ts.sprite3);
            drawSprite(ts.sprite4);
        }

        public void drawSprite(Sprite s)
        {
            var sr = s.sourceRectangle ?? new Rectangle(0, 0, s.bitmap.Width, s.bitmap.Height);

            g.DrawImage(s.bitmap, new Rectangle(s.displayPosition, sr.Size), sr, GraphicsUnit.Pixel);
        }
    }
}
