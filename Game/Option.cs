using WinLibrary;

namespace Game
{
    public class Option : IFormGameOption
    {
        public int screenWidth { get; set; } = 1366;

        public int screenHeight { get; set; } = 768;

        public string title { get; set; } = "SengokuScroll";

        public int port { get; set; } = 7789;
    }
}
