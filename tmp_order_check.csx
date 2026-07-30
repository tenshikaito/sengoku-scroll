using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Helpers;

using var host = new StrategySimulationHost();
host.LoadScenario("mini_kanto");
var shId = host.GetState().Value!.Strongholds.First(sh => sh.Name is "清洲" or "清州城").Id;
var gd = typeof(StrategySimulationHost).GetField("simulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(host);
var sim = gd!.GetType().GetProperty("World") != null ? null : gd;
// use reflection to get world
var simField = host.GetType().GetField("simulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var simulation = simField!.GetValue(host);
var world = simulation!.GetType().GetProperty("World")!.GetValue(simulation)!;
var gameData = world.GetType().GetProperty("GameData")!.GetValue(world)!;
var strongholds = (System.Collections.Generic.IDictionary<int, object>)gameData.GetType().GetProperty("Strongholds")!.GetValue(gameData)!;
var sh = strongholds[shId];
var market = sh.GetType().GetProperty("Market")!.GetValue(sh)!;
var orders = (System.Collections.IList)market.GetType().GetProperty("Orders")!.GetValue(market)!;
Console.WriteLine($"Before advance orders: {orders.Count}");
var snap = host.GetMarketSnapshot(shId).Value!;
Console.WriteLine($"Bid levels: {snap.BidLevels.Count}, Ask levels: {snap.AskLevels.Count}, PlayerOpen: {snap.PlayerOpenOrders.Count}");
host.AdvanceDay();
orders = (System.Collections.IList)market.GetType().GetProperty("Orders")!.GetValue(market)!;
Console.WriteLine($"After advance orders: {orders.Count}");
snap = host.GetMarketSnapshot(shId).Value!;
Console.WriteLine($"Bid levels: {snap.BidLevels.Count}, Ask levels: {snap.AskLevels.Count}, PlayerOpen: {snap.PlayerOpenOrders.Count}");
