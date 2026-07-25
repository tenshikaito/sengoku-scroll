namespace SengokuScroll.Domain.Entities.Types;

/// <summary>地图单位种类：决定菜单、战斗与贸易规则。</summary>
public enum UnitKind : byte
{
    Military = 0,
    Convoy = 1,
    Merchant = 2,
    Migrant = 3,
}
