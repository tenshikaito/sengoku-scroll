﻿using Game.UI.SceneTitle;
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
