using Library.Extension;
using Library.Module;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Helper
{
    public static class GameWorldHelper
    {
        private static readonly FileProcessor fileProcessor
            = new FileProcessor("/gameworld", "sgw");

        public static async Task<GameWorldInfo[]> getGameWorldInfoList()
        {
            fileProcessor.createDirectory();

            var filePathList = fileProcessor.enumerateFiles();

            var result = await Task.WhenAll(filePathList.Select(async o =>
            {
                var fileName = o.getFileName();
                var gw = await loadGameWorldData<GameWorld>(fileName);

                return new GameWorldInfo()
                {
                    name = gw.name,
                    code = gw.code,
                    fileName = fileName,
                    width = gw.tileMap.column,
                    height = gw.tileMap.row
                };
            }));

            return result;
        }

        public static async Task<T> loadGameWorldData<T>(string fileName) where T : GameWorld
            => await fileProcessor.read<T>(fileName);

        public static async Task saveGameWorldData<T>(string fileName, T data) where T : GameWorld
            => await fileProcessor.write(fileName, data);

        public static async ValueTask<bool> create(string name, int width, int height)
        {
            if (fileProcessor.isExist(name)) return false;

            var md = ExampleHelper.getMasterData();

            var tm = ExampleHelper.getTileMap(width, height);

            var gd = ExampleHelper.getGameData();

            await fileProcessor.write(name, new GameWorld(name)
            {
                gameDate = ExampleHelper.getGameDate(),
                tileMap = tm,
                masterData = md,
                gameData = gd
            });

            return true;
        }

        public static void delete(string name) => fileProcessor.delete(name);
    }
}
