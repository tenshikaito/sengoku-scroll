using System.Collections.Generic;

namespace WinLibrary
{
    public class StateManager : GameObject
    {
        private Stack<GameObject> stack = new Stack<GameObject>();
        private GameObject current;

        public void switchStatus(GameObject go)
        {
            children.Clear();

            current?.finish();

            addChild(current = go);

            go.start();
        }

        public void pushStatus(GameObject go)
        {
            children.Clear();

            if (current != null)
            {
                current.sleep();

                stack.Push(current);
            }

            addChild(current = go);

            current.start();
        }

        public void popStatus()
        {
            children.Clear();

            current?.finish();

            addChild(current = stack.Pop());

            current?.resume();
        }

        public void clearStatus()
        {
            while(stack.TryPop(out var go))
            {
                go.finish();
            }
        }
    }
}
