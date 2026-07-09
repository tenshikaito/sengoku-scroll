using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式信使系统接口。</summary>
public interface IStrategyMessengerSystem : IGameSystem
{
}

/// <summary>
/// 信使系统：每日推进信使移动，抵达后投递方针/战报或向运输队施加假情报。
/// </summary>
/// <remarks>
/// <para>业务规则：</para>
/// <list type="bullet">
///   <item>信使为不受玩家直接操控的地图实体，沿路径逐日移动。</item>
///   <item>方针信使抵达目标部队后写入 PendingDirective 并即时生效。</item>
///   <item>战报信使抵达当主所在格后，向玩家势力事件栏推送消息（详情仍可在战报弹窗查看）。</item>
///   <item>玩家势力相关投递会写入 <see cref="StrategyDayOutcomeBuffer"/> 供前端左上角展示。</item>
/// </list>
/// </remarks>
public class StrategyMessengerSystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer) : IStrategyMessengerSystem
{
    /// <summary>在单位系统之后、角色系统之前执行。</summary>
    public int Order { get; } = 25;

    /// <inheritdoc />
    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var messengers = gameData.Messengers;
        var convoys = gameData.SupplyConvoys;
        var toRemove = new List<int>();

        foreach (var messenger in messengers.Values.ToList())
        {
            if (messenger.Status != MessengerStatus.Moving)
                continue;

            if (messenger.RoutePoints.Count == 0)
            {
                Deliver(messenger, convoys, gameData);
                toRemove.Add(messenger.Id);
                continue;
            }

            messenger.Location = messenger.RoutePoints.Dequeue();

            if (messenger.RoutePoints.Count == 0)
            {
                Deliver(messenger, convoys, gameData);
                toRemove.Add(messenger.Id);
            }
        }

        foreach (var id in toRemove)
            messengers.Remove(id);
    }

    /// <summary>信使抵达后的投递与玩家事件通知。</summary>
    private void Deliver(
        Messenger messenger,
        Dictionary<int, SupplyConvoy> convoys,
        Domain.GameData gameData)
    {
        messenger.Status = MessengerStatus.Arrived;

        if (messenger.PayloadType == MessengerPayloadType.PolicyChange
            && gameData.Units.TryGetValue(messenger.TargetUnitId, out var unit))
        {
            MessengerActions.DeliverPendingPolicy(messenger, unit);
            NotifyPolicyDelivered(messenger, unit, gameData);
            return;
        }

        if (messenger.PayloadType == MessengerPayloadType.BattleReport)
        {
            NotifyBattleReportArrived(messenger, gameData);
            return;
        }

        if (messenger.PayloadType != MessengerPayloadType.FalseIntelligence
            || messenger.TargetConvoyId <= 0
            || !convoys.TryGetValue(messenger.TargetConvoyId, out var convoy))
        {
            return;
        }

        MessengerActions.ApplyFalseIntelligence(convoy, messenger);
    }

    /// <summary>方针信使送达部队：玩家势力写入事件栏。</summary>
    private void NotifyPolicyDelivered(Messenger messenger, Unit unit, Domain.GameData gameData)
    {
        if (messenger.ForceId != scenarioMeta.PlayerForceId)
            return;

        var directive = messenger.PendingDirective?.ToString() ?? "未知";
        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "PolicyDelivered",
            Message = $"📨 方针信使已传达至 {unit.Name}：{DirectiveLabel(directive)}"
        });
    }

    /// <summary>战报信使抵达当主所在格：玩家势力写入事件栏。</summary>
    private void NotifyBattleReportArrived(Messenger messenger, Domain.GameData gameData)
    {
        if (messenger.ForceId != scenarioMeta.PlayerForceId)
            return;

        var lordLocation = StrategyLordHelper.ResolveLocation(gameData, scenarioMeta);
        if (messenger.Location.X != lordLocation.X || messenger.Location.Y != lordLocation.Y)
            return;

        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "BattleReportArrived",
            Message = $"📨 战报信使抵达当主 {scenarioMeta.LordName}（{lordLocation.X}, {lordLocation.Y}）"
        });
    }

    private static string DirectiveLabel(string directive) => directive switch
    {
        "Move" => "移动",
        "Occupy" => "占领",
        "Raid" => "劫掠",
        "Support" => "支援",
        "Retreat" => "撤退",
        _ => directive
    };
}
