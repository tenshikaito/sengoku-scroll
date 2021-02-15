namespace Library.Model
{
    public struct MapPoint
    {
        public int x;
        public int y;

        public MapPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString() => $"x:{x}, y:{y}";

        public override bool Equals(object obj) => base.Equals(obj);

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(MapPoint p1, MapPoint p2) => p1.Equals(p2);

        public static bool operator !=(MapPoint p1, MapPoint p2) => !p1.Equals(p2);
    }
}
