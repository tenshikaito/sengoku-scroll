using System.IO;
using System.Threading.Tasks;
using static Library.Helper.FileHelper;

namespace Core.Helper
{
    public static class PlayerHelper
    {
        private static string playerPath { get; } = Directory.GetCurrentDirectory() + "/player.dat";

        public static async Task<T> loadPlayer<T>() => await read<T>(playerPath);

        public static async Task savePlayer(object o) => await write(playerPath, o, true);
    }
}
