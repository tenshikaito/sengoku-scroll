using Core;
using System.Drawing;
using WinLibrary.Module;

namespace Game
{
    public class ResourcePackageCache : ResourceCache
    {
        public const string defaultName = "default";

        private readonly ResourcePackage resourcePackage;

        public string name => resourcePackage.name;

        public ResourcePackageCache(string name = defaultName)
        {
            resourcePackage = new ResourcePackage(name);
        }

        public Image getSystemImage(string fileName)
            => getImage(resourcePackage.getSystemImageFileFullPath(fileName));

        public Image getTileMapImage(string fileName)
            => getImage(resourcePackage.getTileMapImageFileFullPath(fileName));
    }
}
