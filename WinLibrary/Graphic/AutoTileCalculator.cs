namespace WinLibrary.Graphic
{
    public static class AutoTileCalculator
    {
        public const byte left = 1;
        public const byte right = left * 2;
        public const byte top = right * 2;
        public const byte bottom = top * 2;
        public const byte topLeft = bottom * 2;
        public const byte topRight = topLeft * 2;
        public const byte bottomLeft = topRight * 2;
        public const byte bottomRight = bottomLeft * 2;

        public static readonly ICalculator cTopLeft = new TopLeftCalculator();
        public static readonly ICalculator cTopRight = new TopRightCalculator();
        public static readonly ICalculator cBottomLeft = new BottomLeftCalculator();
        public static readonly ICalculator cBottomRight = new BottomRightCalculator();

        public abstract class ICalculator
        {
            public abstract (int x, int y) calculate(
                int vertexX, int vertexY, int tileWidth, int tileHeight, int width, int height, byte different);
        }

        protected class TopLeftCalculator : ICalculator
        {
            public override (int x, int y) calculate(
                int vertexX, int vertexY, int tileWidth, int tileHeight, int width, int height, byte different)
            {
                var x = vertexX;
                var y = vertexY + tileHeight;
                if ((different & (left | top)) == (left | top))
                {
                }
                else if ((different & left) == left)
                {
                    y += tileHeight;
                }
                else if ((different & top) == top)
                {
                    x += tileWidth;
                }
                else if ((different & topLeft) == topLeft)
                {
                    x = vertexX + tileWidth;
                    y = vertexY;
                }
                else
                {
                    x += tileWidth;
                    y += tileHeight;
                }

                return (x, y);
            }
        }

        protected class TopRightCalculator : ICalculator
        {
            public override (int x, int y) calculate(
                int vertexX, int vertexY, int tileWidth, int tileHeight, int width, int height, byte different)
            {
                var x = vertexX;
                var y = vertexY + tileHeight;
                if ((different & (top | right)) == (top | right))
                {
                    x += tileWidth + width;
                }
                else if ((different & right) == right)
                {
                    x += tileWidth + width;
                    y += tileHeight;
                }
                else if ((different & top) == top)
                {
                    x += width;
                }
                else if ((different & topRight) == topRight)
                {
                    x = vertexX + tileWidth + width;
                    y = vertexY;
                }
                else
                {
                    x += width;
                    y += tileHeight;
                }

                return (x, y);
            }
        }

        protected class BottomLeftCalculator : ICalculator
        {
            public override (int x, int y) calculate(
                int vertexX, int vertexY, int tileWidth, int tileHeight, int width, int height, byte different)
            {
                var x = vertexX;
                var y = vertexY + tileHeight;
                if ((different & (bottom | left)) == (bottom | left))
                {
                    y += tileHeight + height;
                }
                else if ((different & left) == left)
                {
                    y += height;
                }
                else if ((different & bottom) == bottom)
                {
                    x += tileWidth;
                    y += tileHeight + height;
                }
                else if ((different & bottomLeft) == bottomLeft)
                {
                    x = vertexX + tileWidth;
                    y = vertexY + height;
                }
                else
                {
                    x += tileWidth;
                    y += height;
                }

                return (x, y);
            }
        }

        protected class BottomRightCalculator : ICalculator
        {
            public override (int x, int y) calculate(
                int vertexX, int vertexY, int tileWidth, int tileHeight, int width, int height, byte different)
            {
                var x = vertexX;
                var y = vertexY + tileHeight;
                if ((different & (bottom | right)) == (bottom | right))
                {
                    x += tileWidth + width;
                    y += tileHeight + height;
                }
                else if ((different & right) == right)
                {
                    x += tileWidth + width;
                    y += height;
                }
                else if ((different & bottom) == bottom)
                {
                    x += width;
                    y += tileHeight + height;
                }
                else if ((different & bottomRight) == bottomRight)
                {
                    x = vertexX + tileWidth + width;
                    y = vertexY + height;
                }
                else
                {
                    x += width;
                    y += height;
                }

                return (x, y);
            }
        }
    }
}
