using System;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneEditGameWorld
{
    public partial class UIEditGameWorldMenuWindow : UIWindow
    {
        public UIEditGameWorldMenuWindow(
            GameSystem gs,
            Action database,
            Action save,
            Action exit) : base(gs)
        {
            this.setCommandWindow(w.scene_title.edit_map).setAutoSizeF().setCenter();

            StartPosition = FormStartPosition.Manual;
            Location = gs.formMain.Location;

            var p = panel;

            new Button().init(w.scene_edit_game_world.database, database).setAutoSize().addTo(p);

            new Button().init(w.scene_edit_game_world.save, save).setAutoSize().addTo(p);

            new Button().init(w.scene_edit_game_world.exit, exit).setAutoSize().addTo(p);
        }
    }
}
