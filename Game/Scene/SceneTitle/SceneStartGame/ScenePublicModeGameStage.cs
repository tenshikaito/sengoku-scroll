using Core.Helper;
using Game.UI.SceneTitle;
using System.Threading.Tasks;

namespace Game.Scene.SceneTitle.SceneStartGame
{
    public class ScenePublicModeGameStage : SceneBase
    {
        private UISelectGameStageDialog uiSelectGameStageDialog;

        public ScenePublicModeGameStage(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            onShow();
        }

        public override void sleep()
        {
            uiSelectGameStageDialog.Visible = false;
        }

        public override void resume()
        {
            uiSelectGameStageDialog.Visible = true;
        }

        public override void finish()
        {
            uiSelectGameStageDialog?.Close();
        }

        private void onShow()
        {
            uiSelectGameStageDialog = new UISelectGameStageDialog(gameSystem)
            {
                Visible = true,
                okButtonClicked = onOkButtonClicked,
                cancelButtonClicked = () =>
                {
                    gameSystem.sceneToTitleMain();
                }
            };

            _ = loadGameWorldList();
        }

        private void onOkButtonClicked()
        {
            var gsi = uiSelectGameStageDialog.selectedGameStage;

            if (gsi == null) return;

            gameSystem.currentStartMode = uiSelectGameStageDialog.checkedStartMode;
            gameSystem.currentGameStageInfo = gsi;

            gameSystem.sceneManager.pushStatus(new ScenePublicModeGameScenario(gameSystem));
        }

        private async Task loadGameWorldList() => uiSelectGameStageDialog.setData(await GameStageHelper.getInfoList());
    }
}
