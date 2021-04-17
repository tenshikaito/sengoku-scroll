using Core;
using Core.Helper;
using Game.Model;
using System.Collections.Generic;
using System.Drawing;
using WinLibrary;
using WinLibrary.Extension;

namespace Game
{
    public class FormMain : FormGame
    {
        private readonly Option option;
        private readonly GameWording wording;

        private GameSystem gameSystem;

        private List<PlayerInfo> players;

        protected override IFormGameOption formGameOption => option;

        public FormMain(Option option, GameWording wording)
        {
            this.option = option;
            this.wording = wording;

            initWindow();
            initSystem();
        }

        private void initWindow()
        {
            Width = option.screenWidth;
            Height = option.screenHeight;
            Text = option.title;
            Icon = new Icon("Icon.ico");
            MaximizeBox = false;

            this.setCenter();
        }

        private void initSystem()
        {
            players = PlayerHelper.loadPlayer<List<PlayerInfo>>();

            var sceneManager = new SceneManager();

            gameRoot = sceneManager;

            gameSystem = new GameSystem()
            {
                formMain = this,
                option = option,
                gameWording = wording,
                players = players,
                sceneManager = sceneManager,
                gameGraphic = gameGraphic
            };

            gameSystem.init();

            gameSystem.sceneToTitlePlayer();
        }
    }
}
