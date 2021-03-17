using Core;
using System.Threading.Tasks;

namespace Game.Scene
{
    public class SceneLoadGameWorld : SceneWaiting
    {
        public SceneLoadGameWorld(GameSystem gs) : base(gs, gs.gameWording.loading)
        {
        }

        public override void start()
        {
            Task.Run(() =>
            {
                var gameWorldInfo = gameSystem.currentGameWorldInfo;
                var gameScenarioInfo = gameSystem.currentGameScenarioInfo;

                var gameEngine = new GameWorldEngine();
            });
        }
    }
}
