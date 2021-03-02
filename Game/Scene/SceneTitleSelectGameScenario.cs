using Core.Helper;
using Core.Network;
using Game.UI.SceneTitle;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Game.Scene
{
    public class SceneTitleSelectGameScenario : SceneBase
    {
        private UISelectGameScenarioDialog uiSelectGameScenarioDialog;

        public SceneTitleSelectGameScenario(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            onSelectGameScenario();
        }

        public override void finish()
        {
            uiSelectGameScenarioDialog?.Close();
        }

        private void onSelectGameScenario()
        {
            uiSelectGameScenarioDialog = new UISelectGameScenarioDialog(gameSystem)
            {
                Visible = true,
                okButtonClicked = onOkButtonClicked,
                cancelButtonClicked = () =>
                {
                    uiSelectGameScenarioDialog.Close();
                    gameSystem.sceneToTitleSelectGameWorld();
                }
            };

            _ = loadGameWorldList();
        }

        private void onOkButtonClicked()
        {
            var scenarioInfo = uiSelectGameScenarioDialog.selectedGameScenario;

            if (scenarioInfo == null) return;

            gameSystem.currentGameScenarioInfo = scenarioInfo;

            gameSystem.sceneManager.switchStatus(new SceneLoadGameWorld(gameSystem));
        }

        private async Task loadGameWorldList() => uiSelectGameScenarioDialog.setData(await GameWorldHelper.getGameScenarioInfoList());
    }
}
