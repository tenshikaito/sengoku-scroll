using Core;
using Core.Code;
using Game.Model;
using Game.Scene;
using Library;
using System.Collections.Generic;
using System.Drawing;
using WinLibrary;
using WinLibrary.Graphic;

namespace Game
{
    public class GameSystem : IGameSystem<GameWording>
    {
        public FormMain formMain;

        public List<PlayerInfo> players;
        public PlayerInfo currentPlayer;

        public Option option;

        public GameGraphic gameGraphic;

        public Camera camera;

        public SceneManager sceneManager;

        public Dictionary<string, Bitmap> gameWorldThumbnailMap = new Dictionary<string, Bitmap>();

        public GameWorldInfo currentGameWorldInfo;
        public GameScenarioInfo currentGameScenarioInfo;

        public GameMode currentGameMode;
        public StartMode currentStartMode;
        public ScenarioMode currentScenarioMode;

        public GameWording wording { get; set; }

        public int screenWidth => option.screenWidth;
        public int screenHeight => option.screenHeight;

        public FormGame formGame => formMain;

        public void init()
        {
            camera = new Camera(screenWidth, screenHeight);

            formMain.Resize += (s, e) => camera.setSize(formMain.Width, formMain.Height);
        }

        public SceneTitlePlayer sceneToTitlePlayer()
        {
            var s = new SceneTitlePlayer(this);

            sceneManager.switchStatus(s);

            return s;
        }

        public SceneTitleMain sceneToTitleMain()
        {
            var s = new SceneTitleMain(this);

            sceneManager.switchStatus(s);

            return s;
        }

        public SceneTitleMultiplayerGame sceneToTitleMultiplayerGame()
        {
            var s = new SceneTitleMultiplayerGame(this);

            sceneManager.switchStatus(s);

            return s;
        }

        public SceneTitleEditGameWorld sceneToTitleEditGameWorld()
        {
            var s = new SceneTitleEditGameWorld(this);

            sceneManager.switchStatus(s);

            return s;
        }

        public SceneGame sceneToGame(GameWorldSystem gw)
        {
            var s = new SceneGame(this, gw);

            sceneManager.switchStatus(s);

            return s;
        }

        public SceneEditGameWorld sceneToEditGame(GameWorldSystem gw)
        {
            var s = new SceneEditGameWorld(this, gw);

            sceneManager.switchStatus(s);

            return s;
        }

        public SceneWaiting sceneToWaiting()
        {
            var s = new SceneWaiting(this, $"{wording.loading} ...");

            sceneManager.switchStatus(s);

            return s;
        }

        public void dispatchSceneToGame(GameWorldSystem gw) => formMain.dispatcher.invoke(() => sceneToGame(gw));

        public void dispatchSceneToEditGame(GameWorldSystem gw) => formMain.dispatcher.invoke(() => sceneToEditGame(gw));
    }
}
