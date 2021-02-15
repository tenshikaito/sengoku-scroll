using System.IO;

namespace Library.Extension
{
    public static class FileExtension
    {
        public static string getFileFullName(this string filePath) => new FileInfo(filePath).Name;

        public static string getFileName(this string filePath)
            => Path.GetFileNameWithoutExtension(filePath.getFileFullName());

        public static string getFilePath(this string fileName, string filePath) => $"{filePath}/{fileName}";
    }
}
