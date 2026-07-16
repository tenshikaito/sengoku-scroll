using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Battle;

/// <summary>将领装备（WeaponId / ArmorId）对战力的修正。</summary>
public static class BattleEquipmentRules
{
    /// <summary>将领所持武器与护甲按品阶折算战力倍率，写入攻防修正明细。</summary>
    public static void ApplyCommanderEquipment(Character? commander, bool isAttacker, BattleFactorBreakdown b)
    {
        if (commander is null)
            return;

        var weaponScale = ResolveWeaponScale(commander.WeaponId);
        var armorScale = ResolveArmorScale(commander.ArmorId);

        if (weaponScale == 1.0 && armorScale == 1.0)
            return;

        if (isAttacker)
            b.AttackerPowerScale *= weaponScale * armorScale;
        else
            b.DefenderPowerScale *= weaponScale * armorScale;

        b.Add(
            "equipment",
            $"装备（武{commander.WeaponId}/甲{commander.ArmorId}）",
            isAttacker ? 2 : 0,
            isAttacker ? 0 : 2,
            commander.Name);
    }

    // 业务：武器品阶分四档（无/低/中/高），最高档 +12% 战力
    private static double ResolveWeaponScale(int weaponId)
        => weaponId switch
        {
            0 => 1.0,
            <= 3 => 1.04,
            <= 6 => 1.08,
            _ => 1.12
        };

    // 业务：护甲品阶分四档，最高档 +9% 战力（略低于武器）
    private static double ResolveArmorScale(int armorId)
        => armorId switch
        {
            0 => 1.0,
            <= 3 => 1.03,
            <= 6 => 1.06,
            _ => 1.09
        };
}
