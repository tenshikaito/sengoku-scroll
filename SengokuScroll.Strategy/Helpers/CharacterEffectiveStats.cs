using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>生病时能力与技能减半后的有效数值。</summary>
public static class CharacterEffectiveStats
{
    public static byte Leadership(Character character) => ScaleByte(character.Leadership, character);
    public static byte Power(Character character) => ScaleByte(character.Power, character);
    public static byte Politics(Character character) => ScaleByte(character.Politics, character);
    public static byte Strategy(Character character) => ScaleByte(character.Strategy, character);
    public static byte Charm(Character character) => ScaleByte(character.Charm, character);

    public static int SkillLevel(int level, Character character)
        => character.IsSick ? Math.Max(1, level / 2) : level;

    private static byte ScaleByte(byte value, Character character)
        => character.IsSick ? (byte)Math.Max(1, value / 2) : value;
}
