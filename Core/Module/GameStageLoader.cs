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
using static Core.Constant.GameStageConstant;

namespace Core.Module
{
    public class GameStageLoader
    {
        public string name { get; }

        public GameStageLoader(string name)
        {
            this.name = name;
        }

        public static List<GameStageInfo> getList()
        {
            Directory.CreateDirectory(stageDirectoryName);

            return Directory.EnumerateDirectories(stageDirectoryName)
                .Select(o => $"{stageDirectoryName}/{o}/{stageInformationFileName}")
                .Where(o => File.Exists(o))
                .Select(o => FileHelper.read<GameStageInfo>(o))
                .ToList();
        }
    }
}
