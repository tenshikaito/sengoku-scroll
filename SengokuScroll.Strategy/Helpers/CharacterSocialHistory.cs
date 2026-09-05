using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Helpers;

public static class CharacterSocialHistory
{
    public const int Capacity = 64;
    public static void Record(Character character, int otherId, int day, string kind, string description)
    {
        character.SocialMemories.Add(new(character.NextSocialMemoryId++, day, otherId, kind, description));
        if (character.SocialMemories.Count > Capacity)
            character.SocialMemories.RemoveRange(0, character.SocialMemories.Count - Capacity);
    }
}
