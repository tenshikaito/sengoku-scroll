using Library.Extension;
using Library.Module;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Helper
{
    public static class GameScenarioHelper
    {
        private static readonly FileProcessor fileProcessor
            = new FileProcessor("/gameworld", "sgs");

        public static async Task<GameScenarioInfo[]> getInfoList()
        {
            fileProcessor.createDirectory();

            var filePathList = fileProcessor.enumerateFiles();

            var result = await Task.WhenAll(filePathList.Select(async o =>
            {
                var fileName = o.getFileName();
                var gw = await loadGameScenarioData<GameScenario>(fileName);

                return new GameScenarioInfo()
                {
                    name = gw.name,
                    code = gw.code,
                    fileName = fileName,
                    introduction = gw.introduction
                };
            }));

            return result;
        }

        public static async Task<T> loadGameScenarioData<T>(string fileName) where T : GameScenario
            => await fileProcessor.read<T>(fileName);

        public static async Task saveGameScenarioData<T>(string fileName, T data) where T : GameScenario
            => await fileProcessor.write(fileName, data);

        public static async ValueTask<bool> create(string name, int width, int height)
        {
            if (fileProcessor.isExist(name)) return false;

            var gd = ExampleHelper.getGameData();

            throw new NotImplementedException();
            //await fileProcessor.write(name, new GameScenario(name)
            //{
            //    gameDate = ExampleHelper.getGameDate(),
            //    tileMap = tm,
            //    masterData = md,
            //    gameData = gd,
            //    introduction = string.Empty
            //});

            return true;
        }

        public static void delete(string name) => fileProcessor.delete(name);
    }
}
