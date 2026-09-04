using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Systems;

public interface IStrategyCharacterObjectiveSystem : IGameSystem
{
}

/// <summary>把人物长期目标与真实世界状态同步，避免任务 Tab 永久停留在演示文本。</summary>
public sealed class StrategyCharacterObjectiveSystem(IGameContext context) : IStrategyCharacterObjectiveSystem
{
    public int Order => 32;

    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var totalStrongholds = gameData.Strongholds.Count;
        var strongholdsByRealm = gameData.Strongholds.Values
            .GroupBy(stronghold => TributeRoutingHelper.ResolveRealmRootForceId(stronghold.ForceId, gameData))
            .ToDictionary(group => group.Key, group => group.Count());
        var characters = gameData.Characters.Values
            .OrderBy(character => character.Id)
            .ToArray();

        StrategyParallelWork.ForEachIndex(
            characters.Length,
            index =>
            {
                var character = characters[index];
                foreach (var task in character.IntelTasks)
                    RefreshTask(task, character, gameData, strongholdsByRealm, totalStrongholds);
            },
            minimumParallelCount: 32);
    }

    private static void RefreshTask(
        CharacterIntelTask task,
        Character character,
        GameData gameData,
        IReadOnlyDictionary<int, int> strongholdsByRealm,
        int totalStrongholds)
    {
        switch (task.Name)
        {
            case "缔结婚姻" when character.SpouseId > 0:
                task.Complete();
                break;
            case "奔走仕官" when character.ForceId > 0:
                task.Complete();
                break;
            case "稳固仕官":
                {
                    var years = Math.Max(0, gameData.GameDate.Year - character.ServiceDate.Year);
                    if (years >= 3)
                        task.Complete();
                    else
                    {
                        task.Status = $"进度 {years}/3年";
                        task.Remaining = $"{3 - years}年";
                    }
                    break;
                }
            case "扬名立万":
                {
                    var progress = Math.Clamp(character.Popular, 0, 100);
                    if (progress >= 100)
                        task.Complete();
                    else
                    {
                        task.Status = $"进度 {progress}/100";
                        task.Remaining = $"{100 - progress}";
                    }
                    break;
                }
            case "统一领国":
                {
                    var realmRoot = TributeRoutingHelper.ResolveRealmRootForceId(character.ForceId, gameData);
                    var owned = strongholdsByRealm.GetValueOrDefault(realmRoot);
                    if (owned >= totalStrongholds && totalStrongholds > 0)
                        task.Complete();
                    else
                    {
                        task.Status = $"进度 {owned}/{totalStrongholds}城";
                        task.Remaining = $"{Math.Max(0, totalStrongholds - owned)}城";
                    }
                    break;
                }
        }
    }
}
