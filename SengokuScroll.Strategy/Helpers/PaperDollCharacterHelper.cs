using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>纸娃娃角色：为城中商户/寺社等随机生成可后续替换为具名 NPC 的真实 Character。</summary>
public static class PaperDollCharacterHelper
{
    private static readonly string[] FamilyNames =
    [
        "藤原", "源", "平", "安倍", "橘", "大江", "菅原", "纪", "中原", "秦",
        "佐藤", "铃木", "高桥", "田中", "渡边", "伊藤", "山本", "中村", "小林", "加藤",
    ];

    private static readonly string[] GivenNames =
    [
        "信平", "义朝", "忠信", "正信", "盛政", "长政", "康政", "信政", "重政", "定政",
        "宗久", "算长", "吉次", "新七", "作左卫门", "与一", "半藏", "勘助", "久秀", "信玄",
        "晴信", "义元", "元信", "信长", "信忠", "胜家", "利家", "秀吉", "秀赖", "家康",
    ];

    public static int EnsurePaperDollCharacter(
        GameData gameData,
        Stronghold stronghold,
        int characterId,
        int forceId,
        string seedLabel,
        int? religionId = null)
    {
        var name = GeneratePaperDollName(characterId, seedLabel);
        return CityActorDemoRosterHelper.EnsureCharacter(
            gameData,
            stronghold,
            characterId,
            name,
            forceId,
            religionId);
    }

    public static string GeneratePaperDollName(int characterId, string seedLabel)
    {
        var seed = DeterministicHash.Combine(seedLabel, characterId);
        var family = FamilyNames[seed % FamilyNames.Length];
        var given = GivenNames[seed / FamilyNames.Length % GivenNames.Length];
        return $"{family}{given}";
    }
}
