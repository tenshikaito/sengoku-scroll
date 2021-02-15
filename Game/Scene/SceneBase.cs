using System;
using WinLibrary;

namespace Game.Scene
{
    public abstract class SceneBase : GameObject
    {
        protected GameSystem gameSystem;

        protected FormMain formMain => gameSystem.formMain;

        protected FormGame.Dispatcher dispatcher => formMain.dispatcher;

        public SceneBase(GameSystem gs) => gameSystem = gs;

        protected void Invoke(Action a) => formMain.dispatcher.invoke(a);
    }
}
