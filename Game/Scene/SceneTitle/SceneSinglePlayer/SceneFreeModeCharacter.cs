using Core;
using Core.Helper;
using Game.UI.SceneTitle;
using Library.Helper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Scene.SceneTitle.SceneSinglePlayer
{
    public class SceneFreeModeCharacter : SceneBase
    {
        private UISetCharacterDialog uISetCharacterDialog;

        public SceneFreeModeCharacter(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            onShow();
        }

        public override void sleep()
        {
            uISetCharacterDialog.Visible = false;
        }

        public override void resume()
        {
            uISetCharacterDialog.Visible = true;
        }

        public override void finish()
        {
            uISetCharacterDialog?.Close();
        }

        private void onShow()
        {
            uISetCharacterDialog = new UISetCharacterDialog(gameSystem)
            {
                Visible = true,
                okButtonClicked = onOkButtonClicked,
                cancelButtonClicked = () =>
                {
                    gameSystem.sceneManager.popStatus();
                }
            };

            _ = loadGameScenarioList();
        }

        private void onOkButtonClicked()
        {

        }

        private async Task loadGameScenarioList()
        {
            var data = await GameWorldHelper.getGameScenarioInfoList();

            var list = new List<GameScenarioInfo>()
            {
                new GameScenarioInfo()
            };

            list.AddRange(data);

            uISetCharacterDialog.setData(list);
        }
    }
}
