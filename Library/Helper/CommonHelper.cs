using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.Helper
{
    public static class CommonHelper
    {
        public static List<T> getEnumList<T>() where T : struct => ((T[])Enum.GetValues(typeof(T))).Cast<T>().ToList();

        public static bool isNullOrEmpty<T>(this IEnumerable<T> list) => list?.isNullOrEmpty() ?? true;
    }
}
