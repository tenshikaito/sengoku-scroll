using SengokuScroll.Domain;

namespace SengokuScroll.Strategy.Helpers;

public static class EntityEffectExpiryHelper
{
    public static void RemoveExpired(GameData data)
    {
        void Prune(List<SengokuScroll.Domain.Entities.EntityEffect> effects)
            => effects.RemoveAll(e => !CharacterRelationshipRules.IsActive(e, data.GameDate));
        foreach (var character in data.Characters.Values)
        {
            Prune(character.ActiveEffects);
            foreach (var relationship in character.Relationships) Prune(relationship.ViewEffects);
        }
        foreach (var stronghold in data.Strongholds.Values) Prune(stronghold.ActiveEffects);
        foreach (var force in data.Forces.Values)
        {
            Prune(force.ActiveEffects);
            foreach (var diplomacy in force.Diplomacies) Prune(diplomacy.ViewEffects);
        }
    }
}
