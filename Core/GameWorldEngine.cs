using Core.Helper;
using System.Threading.Tasks;

namespace Core
{
    public class GameWorldEngine
    {
        public GameWorldEngine()
        {
        }

        public async Task load(string gameWorldFileName, string gameScenarioFile)
        {
            var gameWorld = await GameWorldHelper.loadGameWorldData<GameWorld>(gameWorldFileName);

            var gameScenario = await GameScenarioHelper.loadGameScenarioData<GameScenario>(gameScenarioFile);
        }
    }
}
