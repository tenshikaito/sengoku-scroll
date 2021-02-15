using Library;
using System.Collections.Generic;

namespace Core.Model
{
    public class Province : BaseObject
    {
        public int id;

        public string name;

        public int? forceId;

        public string introduction;

        public List<int> strongholds;
    }
}
