using Library;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinLibrary
{
    public class GameObject : TreeObject<GameObject>, IGameStatus, IDisposable
    {
        public string name;

        public bool isEnable = true;

        public int order;

        public void addChild(GameObject go)
        {
            children.Add(go);
            sort();
        }

        public void sort() => children.Sort((o1, o2) => o1.order - o2.order);

        public virtual void start()
        {
        }

        public virtual void update()
        {
        }

        public virtual void draw()
        {
        }

        public virtual void sleep()
        {
        }

        public virtual void resume()
        {
        }

        public virtual void finish()
        {
        }

        public void onUpdate()
        {
            if (!isEnable) return;

            update();

            children.ForEach(o => o.onUpdate());
        }

        public void onDraw()
        {
            if (!isEnable) return;

            draw();

            children.ForEach(o => o.onDraw());
        }

        public void onMouseMoved(MouseEventArgs e)
        {
            if (!isEnable) return;

            mouseMoved(e);

            children.ForEach(o => o.onMouseMoved(e));
        }

        public void onMouseDragging(MouseEventArgs e, Point p)
        {
            if (!isEnable) return;

            mouseDragging(e, p);

            children.ForEach(o => o.onMouseDragging(e, p));
        }

        public void onMouseEntered(MouseEventArgs e)
        {
            if (!isEnable) return;

            mouseEntered(e);

            children.ForEach(o => o.onMouseEntered(e));
        }

        public void onMouseExited(MouseEventArgs e)
        {
            if (!isEnable) return;

            mouseExited(e);

            children.ForEach(o => o.onMouseExited(e));
        }

        public void onMouseClicked(MouseEventArgs e)
        {
            if (!isEnable) return;

            mouseClicked(e);

            children.ForEach(o => o.onMouseClicked(e));
        }

        public void onMousePressed(MouseEventArgs e)
        {
            if (!isEnable) return;

            mousePressed(e);

            children.ForEach(o => o.onMousePressed(e));
        }

        public void onMouseReleased(MouseEventArgs e)
        {
            if (!isEnable) return;

            mouseReleased(e);

            children.ForEach(o => o.onMouseReleased(e));
        }

        public void onMouseWheelScrolled(MouseEventArgs e)
        {
            if (!isEnable) return;

            mouseWheelScrolled(e);

            children.ForEach(o => o.onMouseWheelScrolled(e));
        }

        public void onKeyPressed(KeyPressEventArgs e)
        {
            if (!isEnable) return;

            keyPressed(e);

            children.ForEach(o => o.onKeyPressed(e));
        }

        public void onKeyPressing(KeyEventArgs e)
        {
            if (!isEnable) return;

            keyPressing(e);

            children.ForEach(o => o.onKeyPressing(e));
        }

        public void onKeyReleased(KeyEventArgs e)
        {
            if (!isEnable) return;

            keyReleased(e);

            children.ForEach(o => o.onKeyReleased(e));
        }

        public virtual void mouseMoved(MouseEventArgs e)
        {
        }

        public virtual void mouseDragging(MouseEventArgs e, Point p)
        {
        }

        public virtual void mouseEntered(MouseEventArgs e)
        {
        }

        public virtual void mouseExited(MouseEventArgs e)
        {
        }

        public virtual void mouseClicked(MouseEventArgs e)
        {
        }

        public virtual void mousePressed(MouseEventArgs e)
        {
        }

        public virtual void mouseReleased(MouseEventArgs e)
        {
        }

        public virtual void mouseWheelScrolled(MouseEventArgs e)
        {
        }

        public virtual void keyPressed(KeyPressEventArgs e)
        {
        }

        public virtual void keyPressing(KeyEventArgs e)
        {
        }

        public virtual void keyReleased(KeyEventArgs e)
        {
        }
    }
}
