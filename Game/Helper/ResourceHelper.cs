namespace Game.Helper
{
    public static class ResourceHelper
    {
        private static ResourcePackageCache currentResourcePackageCache;

        public static ResourcePackageCache getResourcePackage(string name)
        {
            name ??= ResourcePackageCache.defaultName;

            if (currentResourcePackageCache?.name != name)
            {
                currentResourcePackageCache = new ResourcePackageCache(name);
            }

            return currentResourcePackageCache;
        }
    }
}
