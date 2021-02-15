using System.Collections.Generic;

namespace Library
{
    public class IncreasedIdDictionary<TValue>
    {
        private readonly int startId;

        public int id;
        public Dictionary<int, TValue> map;

        public TValue this[int index]
        {
            get => map[index];
            set => map[index] = value;
        }

        public IncreasedIdDictionary(int startId = 1)
        {
            id = this.startId = startId;
        }

        public IncreasedIdDictionary<TValue> init()
        {
            id = startId;
            map = new Dictionary<int, TValue>();

            return this;
        }

        public int getNextId() => id++;
    }
}
