namespace Core.Model
{
    public class Army : Unit
    {
        public bool isWaver;
        public bool isAttackUp;
        public bool isDefenseUp;
        public bool isAmbushed;

        public class Type
        {
            public int id;
            public string name;
            public int? culture;
            public int hp;
            public int ap;
            public int attack;
            public int rangedAttack;
            public int damage;
            public int defense;
        }
    }
}
