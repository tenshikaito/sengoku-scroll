using System;

namespace Game.UI.SceneTitle
{
    public class UISelectModeWindow : UICommandWindow
    {
        public UISelectModeWindow(
            GameSystem gs,
            Action personalMode,
            Action publicMode) : base(gs)
        {
            initCommandWindow(w.scene_title.select_mode);

            addCommandButton(w.scene_title.personal_mode, personalMode);
            addCommandButton(w.scene_title.public_mode, publicMode);

            addCancelButton();
        }
    }
}
