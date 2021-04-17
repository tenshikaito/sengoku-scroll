using System.IO;
using static Library.Helper.FileHelper;

namespace Core.Helper
{
    public static class PlayerHelper
    {
        private static string playerPath { get; } = Directory.GetCurrentDirectory() + "/player.dat";

        public static T loadPlayer<T>() => read<T>(playerPath);

        public static void savePlayer(object o) => write(playerPath, o, true);
    }
}
