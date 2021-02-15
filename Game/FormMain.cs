using Core;
using Game.Model;
using System.Collections.Generic;
using System.Drawing;
using WinLibrary;
using WinLibrary.Extension;

namespace Game
{
    public class FormMain : FormGame
    {
        private GameSystem gameSystem;

        private readonly Option option;
        private readonly GameWording wording;
        private readonly List<PlayerInfo> players;

        protected override IFormGameOption formGameOption => option;

        public FormMain(Option option, GameWording wording, List<PlayerInfo> players)
        {
            this.option = option;
            this.wording = wording;
            this.players = players;

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
            gameSystem = new GameSystem()
            {
                formMain = this,
                option = option,
                wording = wording,
                players = players,
                sceneManager = gameRoot,
                gameGraphic = gameGraphic
            };

            gameSystem.init();

            gameSystem.sceneToTitlePlayer();
        }
    }
}
