using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Library.Helper.FileHelper;

namespace Core.Helper
{
    public static class WordingHelper
    {
        public static async Task<GameWording> loadCharset(string charset = "zh-tw")
        {
            var lines = (await readLines("charset/system.dat")).Union(await readLines("charset/zh-tw.dat"));

            return new GameWording(charset, lines.Where(o => !o.StartsWith("#") && !string.IsNullOrWhiteSpace(o)).Select(o =>
            {
                var line = o.Split('=');
                return new KeyValuePair<string, string>(line[0], line[1]);
            }).ToDictionary(o => o.Key, o => o.Value));
        }

    }
}
