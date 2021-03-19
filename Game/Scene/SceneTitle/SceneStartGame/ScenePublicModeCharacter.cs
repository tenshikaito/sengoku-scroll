using Game.UI.SceneTitle;
using System.Threading.Tasks;

namespace Game.Scene.SceneTitle.SceneStartGame
{
    public class ScenePublicModeCharacter : SceneBase
    {
        private UISetCharacterDialog uiSetCharacterDialog;

        public ScenePublicModeCharacter(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            onShow();
        }

        public override void sleep()
        {
            uiSetCharacterDialog.Visible = false;
        }

        public override void resume()
        {
            uiSetCharacterDialog.Visible = true;
        }

        public override void finish()
        {
            uiSetCharacterDialog?.Close();
        }

        private void onShow()
        {
            uiSetCharacterDialog = new UISetCharacterDialog(gameSystem)
            {
                Visible = true,
                okButtonClicked = onOkButtonClicked,
                cancelButtonClicked = () =>
                {
                    gameSystem.sceneManager.popStatus();
                }
            };

            _ = loadGameMasterData();
        }

        private void onOkButtonClicked()
        {

        }

        private async Task loadGameMasterData()
        {
        }
    }
}
