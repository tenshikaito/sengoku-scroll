using Library;
using Library.Helper;
using System.Collections.Generic;

namespace Core.Model
{
    public class Region : BaseObject
    {
        public static readonly List<Climate> list = CommonHelper.getEnumList<Climate>();

        public int id;
        public string name;
        /// <summary>气候</summary>
        public Climate climate;
    }
}
