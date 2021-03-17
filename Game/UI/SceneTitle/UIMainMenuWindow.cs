using System;

namespace Game.UI.SceneTitle
{
    public class UIMainMenuWindow : UICommandWindow
    {
        public UIMainMenuWindow(
            GameSystem gs,
            Action singlePlayerGame,
            Action multiplayerGame,
            Action editMap,
            Action selectPlayer)
            : base(gs)
        {
            initCommandWindow(w.scene_title.start);

            addCommandButton(w.scene_title.select_game_world, singlePlayerGame);
            addCommandButton(w.scene_title.multiplayer_game, multiplayerGame);
            addCommandButton(w.scene_title.edit_map, editMap);
            addCommandButton(w.scene_title.select_player, selectPlayer);
        }
    }
}
