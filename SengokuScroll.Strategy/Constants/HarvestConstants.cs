namespace SengokuScroll.Strategy.Constants;

/// <summary>Region 收粮默认日历（无剧本配置时使用）。</summary>
public static class HarvestConstants
{
    /// <summary>北方单季：11 月 1 日收全额产出。</summary>
    public static readonly Data.Models.HarvestEventDefinition DefaultNorthernSingle = new(11, 1, 10_000);

    /// <summary>二季作早稻：6 月 1 日 50%。</summary>
    public static readonly Data.Models.HarvestEventDefinition DefaultDoubleEarly = new(6, 1, 5000);

    /// <summary>二季作晩稻：9 月 1 日 50%。</summary>
    public static readonly Data.Models.HarvestEventDefinition DefaultDoubleLate = new(9, 1, 5000);

    /// <summary>势力内默认贡粮比例（产出，万分比）。</summary>
    public const int DefaultInternalTributeFoodBp = 2000;

    /// <summary>势力内默认钱纳比例（当月钱税合计，万分比）。</summary>
    public const int DefaultInternalTributeMoneyBp = 2000;
}
