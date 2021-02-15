using Core;
using Library;
using System.Drawing;

namespace Game.Helper
{
    public static class CommonHelper
    {
        public static void dragCamera(this Camera c, Point p)
        {
            c.x -= p.X;
            c.y -= p.Y;
        }

        public static string getSymbol(this bool value, GameWording w) => value ? w.symbol_selected : w.symbol_unselected;
    }
}
