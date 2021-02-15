using Game.UI.SceneTitle;

namespace Game.Scene
{
    public class SceneTitleMain : SceneBase
    {
        private UIMainMenuWindow uiMainMenuWindow;

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
        }

        public override void finish()
        {
            uiMainMenuWindow?.Close();
        }

        private void singlePlayerGame()
        {
            gameSystem.sceneToTitleSinglePlayerGame();
        }

        private void multiplayerGame()
        {
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
