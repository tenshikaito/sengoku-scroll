using Core.Data;
using Core.Extension;
using Library.Extension;
using Library.Helper;
using Library.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Helper
{
    public static class GameStageHelper
    {
        private const string directoryName = "stage";
        private const string introductionFileName = "introduction.dat";
        private const string mapFileName = "map.dat";

        public static async Task<IEnumerable<GameStageInfo>> getInfoList()
        {
            Directory.CreateDirectory(directoryName);

            return await Task.WhenAll(Directory.EnumerateDirectories(directoryName)
                .Where(o => File.Exists($"{o}/{directoryName}/{introductionFileName}"))
                .Select(o => FileHelper.read<GameStageInfo>($"{o}/{directoryName}/{introductionFileName}")));
        }

        public static async Task save(string name, GameStageInfo gsi, TileMap tileMap)
        {
            var path = $"{directoryName}/{name}";

            Directory.CreateDirectory(path);

            await FileHelper.write($"{path}/{introductionFileName}", gsi);
            await FileHelper.write($"{path}/{mapFileName}", tileMap);
        }
    }
}
