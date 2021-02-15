using System;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UIMainMenuWindow : UIWindow
    {
        public UIMainMenuWindow(
            GameSystem gs,
            Action singlePlayerGame,
            Action multiplayerGame,
            Action editMap,
            Action selectPlayer) : base(gs)
        {
            this.setCommandWindow(w.scene_title.start).setAutoSizeF().setCenter();

            new Button().init(w.scene_title.single_player_game, singlePlayerGame).setAutoSize().addTo(panel);

            new Button().init(w.scene_title.multiplayer_game, multiplayerGame).setAutoSize().addTo(panel);

            new Button().init(w.scene_title.edit_map, editMap).setAutoSize().addTo(panel);

            new Button().init(w.scene_title.select_player, selectPlayer).setAutoSize().addTo(panel);
        }
    }
}
