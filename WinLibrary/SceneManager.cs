namespace WinLibrary
{
    public class SceneManager : StateManager
    {
        public void switchStatus(Scene s) => base.switchStatus(s);

        public void pushStatus(Scene s) => base.pushStatus(s);
    }
}
