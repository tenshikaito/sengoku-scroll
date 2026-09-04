using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>情报展示用任务（个人/人生/势力/兼职），驱动角色行动叙事；与运行时募兵任务并存。</summary>
public static class CharacterIntelTasksHelper
{
    public static IReadOnlyList<StrategyCharacterTaskDto> BuildIntelTasks(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        IReadOnlyDictionary<int, Stronghold> strongholds)
    {
        var rows = new List<StrategyCharacterTaskDto>();
        rows.AddRange(MapOperationalTasks(character, gameData, strongholds));
        rows.AddRange(character.IntelTasks.Select(t => new StrategyCharacterTaskDto
        {
            TaskCategory = t.TaskCategory,
            Name = t.Name,
            Target = t.Target,
            Status = t.Status,
            Remaining = t.Remaining,
        }));

        if (character.IntelTasks.Count == 0)
        {
            rows.AddRange(BuildPersonalTasks(character, gameData, meta, strongholds));
            rows.AddRange(BuildLifeTasks(character, gameData, meta, strongholds));
        }

        return [.. rows
            .GroupBy(row => $"{row.TaskCategory}:{row.Name}:{row.Target}", StringComparer.Ordinal)
            .Select(group => group.First())];
    }

    /// <summary>将情报任务写入角色实体（开局 bootstrap）。</summary>
    public static IReadOnlyList<CharacterIntelTask> BuildStoredIntelTasks(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        IReadOnlyDictionary<int, Stronghold> strongholds)
    {
        var dtos = new List<StrategyCharacterTaskDto>();
        dtos.AddRange(BuildPersonalTasks(character, gameData, meta, strongholds));
        dtos.AddRange(BuildLifeTasks(character, gameData, meta, strongholds));

        return dtos.Select(t => new CharacterIntelTask
        {
            TaskCategory = t.TaskCategory,
            Name = t.Name,
            Target = t.Target,
            Status = t.Status,
            Remaining = t.Remaining,
        }).ToList();
    }

