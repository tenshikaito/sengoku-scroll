using System;

namespace Game.UI.SceneTitle
{
    public class UISelectModeWindow : UICommandWindow
    {
        public UISelectModeWindow(
            GameSystem gs,
            Action storyMode,
            Action freeMode) : base(gs)
        {
            initCommandWindow(w.scene_title.select_mode);

            addCommandButton(w.scene_title.story_mode, storyMode);
            addCommandButton(w.scene_title.free_mode, freeMode);

            addCancelButton();
        }
    }
}
