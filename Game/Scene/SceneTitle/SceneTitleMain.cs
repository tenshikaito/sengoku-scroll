﻿using Core.Code;
using Game.Scene.SceneTitle.SceneSinglePlayer;
using Game.UI;
using Game.UI.SceneTitle;

namespace Game.Scene
{
    public class SceneTitleMain : SceneBase
    {
        private UIMainMenuWindow uiMainMenuWindow;
        private UISelectModeWindow uiSelectModeWindow;

        public SceneTitleMain(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            uiMainMenuWindow = new UIMainMenuWindow(
                gameSystem,
                singlePlayerGame,
                multiplayerGame,
                editGameWorld,
                selectPlayer)
            {
                Text = gameSystem.currentPlayer.name,
                Visible = true
            };

            uiSelectModeWindow = new UISelectModeWindow(
                gameSystem,
                personalMode,
                publicMode)
            {
                cancelButtonClicked = () =>
                {
                    uiMainMenuWindow.Visible = true;
                    uiSelectModeWindow.Visible = false;
                },
                Visible = false
            };
        }

        public override void finish()
        {
            uiMainMenuWindow?.Close();
            uiSelectModeWindow?.Close();
        }

        private void singlePlayerGame()
        {
            uiMainMenuWindow.Visible = false;
            uiSelectModeWindow.Visible = true;
        }

        private void personalMode()
        {
            gameSystem.currentGameMode = GameMode.personal;

            uiSelectModeWindow.Visible = false;

            var dialog = new UIDialog(gameSystem, "message", "developing");

            dialog.okButtonClicked = () =>
            {
                uiSelectModeWindow.Visible = true;
                dialog.Close();
            };

            dialog.Show(uiSelectModeWindow);
        }

        private void publicMode()
        {
            gameSystem.currentGameMode = GameMode.@public;

            gameSystem.sceneManager.switchStatus(new SceneFreeModeGameWorld(gameSystem));
        }

        private void multiplayerGame()
        {
            gameSystem.currentGameMode = GameMode.@public;

            gameSystem.sceneToTitleMultiplayerGame();
        }

        private void editGameWorld()
        {
            gameSystem.sceneToTitleEditGameWorld();
        }

        private void selectPlayer()
        {
            gameSystem.sceneToTitlePlayer();
        }
    }
}
