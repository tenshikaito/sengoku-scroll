using Core.Data;
using System;

namespace Core
{
    public class GameWorld
    {
        public string name;

        public string code;

        public string resourcePackageName;

        public string introduction;

        public GameDate gameDate;

        public TileMap tileMap;

        public MasterData masterData;

        public GameData gameData;

        public GameWorld(string name)
        {
            this.name = name;

            code = Guid.NewGuid().ToString();
        }
    }
}
