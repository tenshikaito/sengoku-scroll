using Library.Extension;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Library.Helper
{
    public static class FileHelper
    {
        public static readonly Encoding encoding = EncodingHelper.currentEncoding;

        public static async Task<string[]> readLines(string path)
            => await File.ReadAllLinesAsync(path, encoding);

        public static async Task<string> read(string path)
            => await File.ReadAllTextAsync(path, encoding);

        public static async Task<T> read<T>(string path)
            => (await read(path)).fromJson<T>();

        public static async Task write(string path, object o, bool isIndented = false)
            => await File.WriteAllTextAsync(path, o.toJson(isIndented), encoding);
    }
}
