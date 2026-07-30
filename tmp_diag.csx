using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Hosting;

using var host = new StrategySimulationHost();
host.LoadScenario("mini_kanto");
var id = host.GetState().Value!.Strongholds.First(sh => sh.Name is "清洲" or "清州城").Id;
var d1 = host.GetMarketSnapshot(id).Value!;
Console.WriteLine($"Day1 asks={d1.AskLevels.Count} bids={d1.BidLevels.Count} player={d1.PlayerOpenOrders.Count}");
Console.WriteLine($"Day1 ask raw: {string.Join(", ", d1.AskLevels.Where(l=>l.PriceMoneyPerGo>0).Select(l=>$"{l.PriceMoneyPerGo}@{l.QuantityGo}"))}");
host.AdvanceDay();
var d2 = host.GetMarketSnapshot(id).Value!;
Console.WriteLine($"Day2 asks={d2.AskLevels.Count} bids={d2.BidLevels.Count} player={d2.PlayerOpenOrders.Count}");
Console.WriteLine($"Day2 ask raw: {string.Join(", ", d2.AskLevels.Where(l=>l.PriceMoneyPerGo>0).Select(l=>$"{l.PriceMoneyPerGo}@{l.QuantityGo}"))}");
Console.WriteLine($"Day2 bid raw: {string.Join(", ", d2.BidLevels.Where(l=>l.PriceMoneyPerGo>0).Select(l=>$"{l.PriceMoneyPerGo}@{l.QuantityGo}"))}");
