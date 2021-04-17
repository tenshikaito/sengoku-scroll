using Library.Extension;
using System.IO;
using System.Threading.Tasks;
using static Library.Helper.EncodingHelper;

namespace Library.Helper
{
    public static class FileHelper
    {
        public static string[] readLines(string path)
            => File.ReadAllLines(path, currentEncoding);

        public static string read(string path)
            => File.ReadAllText(path, currentEncoding);

        public static T read<T>(string path)
            => read(path).fromJson<T>();

        public static void write(string path, object o, bool isIndented = false)
            => File.WriteAllText(path, o.toJson(isIndented), currentEncoding);

        public static async Task<string[]> readLinesAsync(string path)
            => await File.ReadAllLinesAsync(path, currentEncoding);

        public static async Task<string> readAsync(string path)
            => await File.ReadAllTextAsync(path, currentEncoding);

        public static async Task<T> readAsync<T>(string path)
            => (await readAsync(path)).fromJson<T>();

        public static async Task writeAsync(string path, object o, bool isIndented = false)
            => await File.WriteAllTextAsync(path, o.toJson(isIndented), currentEncoding);
    }
}
