using Core.Helper;
using Game.UI.SceneTitle;
using System.Threading.Tasks;

namespace Game.Scene
{
    public class SceneTitleSelectGameWorld : SceneBase
    {
        private UISelectGameWorldDialog uiSelectGameWorldDialog;

        public SceneTitleSelectGameWorld(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            onSelectGameWorld();
        }

        public override void finish()
        {
            uiSelectGameWorldDialog?.Close();
        }

        private void onSelectGameWorld()
        {
            uiSelectGameWorldDialog = new UISelectGameWorldDialog(gameSystem)
            {
                Visible = true,
                okButtonClicked = onOkButtonClicked,
                cancelButtonClicked = () =>
                {
                    uiSelectGameWorldDialog.Close();
                    gameSystem.sceneToTitleMain();
                }
            };

            _ = loadGameWorldList();
        }

        private void onOkButtonClicked()
        {
            var gwi = uiSelectGameWorldDialog.selectedGameWorld;

            if (gwi == null) return;

            gameSystem.currentGameWorldName = gwi.name;
            gameSystem.currentGameWorldCode = gwi.code;

            gameSystem.sceneToTitleSelectGameScenario();
        }

        private async Task loadGameWorldList() => uiSelectGameWorldDialog.setData(await GameWorldHelper.getGameWorldInfoList());
    }
}
