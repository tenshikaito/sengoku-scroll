using Core;
using Core.Code;
using Core.Data;
using Core.Helper;
using Game.Scene.SceneTitle.SceneStartGame;
using Game.UI;
using Game.UI.SceneTitle;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                selectPlayer,
                showDebugDialog)
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

            initDebugCommand();
        }

        public override void finish()
        {
            uiMainMenuWindow?.Close();
            uiSelectModeWindow?.Close();
        }

        private UIDebugWindow uiDebugWindow;
        private Dictionary<string, Func<string, Task>> commandMap = new Dictionary<string, Func<string, Task>>();

        private void initDebugCommand()
        {
            void add(string name, Func<string, Task> action) => commandMap[name] = action;

            add(nameof(test), test);
        }

        private void showDebugDialog()
        {
            uiDebugWindow = new UIDebugWindow(gameSystem, onDeubgCommandInput)
            {
                Visible = true,
            };

            uiDebugWindow.FormClosed += (s, e) => uiDebugWindow = null;
        }

        private async Task onDeubgCommandInput(string text)
        {
            appendCommandOutputText($"{DateTime.Now}>{text}");

            if (commandMap.TryGetValue(text, out var action)) await action(text);
            else appendCommandOutputText("no command found.");
        }

        private void appendCommandOutputText(string text)
        {
            uiDebugWindow?.appendOutputText(text);
        }

        private async Task test(string text)
        {
            await GameStageHelper.save("test", new GameStageInfo()
            {
                name = "test",
                width = 1000,
                height = 800,
                introduction = "introduction"
            }, new TileMap(new Library.Component.TileMap2D.Size(1000, 800)));

            appendCommandOutputText("completed");
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

            gameSystem.sceneManager.switchStatus(new ScenePublicModeGameStage(gameSystem));
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
