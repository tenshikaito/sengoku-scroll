using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Actions;

public static class CharacterMarriageActions
{
    public static bool Eligible(GameData data, Character a, Character b)
    {
        bool Adult(Character c) => c.Birthday.Year > 0 && data.GameDate.Year - c.Birthday.Year
            - ((data.GameDate.Month, data.GameDate.Day).CompareTo((c.Birthday.Month, c.Birthday.Day)) < 0 ? 1 : 0) >= 18;
        bool Single(Character c) => c.SpouseId == 0
            || data.Characters.TryGetValue(c.SpouseId, out var spouse) && spouse.IsDead;
        return a.Id != b.Id && !a.IsDead && !b.IsDead && Adult(a) && Adult(b) && Single(a) && Single(b)
            && a.ForceStatus != Character.CharacterForceStatus.Prisoner
            && b.ForceStatus != Character.CharacterForceStatus.Prisoner && !CloseKin(data, a, b);
    }

    private static bool CloseKin(GameData data, Character a, Character b)
    {
        HashSet<int> Ancestors(Character c)
        {
            var result = new HashSet<int>();
            var parents = new[] { c.FatherId, c.MotherId }.Where(id => id > 0).ToArray();
            foreach (var id in parents)
            {
                result.Add(id);
                if (data.Characters.TryGetValue(id, out var parent))
                { if (parent.FatherId > 0) result.Add(parent.FatherId); if (parent.MotherId > 0) result.Add(parent.MotherId); }
            }
            return result;
        }
        var left = Ancestors(a); var right = Ancestors(b);
        return left.Contains(b.Id) || right.Contains(a.Id) || left.Overlaps(right);
    }

    public static GameResult ProposeOrAccept(GameData data, Character actor, Character target, out string message)
    {
        message = "";
        if (!Eligible(data, actor, target)) return new GameError("MarriageIneligible");
        if (!CharacterSocialActions.AreCoLocated(actor, target)) return new GameError("MarriageNotCoLocated");
        if (actor.Ap < 2) return GameError.ApNotEnough;
        var day = data.GameDate.TotalDays;
        if (actor.PendingMarriageFromId == target.Id && actor.MarriageProposalExpiryDay > day)
        {
            actor.Ap -= 2;
            Complete(data, actor, target);
            message = "双方同意，婚姻成立（不自动改变外交或势力归属）";
            return GameResult.Ok();
        }
        if (actor.Relationships.FirstOrDefault(r => r.TargetCharacterId == target.Id)?.LastMarriageProposalDay is int last
            && day - last < 30) return new GameError("MarriageProposalCooldown");
        if (target.PendingMarriageFromId != 0 && target.MarriageProposalExpiryDay > day)
            return new GameError("MarriageProposalPending");
        actor.Ap -= 2;
        CharacterSocialActions.ApplyRelationship(actor, target.Id, 0, 0);
        actor.Relationships.First(r => r.TargetCharacterId == target.Id).LastMarriageProposalDay = day;
        target.PendingMarriageFromId = actor.Id;
        target.MarriageProposalExpiryDay = day + 30;
        CharacterSocialHistory.Record(actor, target.Id, day, "MarriageProposed", "提出婚约，等待同意");
        CharacterSocialHistory.Record(target, actor.Id, day, "MarriageReceived", "收到婚约，可同意或拒绝");
        message = "婚约已提出，30 日内等待对方同意";
        return GameResult.Ok();
    }

    public static GameResult Decline(GameData data, Character actor, Character target, out string message)
    {
        message = "";
        if (actor.PendingMarriageFromId != target.Id) return new GameError("MarriageProposalMissing");
        actor.PendingMarriageFromId = 0; actor.MarriageProposalExpiryDay = null;
        CharacterSocialHistory.Record(actor, target.Id, data.GameDate.TotalDays, "MarriageDeclined", "拒绝婚约");
        CharacterSocialHistory.Record(target, actor.Id, data.GameDate.TotalDays, "MarriageDeclined", "婚约被拒绝");
        message = "婚约已拒绝";
        return GameResult.Ok();
    }

    internal static void Complete(GameData data, Character a, Character b)
    {
        a.SpouseId = b.Id; b.SpouseId = a.Id;
        a.PendingMarriageFromId = b.PendingMarriageFromId = 0;
        a.MarriageProposalExpiryDay = b.MarriageProposalExpiryDay = null;
        CharacterSocialActions.ApplyRelationship(a, b.Id, 10, 10);
        CharacterSocialActions.ApplyRelationship(b, a.Id, 10, 10);
        CharacterSocialHistory.Record(a, b.Id, data.GameDate.TotalDays, "Married", "双方同意缔结婚姻");
        CharacterSocialHistory.Record(b, a.Id, data.GameDate.TotalDays, "Married", "双方同意缔结婚姻");
    }
}
