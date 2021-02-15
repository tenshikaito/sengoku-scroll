using System.IO;
using System.Threading.Tasks;
using static Library.Helper.FileHelper;

namespace Core.Helper
{
    public static class OptionHelper
    {
        private static string optionPath { get; } = Directory.GetCurrentDirectory() + "/option.json";

        public static async Task<T> loadOption<T>() => await read<T>(optionPath);

        public static async Task saveOption(object o) => await write(optionPath, o, true);

    }
}
