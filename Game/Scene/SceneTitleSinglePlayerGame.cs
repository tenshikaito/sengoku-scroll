using Core.Helper;
using Core.Network;
using Game.UI.SceneTitle;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Game.Scene
{
    public class SceneTitleSinglePlayerGame : SceneBase
    {
        private UISelectGameWorldDialog uiSelectGameWorldDialog;

        private UISelectGameScenarioDialog uiSelectGameScenarioDialog;

        private UIGameServerDetailDialog uiGameServerDetailDialog;

        private readonly ConcurrentDictionary<string, TestServerData> map = new ConcurrentDictionary<string, TestServerData>();

        public SceneTitleSinglePlayerGame(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            onSinglePlayerGame();
        }

        public override void finish()
        {
            uiSelectGameWorldDialog?.Close();
        }

        private void onSinglePlayerGame()
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
            var code = uiSelectGameWorldDialog.selectedValue;

            if (code == null) return;

        }

        private async Task loadGameWorldList() => uiSelectGameWorldDialog.setData(await GameWorldHelper.getGameWorldInfoList());
    }
}
