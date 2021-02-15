using System;

namespace Core.Model
{
    public class AskJobApply
    {
        public int id { get; set; }
        public bool isInvalid { get; set; }
        public int characterId;
        public DateTime time;
        public string comment;
    }
}