    private static IEnumerable<StrategyCharacterTaskDto> MapOperationalTasks(
        Character character,
        GameData gameData,
        IReadOnlyDictionary<int, Stronghold> strongholds)
    {
        if (character.RecruitAssignment is { } assignment && character.RecruitTask is null)
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Force",
                Name = assignment.Kind == CharacterRecruitTaskKind.Mercenary ? "募兵" : "征兵",
                Target = ResolveStrongholdName(strongholds, assignment.StrongholdId),
                Status = "待执行",
                Remaining = "—",
            };
        }

        if (character.RecruitTask is { } task)
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Force",
                Name = task.Kind == CharacterRecruitTaskKind.Mercenary ? "募兵" : "征兵",
                Target = ResolveStrongholdName(strongholds, task.StrongholdId),
                Status = task.Phase switch
                {
                    CharacterRecruitTaskPhase.Travel => "前往",
                    CharacterRecruitTaskPhase.Execute => "执行中",
                    CharacterRecruitTaskPhase.Report => "汇报",
                    _ => "进行中",
                },
                Remaining = $"{Math.Max(0, task.DeadlineDaysRemaining)}日",
            };
        }

        if (character.DiplomacyMission is { } diplomacyMission)
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Force",
                Name = ResolveDiplomacyMissionLabel(diplomacyMission.Action),
                Target = ResolveForceName(gameData, diplomacyMission.TargetForceId),
                Status = "出使中",
                Remaining = $"{Math.Max(0, diplomacyMission.RemainingDays)}日",
            };
        }

        if (character.ForceStatus == Character.CharacterForceStatus.UnitAction)
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Force",
                Name = "出阵",
                Target = character.LocationType == Character.CharacterLocationType.Unit ? "部队" : "地图",
                Status = "进行中",
                Remaining = "—",
            };
        }
    }

    private static IEnumerable<StrategyCharacterTaskDto> BuildPersonalTasks(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        IReadOnlyDictionary<int, Stronghold> strongholds)
    {
        if (character.IsDead)
            yield break;

        var age = ResolveAge(character, gameData.GameDate);
        if (character.SpouseId <= 0 && age >= 18)
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Personal",
                Name = "缔结婚姻",
                Target = ResolveHomeStrongholdName(character, strongholds),
                Status = "筹划中",
                Remaining = "—",
            };
        }

        if (character.ForceId > 0 && !IsOrganizationMember(character, gameData))
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Personal",
                Name = "稳固仕官",
                Target = ResolveForceName(gameData, character.ForceId),
                Status = "进行中",
                Remaining = "—",
            };
        }
        else if (character.ForceId == 0)
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Personal",
                Name = "奔走仕官",
                Target = ResolveHomeStrongholdName(character, strongholds),
                Status = "进行中",
                Remaining = "—",
            };
        }

        if (IsOrganizationMember(character, gameData))
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "PartTime",
                Name = "整理账簿",
                Target = ResolveOrganizationShopName(character, gameData),
                Status = "例行",
                Remaining = "—",
            };
        }
    }

    private static IEnumerable<StrategyCharacterTaskDto> BuildLifeTasks(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        IReadOnlyDictionary<int, Stronghold> strongholds)
    {
        if (character.IsDead)
            yield break;

        var stronghold = ResolveHomeStronghold(character, strongholds);
        var role = ResolveIntelRole(character, gameData, meta, stronghold);

        if (role is "当主" or "领主")
        {
            var isResidence = stronghold != null
                && StrategyStrongholdLordHelper.IsGovernanceResidence(stronghold, meta, gameData);
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Life",
                Name = isResidence ? "统一领国" : "守成领内",
                Target = ResolveForceName(gameData, character.ForceId),
                Status = "长期",
                Remaining = "—",
            };
            yield break;
        }

        if (role is "住持" or "别当" or "执事")
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Life",
                Name = "弘传本教",
                Target = ResolveOrganizationShopName(character, gameData),
                Status = "长期",
                Remaining = "—",
            };
            yield break;
        }

        if (role is "老板" or "店长" or "掌柜")
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Life",
                Name = "扩大商路",
                Target = ResolveOrganizationShopName(character, gameData),
                Status = "长期",
                Remaining = "—",
            };
            yield break;
        }

        if (character.ForceStatus == Character.CharacterForceStatus.Task)
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Life",
                Name = "完成差遣",
                Target = ResolveHomeStrongholdName(character, strongholds),
                Status = "进行中",
                Remaining = character.RecruitTask is { } task
                    ? $"{Math.Max(0, task.DeadlineDaysRemaining)}日"
                    : "—",
            };
            yield break;
        }

        if (character.ForceId > 0)
        {
            yield return new StrategyCharacterTaskDto
            {
                TaskCategory = "Life",
                Name = "扬名立万",
                Target = ResolveForceName(gameData, character.ForceId),
                Status = "长期",
                Remaining = "—",
            };
        }
    }

    private static string ResolveIntelRole(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        Stronghold? stronghold)
    {
        if (stronghold != null)
        {
            if (stronghold.LordId == character.Id)
            {
                return StrategyStrongholdLordHelper.IsGovernanceResidence(stronghold, meta, gameData)
                    ? "当主"
                    : "领主";
            }

            if (stronghold.LeaderId == character.Id)
                return "代官";
        }

        foreach (var shopStronghold in gameData.Strongholds.Values)
        {
            foreach (var actor in shopStronghold.MerchantActors.Concat(shopStronghold.ReligionActors))
            {
                var index = actor.CharacterIds.IndexOf(character.Id);
                if (index < 0)
                    continue;

                var kind = actor.Type == ActorType.Regligion ? "Religion" : "Merchant";
                return OrganizationRoleLabels(kind, index);
            }
        }

        return "—";
    }

    private static string OrganizationRoleLabels(string kind, int index)
        => kind switch
        {
            "Religion" => index switch
            {
                0 => "住持",
                1 => "别当",
                _ => "执事",
            },
            _ => index switch
            {
                0 => "老板",
                1 => "店长",
                _ => "掌柜",
            },
        };

    private static bool IsOrganizationMember(Character character, GameData gameData)
        => gameData.Forces.TryGetValue(character.ForceId, out var force)
            && OrganizationForceHelper.IsOrganizationForce(force);

    private static int ResolveAge(Character character, GameDate gameDate)
    {
        var age = Math.Max(0, gameDate.Year - character.Birthday.Year);
        if (gameDate.Month < character.Birthday.Month
            || (gameDate.Month == character.Birthday.Month && gameDate.Day < character.Birthday.Day))
        {
            age = Math.Max(0, age - 1);
        }

        return age;
    }

    private static string ResolveOrganizationShopName(Character character, GameData gameData)
    {
        foreach (var shop in OrganizationForceHelper.EnumerateShops(gameData, character.ForceId))
        {
            if (shop.CharacterIds.Contains(character.Id))
                return string.IsNullOrWhiteSpace(shop.Name) ? "—" : shop.Name.Trim();
        }

        return gameData.Forces.TryGetValue(character.ForceId, out var force)
            ? force.Name
            : "—";
    }

    private static string ResolveForceName(GameData gameData, int forceId)
        => gameData.Forces.TryGetValue(forceId, out var force) && !string.IsNullOrWhiteSpace(force.Name)
            ? force.Name.Trim()
            : $"势力#{forceId}";

    private static string ResolveDiplomacyMissionLabel(string action)
        => action switch
        {
            "Ally" => "同盟",
            "War" => "宣战",
            _ => "议和",
        };

    private static Stronghold? ResolveHomeStronghold(
        Character character,
        IReadOnlyDictionary<int, Stronghold> strongholds)
    {
        var strongholdId = character.LocationType == Character.CharacterLocationType.Stronghold
            ? character.LocationStrongholdId
            : character.StrongholdId;
        return strongholds.TryGetValue(strongholdId, out var stronghold) ? stronghold : null;
    }

    private static string ResolveHomeStrongholdName(
        Character character,
        IReadOnlyDictionary<int, Stronghold> strongholds)
        => ResolveStrongholdName(strongholds, character.StrongholdId);

    private static string ResolveStrongholdName(IReadOnlyDictionary<int, Stronghold> strongholds, int strongholdId)
        => strongholds.TryGetValue(strongholdId, out var stronghold) && !string.IsNullOrWhiteSpace(stronghold.Name)
            ? stronghold.Name.Trim()
            : $"据点#{strongholdId}";
}
