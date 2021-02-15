using Game.Helper;

namespace Game.Extension
{
    public static class GameExtension
    {
        public static ResourcePackageCache getResourceCache(this GameWorldSystem gw)
            => ResourceHelper.getResourcePackage(gw.resourcePackageName);
    }
}
