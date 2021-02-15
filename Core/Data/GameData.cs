using Core.Model;
using Library;

namespace Core.Data
{
    public class GameData
    {
        public IncreasedIdDictionary<Force> force;
        public IncreasedIdDictionary<Province> province;
        public IncreasedIdDictionary<Stronghold> stronghold;

        public IncreasedIdDictionary<Player> player;
    }
}
