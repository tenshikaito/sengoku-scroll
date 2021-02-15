using System.Drawing;
using System.Windows.Forms;

namespace WinLibrary
{
    public interface IInputEventListener
    {
        void mouseMoved(MouseEventArgs e);

        void mouseDragging(MouseEventArgs e, Point p);

        void mouseEntered(MouseEventArgs e);

        void mouseExited(MouseEventArgs e);

        void mouseClicked(MouseEventArgs e);

        void mousePressed(MouseEventArgs e);

        void mouseReleased(MouseEventArgs e);

        void mouseWheelScrolled(MouseEventArgs e);

        void keyPressed(KeyPressEventArgs e);

        void onKeyPressing(KeyEventArgs e);

        void keyReleased(KeyEventArgs e);
    }
}
