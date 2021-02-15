using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class Supplies : Dictionary<int, int>
    {
        public new int this[int t]
        {
            get => TryGetValue(t, out var value) ? value : 0;
            set
            {
                if (value == 0) Remove(t);
                else base[t] = value;
            }
        }

        public bool isEmpty => Values.All(o => o == 0);

        public bool canMinus(Supplies ss) => ss.All(o => o.Value >= this[o.Key]);

        public void set(Dictionary<int, int> value)
        {
            foreach (var o in value) this[o.Key] = o.Value;
        }

        public static bool operator <(Supplies ss1, Supplies ss2) => ss1.All(o => o.Value < ss2[o.Key]);

        public static bool operator >(Supplies ss1, Supplies ss2) => ss2.All(o => o.Value < ss1[o.Key]);

        public static bool operator <=(Supplies ss1, Supplies ss2) => !(ss1 > ss2);

        public static bool operator >=(Supplies ss1, Supplies ss2) => !(ss1 < ss2);

        public static void calculate(Supplies ss, Supplies ss1, Supplies ss2, Func<int, int, int> callback)
        {
            foreach (var o in ss1.Keys.Union(ss2.Keys))
            {
                ss[o] = callback(ss1[o], ss2[o]);
            }
        }

        public static void calculate(Supplies ss, Supplies sss, Func<int, int> callback)
        {
            foreach (var o in sss.Keys)
            {
                ss[o] = callback(sss[o]);
            }
        }
    }
}
