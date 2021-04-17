using Library.Extension;
using Library.Helper;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Library.Module
{
    public class FileProcessor
    {
        public string directoryName { get; }

        public string fileExtension { get; }

        public string directoryPath => $"{Directory.GetCurrentDirectory()}/{directoryName}";

        public FileProcessor(string directoryName, string fileExtension)
        {
            this.directoryName = directoryName;
            this.fileExtension = fileExtension;
        }

        public string getFilePath(string fileName)
            => $"{fileName}.{fileExtension}".getFilePath(directoryPath);

        public bool isExist(string fileName) => File.Exists(getFilePath(fileName));

        public DirectoryInfo createDirectory() => Directory.CreateDirectory(directoryPath);

        public IEnumerable<string> enumerateFiles()
            => Directory.EnumerateFiles(
                directoryPath,
                string.IsNullOrEmpty(fileExtension) ? "*" : $"*.{fileExtension}");

        public async Task write(string fileName, object o)
            => await FileHelper.writeAsync(getFilePath(fileName), o);

        public async Task<T> read<T>(string fileName)
            => await FileHelper.readAsync<T>(getFilePath(fileName));

        public void delete(string name)
            => File.Delete(getFilePath(name));
    }
}
