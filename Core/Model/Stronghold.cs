using Library;

namespace Core.Model
{
    public class Stronghold : Building
    {
        public string name;
        public int type;
        public int? forceId;
        public int? provinceId;
        /// <summary>太守、城代、除当主外可实际对该据点下达命令的人</summary>
        public int? governorId;

        /// <summary>信仰</summary>
        public int religionId;

        /// <summary>税率</summary>
        public int taxRate;

        /// <summary>人口</summary>
        public int population;

        /// <summary>人口增长率</summary>
        public int growth;

        /// <summary>工作人口</summary>
        public int job;

        /// <summary>识字率</summary>
        public int literacyRate;

        /// <summary>技术</summary>
        public int technology;

        /// <summary>劳力</summary>
        public int labour;

        /// <summary>物资</summary>
        public Supplies materialResource;

        /// <summary>官方货币</summary>
        public int currencyType;

        public string introduction;
#warning todo public FinancialReporting financialReporting;
        //public FinancialReporting financialReporting;

        public bool isForceCapital;

        public bool isSurrounded;

        public bool hasLord => governorId != null;

        public class Type : BaseObject
        {
            public int id;
            public string name;
            public int? culture;
            public string introduction;
        }
    }
}
