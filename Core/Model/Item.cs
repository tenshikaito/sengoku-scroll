using Library;

namespace Core.Model
{
    public class Item : Holding
    {
        public bool isInvalid { get; set; }
        public int type { get; set; }
        public Level level { get; set; }
        public string introduction { get; set; }

        public Ability.Type ability { get; set; }
        public int abilityValue;

        public class Type : BaseObject
        {
            public int id;
            public string name;
            public int culture;
            public bool isWeapon;
            public bool isArmor;
            public bool isMount;
            public bool canUse;
        }

        /// <summary>大类、比如武具艺术品等</summary>
        public class MarkType
        {
            public int id;
            public string name;
        }

        public enum Level : byte
        {
            S = 0,
            A = 1,
            B = 2,
            C = 3,
            D = 4,
            E = 5
        }
    }
}
