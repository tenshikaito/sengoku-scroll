using System.Drawing;

namespace Library
{
    public class Camera
    {
        public int x;

        public int y;

        public int width;

        public int height;

        public Point center
        {
            get => new Point(x + width / 2, y + height / 2);
            set { x = value.X - width / 2; y = value.Y - height / 2; }
        }

        public Camera(int width, int height)
        {
            setSize(width, height);
        }

        public Camera setSize(int width, int height)
        {
            this.width = width;
            this.height = height;

            return this;
        }

        public Point translateWorldToScreen(Point p)
        {
            p.X -= x;
            p.Y -= y;
            return p;
        }

        public Point translateScreenToWorld(Point p)
        {
            p.X += x;
            p.Y += y;
            return p;
        }
    }
}
