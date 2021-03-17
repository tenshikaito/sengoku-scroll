using Core.Helper;
using Game.UI.SceneTitle;
using System.Threading.Tasks;

namespace Game.Scene.SceneTitle.SceneSinglePlayer
{
    public class SceneFreeModeGameWorld : SceneBase
    {
        private UISelectGameWorldDialog uiSelectGameWorldDialog;

        public SceneFreeModeGameWorld(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            onShow();
        }

        public override void sleep()
        {
            uiSelectGameWorldDialog.Visible = false;
        }

        public override void resume()
        {
            uiSelectGameWorldDialog.Visible = true;
        }

        public override void finish()
        {
            uiSelectGameWorldDialog?.Close();
        }

        private void onShow()
        {
            uiSelectGameWorldDialog =  new UISelectGameWorldDialog(gameSystem)
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
            var gwi = uiSelectGameWorldDialog.selectedGameWorld;

            if (gwi == null) return;

            gameSystem.currentStartMode = uiSelectGameWorldDialog.checkedStartMode;
            gameSystem.currentGameWorldInfo = gwi;

            gameSystem.sceneManager.pushStatus(new SceneFreeModeGameScenario(gameSystem));
        }

        private async Task loadGameWorldList() => uiSelectGameWorldDialog.setData(await GameWorldHelper.getInfoList());
    }
}
