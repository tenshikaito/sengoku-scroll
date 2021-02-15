using Library;
using Library.Model;

namespace Core.Model
{
    public class TileObject : BaseObject
    {
        public int id { get; set; }
        public MapPoint location;

        public bool isInvalid { get; set; }

        public static implicit operator MapPoint(TileObject to) => to.location;
    }
}
