using System.IO;

namespace Core
{
    public class ResourcePackage
    {
        public const string defaultName = "default";

        private const string dirName = "resource";
        private const string imageDirName = "image";

        private const string systemDirName = "system";
        private const string tileMapDirName = "tilemap";

        private readonly string packagePath;
        private readonly string packageFullPath;

        private readonly string imagePath;
        private readonly string imageFullPath;

        public string name { get; }

        public ResourcePackage(string name = defaultName)
        {
            this.name = name ??= defaultName;

            packagePath = $"{dirName}/{name}";
            packageFullPath = $"{Directory.GetCurrentDirectory()}/{packagePath}";

            imagePath = $"{packagePath}/{imageDirName}";
            imageFullPath = $"{packageFullPath}/{imageDirName}";
        }

        public string getSystemImageFilePath(string fileName) => $"{imagePath}/{systemDirName}/{fileName}";

        public string getSystemImageFileFullPath(string fileName) => $"{imageFullPath}/{systemDirName}/{fileName}";

        public string getTileMapImageFilePath(string fileName) => $"{imagePath}/{tileMapDirName}/{fileName}";

        public string getTileMapImageFileFullPath(string fileName) => $"{imageFullPath}/{tileMapDirName}/{fileName}";
    }
}
