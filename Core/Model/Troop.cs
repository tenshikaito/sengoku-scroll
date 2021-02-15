namespace Core.Model
{
    public class Troop  : Unit
    {
        public Type type;

        public enum Type : byte
        {
            Army = 0,
            Labour = 1,
            Transporter = 2,
            Sattler = 3
        }
    }
}
