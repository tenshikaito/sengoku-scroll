using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain.Rules;

/// <summary>战争参战/敌对/共战判定（军事堆叠与接敌）。</summary>
public static class WarRules
{
    /// <summary>两势力是否可军事同格堆叠：同势力，或处于同一场未结束战争的同侧。</summary>
    public static bool CanMilitaryStack(int forceIdA, int forceIdB, GameData gameData)
    {
        if (forceIdA == forceIdB)
            return true;

        foreach (var war in gameData.Wars.Values)
        {
            if (war.IsEnded)
                continue;

            if (AreOnSameSide(war, forceIdA, forceIdB))
                return true;
        }

        return false;
    }

    /// <summary>两势力是否处于某场战争的对立两侧。</summary>
    public static bool AreWarEnemies(int forceIdA, int forceIdB, GameData gameData)
    {
        if (forceIdA == forceIdB)
            return false;

        foreach (var war in gameData.Wars.Values)
        {
            if (war.IsEnded)
                continue;

            if (AreOnOppositeSides(war, forceIdA, forceIdB))
                return true;
        }

        return false;
    }

    /// <summary>查找双方正在进行的战争（对立两侧）。</summary>
    public static War? FindActiveWarBetween(int forceIdA, int forceIdB, GameData gameData)
    {
        foreach (var war in gameData.Wars.Values)
        {
            if (war.IsEnded)
                continue;

            if (AreOnOppositeSides(war, forceIdA, forceIdB))
                return war;
        }

        return null;
    }

    public static bool IsParticipant(War war, int forceId)
        => war.AggressorForceIds.Contains(forceId) || war.DefenderForceIds.Contains(forceId);

    public static bool IsOnAggressorSide(War war, int forceId)
        => war.AggressorForceIds.Contains(forceId);

    public static bool AreOnSameSide(War war, int forceIdA, int forceIdB)
    {
        var aAgg = war.AggressorForceIds.Contains(forceIdA);
        var bAgg = war.AggressorForceIds.Contains(forceIdB);
        if (aAgg && bAgg)
            return true;

        var aDef = war.DefenderForceIds.Contains(forceIdA);
        var bDef = war.DefenderForceIds.Contains(forceIdB);
        return aDef && bDef;
    }

    public static bool AreOnOppositeSides(War war, int forceIdA, int forceIdB)
    {
        var aAgg = war.AggressorForceIds.Contains(forceIdA);
        var aDef = war.DefenderForceIds.Contains(forceIdA);
        var bAgg = war.AggressorForceIds.Contains(forceIdB);
        var bDef = war.DefenderForceIds.Contains(forceIdB);
        return (aAgg && bDef) || (aDef && bAgg);
    }

    /// <summary>创建双边战争；攻方=宣战方。</summary>
    public static War CreateWar(GameData gameData, int aggressorForceId, int defenderForceId, GameDate startDate)
    {
        var id = gameData.Wars.Count == 0 ? 1 : gameData.Wars.Keys.Max() + 1;
        var war = new War
        {
            Id = id,
            AggressorForceId = aggressorForceId,
            DefenderForceId = defenderForceId,
            AggressorForceIds = [aggressorForceId],
            DefenderForceIds = [defenderForceId],
            StartDate = startDate,
            IsEnded = false,
        };
        gameData.Wars[id] = war;
        return war;
    }

    /// <summary>若尚无对立战争，则用敌对外交关系补建一场（宣战方=forceA）。</summary>
    public static War EnsureWarBetween(GameData gameData, int forceA, int forceB, GameDate date)
    {
        var existing = FindActiveWarBetween(forceA, forceB, gameData);
        if (existing is not null)
            return existing;

        return CreateWar(gameData, forceA, forceB, date);
    }

    /// <summary>参战国加入攻方或守方。</summary>
    public static bool TryJoinWar(War war, int forceId, bool joinAggressorSide)
    {
        if (war.IsEnded || IsParticipant(war, forceId))
            return false;

        if (joinAggressorSide)
            war.AggressorForceIds.Add(forceId);
        else
            war.DefenderForceIds.Add(forceId);

        return true;
    }

    /// <summary>结束整场战争。</summary>
    public static void EndWar(War war, GameDate endDate)
    {
        war.IsEnded = true;
        war.EndDate = endDate;
    }

    /// <summary>参战国单独退出（从名单移除）；若为主战国则结束整场。</summary>
    public static void SeparatePeace(War war, int forceId, GameDate date)
    {
        if (forceId == war.AggressorForceId || forceId == war.DefenderForceId)
        {
            EndWar(war, date);
            return;
        }

        war.AggressorForceIds.Remove(forceId);
        war.DefenderForceIds.Remove(forceId);
    }
}
