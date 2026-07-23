using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Rules;

/// <summary>角色体力消耗、生病与死亡规则。</summary>
public static class CharacterStaminaRules
{
    public const int SickHpThreshold = 60;
    public const int CommandFatigueCost = 4;

    /// <summary>本日是否处于「执行命令」状态并应扣体力。</summary>
    public static bool ShouldApplyCommandFatigue(Character character)
    {
        if (character.IsDead)
            return false;

        if (character.RecruitTask?.Phase == Domain.Entities.Types.CharacterRecruitTaskPhase.Execute)
            return true;

        return character.ForceStatus == CharacterForceStatus.Task
               && character.ActionStatus is CharacterActionStatus.Acting or CharacterActionStatus.Moving;
    }

    /// <summary>执行命令时扣减体力，并判定生病/死亡下限。</summary>
    public static void ApplyCommandFatigue(Character character, GameData gameData)
    {
        if (character.IsDead || !ShouldApplyCommandFatigue(character))
            return;

        character.Hp -= CommandFatigueCost;
        ApplyHpFloorAndDeath(character, gameData);
        TryContractIllness(character);
    }

    /// <summary>休息恢复体力；生病且高龄时恢复更慢。</summary>
    public static int ResolveRestRecovery(Character character, GameDate gameDate)
    {
        if (!character.IsSick)
            return 8;

        var age = CharacterAiRules.ComputeAge(character, gameDate);
        if (age >= CharacterAiRules.VeryElderAgeThreshold)
            return 2;
        if (age >= CharacterAiRules.ElderAgeThreshold)
            return 4;
        return 6;
    }

    /// <summary>生病且体力恢复到阈值以上时解除生病。</summary>
    public static void TryRecoverFromIllness(Character character)
    {
        if (character.IsSick && character.Hp >= SickHpThreshold)
            character.IsSick = false;
    }

    private static void ApplyHpFloorAndDeath(Character character, GameData gameData)
    {
        var age = CharacterAiRules.ComputeAge(character, gameData.GameDate);
        if (character.IsSick && age >= CharacterAiRules.ElderAgeThreshold && character.Hp <= 0)
        {
            character.IsDead = true;
            character.Hp = 0;
            character.IsSick = false;
            return;
        }

        character.Hp = Math.Max(1, character.Hp);
    }

    private static void TryContractIllness(Character character)
    {
        if (character.IsSick || character.Hp >= SickHpThreshold)
            return;

        var deficit = SickHpThreshold - character.Hp;
        var chance = Math.Clamp(deficit * 2, 5, 95);
        if (Random.Shared.Next(100) < chance)
            character.IsSick = true;
    }
}
