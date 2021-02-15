using Library;

namespace Core.Model
{
    public abstract class Building : TileObject
    {
        public int hp;
        public Status status = Status.building;

        public bool isBuilding => status == Status.building;

        public bool isClosing => status != Status.normal;

        public enum Status : byte
        {
            normal = 0,
            building = 1,
            broken = 2,
            /// <summary>被拆除</summary>
            destroying = 3
        }

        public class FunctionBuildingType : BaseObject
        {
            public int id;
            public string name;
            public TileFunctionType tileFunctionType;
            public int hp;
            public int buildSkill;
            /// <summary>建造花费</summary>
            public Supplies buildCost;
            /// <summary>维持费</summary>
            public Supplies keepCost;
            /// <summary>必要劳力才能正常工作</summary>
            public int labour;
            public string introduction;
        }
    }
}
