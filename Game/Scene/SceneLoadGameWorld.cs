using Core;
using System.Drawing;
using System.Threading.Tasks;
using WinLibrary.Graphic;

namespace Game.Scene
{
    public class SceneLoadGameWorld : SceneWaiting
    {
        public SceneLoadGameWorld(GameSystem gs) : base(gs, gs.wording.loading)
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
