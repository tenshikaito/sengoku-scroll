using Core.Helper;
using Game.UI.SceneTitle;
using System.Threading.Tasks;

namespace Game.Scene.SceneTitle.SceneStartGame
{
    public class ScenePublicModeGameScenario : SceneBase
    {
        private UISelectGameScenarioDialog uiSelectGameScenarioDialog;

        public ScenePublicModeGameScenario(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            onShow();
        }

        public override void sleep()
        {
            uiSelectGameScenarioDialog.Visible = false;
        }

        public override void resume()
        {
            uiSelectGameScenarioDialog.Visible = true;
        }

        public override void finish()
        {
            uiSelectGameScenarioDialog?.Close();
        }

        private void onShow()
        {
            uiSelectGameScenarioDialog = new UISelectGameScenarioDialog(gameSystem)
            {
                Visible = true,
                okButtonClicked = onOkButtonClicked,
                cancelButtonClicked = () => gameSystem.sceneManager.popStatus()
            };

            _ = loadGameScenarioList();
        }

        private void onOkButtonClicked()
        {
            var scenarioInfo = uiSelectGameScenarioDialog.selectedGameScenario;

            gameSystem.currentGameScenarioInfo = scenarioInfo;

            gameSystem.sceneManager.pushStatus(new ScenePublicModeCharacter(gameSystem));
        }

        private async Task loadGameScenarioList() => uiSelectGameScenarioDialog.setData(await GameWorldHelper.getGameScenarioInfoList());
    }
}
