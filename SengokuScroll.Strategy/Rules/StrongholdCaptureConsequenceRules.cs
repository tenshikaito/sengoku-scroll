using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Rules;

/// <summary>据点陷落后的政务后果：代官清空、居城迁移、将领俘虏、战时占领登记。</summary>
public static class StrongholdCaptureConsequenceRules
{
    /// <summary>据点易手后处理在城将领、居城迁移、战时占领登记与日终事件。</summary>
    public static void Apply(
        Stronghold stronghold,
        int previousForceId,
        int captorForceId,
        GameData gameData,
        GameMasterData gameMasterData,
        StrategyScenarioMeta meta,
        StrategyForceLordRegistry lordRegistry,
        StrategyWarOccupationRegistry warOccupationRegistry,
        StrategyDayOutcomeBuffer dayOutcomeBuffer,
        GameDate occupiedDate)
    {
        var capturedNames = new List<string>();
        var mayorId = stronghold.LeaderId;
        var appointedLordId = stronghold.LordId;

        // 业务：占领后清空代官，待新势力任命
        stronghold.LeaderId = 0;

        var wasLordResidence = WasForceLordResidence(
            stronghold,
            previousForceId,
            meta,
            gameData,
            lordRegistry);

        var forceLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            previousForceId,
            meta,
            gameData,
            lordRegistry);

        // 业务：失守据点为势力当主居城时，在城当主被俘，不在城则迁往他城
        if (wasLordResidence && forceLordId > 0
            && gameData.Characters.TryGetValue(forceLordId, out var forceLord))
        {
            if (IsCharacterPresentInStronghold(forceLord, stronghold))
            {
                if (TryCaptureCharacter(forceLord, captorForceId, stronghold, gameData))
                    capturedNames.Add(forceLord.Name);
            }
            else
            {
                MigrateLordResidence(forceLord, previousForceId, stronghold, gameData, dayOutcomeBuffer);
            }
        }

        // 业务：任命城主（非当主）若在城则优先被俘
        if (appointedLordId > 0
            && appointedLordId != forceLordId
            && gameData.Characters.TryGetValue(appointedLordId, out var appointedLord)
            && IsCharacterPresentInStronghold(appointedLord, stronghold)
            && TryCaptureCharacter(appointedLord, captorForceId, stronghold, gameData))
        {
            capturedNames.Add(appointedLord.Name);
        }

        // 业务：代官若在城且非已处理的当主/城主，同样被俘
        if (mayorId > 0
            && mayorId != forceLordId
            && mayorId != appointedLordId
            && gameData.Characters.TryGetValue(mayorId, out var mayor)
            && IsCharacterPresentInStronghold(mayor, stronghold)
            && TryCaptureCharacter(mayor, captorForceId, stronghold, gameData))
        {
            capturedNames.Add(mayor.Name);
        }

        // 业务：其余在城非俘虏将领一并登记被俘
        foreach (var character in gameData.Characters.Values
                     .Where(c => !c.IsDead
                                 && c.ForceStatus != CharacterForceStatus.Prisoner
                                 && c.StrongholdId == stronghold.Id
                                 && c.LocationType == CharacterLocationType.Stronghold))
        {
            if (capturedNames.Contains(character.Name))
                continue;

            if (character.Id == forceLordId || character.Id == appointedLordId || character.Id == mayorId)
                continue;

            if (TryCaptureCharacter(character, captorForceId, stronghold, gameData))
                capturedNames.Add(character.Name);
        }

        warOccupationRegistry.RecordOccupation(stronghold, previousForceId, captorForceId, occupiedDate);

        var occupierName = gameData.Forces.TryGetValue(captorForceId, out var captor)
            ? captor.Name
            : $"势力{captorForceId}";

        if (capturedNames.Count > 0)
        {
            dayOutcomeBuffer.AddEvent(new StrategyEventDto
            {
                Category = "CharacterCaptured",
                Brief = $"⛓ {stronghold.Name} 将领被俘",
                Message = $"{occupierName} 占领 {stronghold.Name}，俘获 {string.Join("、", capturedNames)}。"
            });
        }

        if (wasLordResidence && forceLordId > 0
            && gameData.Characters.TryGetValue(forceLordId, out var lordAfter)
            && lordAfter.ForceStatus != CharacterForceStatus.Prisoner
            && lordAfter.StrongholdId != stronghold.Id)
        {
            dayOutcomeBuffer.AddEvent(new StrategyEventDto
            {
                Category = "LordResidenceMoved",
                Brief = $"🏯 居城迁移",
                Message = $"{ResolveForceName(previousForceId, gameData)} 失去居城 {stronghold.Name}，当主驻留迁至 {ResolveStrongholdName(lordAfter.StrongholdId, gameData)}。"
            });
        }
    }

    private static bool WasForceLordResidence(
        Stronghold stronghold,
        int forceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        StrategyForceLordRegistry lordRegistry)
    {
        var lordCharacterId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            forceId,
            meta,
            gameData,
            lordRegistry);

        if (lordCharacterId <= 0
            || !gameData.Characters.TryGetValue(lordCharacterId, out var lord))
        {
            return false;
        }

        return lord.StrongholdId == stronghold.Id;
    }

    private static void MigrateLordResidence(
        Character lord,
        int previousForceId,
        Stronghold captured,
        GameData gameData,
        StrategyDayOutcomeBuffer dayOutcomeBuffer)
    {
        // 业务：当主不在陷落城时，迁至本势力剩余最大人口据点
        var fallback = gameData.Strongholds.Values
            .Where(s => s.ForceId == previousForceId && s.Id != captured.Id)
            .OrderByDescending(s => s.Population)
            .ThenBy(s => s.Id)
            .FirstOrDefault();

        if (fallback is null)
            return;

        StrategyStrongholdLordHelper.EnsureLordResidence(fallback, lord);
    }

    private static bool IsCharacterPresentInStronghold(Character character, Stronghold stronghold)
        => character.StrongholdId == stronghold.Id
           && character.LocationType == CharacterLocationType.Stronghold;

    private static bool TryCaptureCharacter(
        Character character,
        int captorForceId,
        Stronghold captorStronghold,
        GameData gameData)
    {
        if (character.IsDead || character.ForceStatus == CharacterForceStatus.Prisoner)
            return false;

        character.ForceStatus = CharacterForceStatus.Prisoner;
        character.StrongholdId = captorStronghold.Id;
        character.Location = captorStronghold.Location;
        character.LocationType = CharacterLocationType.Stronghold;
        character.LocationStrongholdId = captorStronghold.Id;
        character.ActionTarget.RoutePoints.Clear();
        return true;
    }

    private static string ResolveForceName(int forceId, GameData gameData)
        => gameData.Forces.TryGetValue(forceId, out var force) ? force.Name : $"势力{forceId}";

    private static string ResolveStrongholdName(int strongholdId, GameData gameData)
        => gameData.Strongholds.TryGetValue(strongholdId, out var sh) ? sh.Name : $"据点{strongholdId}";
}
