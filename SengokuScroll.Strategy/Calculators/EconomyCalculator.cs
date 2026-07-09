using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>经济结算纯计算（M3-d 最小月结）。</summary>
public static class EconomyCalculator
{
    /// <summary>据点月度金钱税收（人头税 + 商业税 + 关税）。</summary>
    public static int CalculateStrongholdMonthlyTaxMoney(Stronghold stronghold)
    {
        var poll = stronghold.Population * stronghold.PollTaxRate / 100;
        var commerce = stronghold.Population * stronghold.CommerceTaxRate / 200;
        var tariff = stronghold.Population * stronghold.TariffTaxRate / 400;
        return Math.Max(500, poll + commerce + tariff);
    }

    /// <summary>据点月度粮草税收（农业税，合）。</summary>
    public static int CalculateStrongholdMonthlyTaxFood(Stronghold stronghold)
    {
        var agriculture = stronghold.Population * stronghold.AgricultureTaxRate / 100;
        return Math.Max(200, agriculture);
    }

    /// <summary>军事单位月度维护费（金钱）。</summary>
    public static int CalculateUnitMonthlyMaintenanceMoney(Unit unit)
        => unit.IsMilitary && unit.Soldier > 0 ? Math.Max(10, unit.Soldier / 5) : 0;

    /// <summary>据点月度维持费（金钱）。</summary>
    public static int CalculateStrongholdMonthlyMaintenanceMoney(Stronghold stronghold)
        => Math.Max(100, stronghold.Population / 15);
}
