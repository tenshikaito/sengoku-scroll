using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneGame
{
    public partial class UIGameWorldCommandWindow : UIWindow
    {
        public UIGameWorldCommandWindow(
            GameSystem gs,
            Action pointer,
            Action move) : base(gs)
        {
            this.setCommandWindow(w.main_tile_map).setAutoSizeF().setCenter();

            StartPosition = FormStartPosition.Manual;
            Location = new Point(gs.formMain.Location.X, gs.formMain.Location.Y + 240);

            var list = new List<CheckBox>();
            var p = new FlowLayoutPanel().init(FlowDirection.LeftToRight).setAutoSizeP().addTo(panel);

            var cbPointer = new CheckBox();

            list.Add(cbPointer.init(w.pointer, true, () =>
            {
                list.ForEach(o => o.Checked = false);
                cbPointer.Checked = true;
                pointer();
            }).setButtonStyle().setAutoSize().addTo(p));

            var cbMove = new CheckBox();

            list.Add(cbMove.init(w.scene_game.move, false, () =>
            {
                list.ForEach(o => o.Checked = false);
                cbMove.Checked = true;
                move();
            }).setButtonStyle().setAutoSize().addTo(p));
        }
    }
}
