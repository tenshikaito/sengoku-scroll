using System.IO;
using static Library.Helper.FileHelper;

namespace Core.Helper
{
    public static class OptionHelper
    {
        private static string optionPath { get; } = Directory.GetCurrentDirectory() + "/option.json";

        public static T loadOption<T>() => read<T>(optionPath);

        public static void saveOption(object o) => write(optionPath, o, true);

    }
}
