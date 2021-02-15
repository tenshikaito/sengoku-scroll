using Library;

namespace Core.Model
{
    public class Religion : BaseObject
    {
        public int id;
        public string name;
        public bool isPolytheism;

        public class Sect
        {
            public int id;
            public string name;
            public int religionId;
            public int creatorId;
        }
    }
}
