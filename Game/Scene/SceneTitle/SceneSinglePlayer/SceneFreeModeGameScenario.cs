using Core;
using Core.Helper;
using Game.UI.SceneTitle;
using Library.Helper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Scene.SceneTitle.SceneSinglePlayer
{
    public class SceneFreeModeGameScenario : SceneBase
    {
        private UISelectGameScenarioDialog uiSelectGameScenarioDialog;

        public SceneFreeModeGameScenario(GameSystem gs) : base(gs)
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

            if (scenarioInfo == null) return;

            gameSystem.currentGameScenarioInfo = scenarioInfo;

            gameSystem.sceneManager.pushStatus(new SceneFreeModeCharacter(gameSystem));
        }

        private async Task loadGameScenarioList()
        {
            var data = await GameWorldHelper.getGameScenarioInfoList();

            var list = new List<GameScenarioInfo>()
            {
                new GameScenarioInfo()
            };

            list.AddRange(data);

            uiSelectGameScenarioDialog.setData(list);
        }
    }
}
