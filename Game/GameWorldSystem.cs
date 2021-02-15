using Core;
using Core.Model;
using Library;

namespace Game
{
    public class GameWorldSystem : GameWorld
    {
        public Player currentPlayer;

        public Camera camera;

        public GameWorldSystem(string name) : base(name)
        {
        }

        //public void init(GameWorld gwd)
        //{
        //    gameWorldData = gwd;

        //    gameResourceManager = new GameResourceManager(gwd.resourcePackageName);
        //}
    }
}
