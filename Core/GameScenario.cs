using Core.Data;
using System;

namespace Core
{
    public class GameScenario : GameData
    {
        public string name;

        public string code;

        public string introduction;

        public GameScenario(string name)
        {
            this.name = name;
        }

        public void @new()
        {
            code = Guid.NewGuid().ToString();
        }
    }
}
