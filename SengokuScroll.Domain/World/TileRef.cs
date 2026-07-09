namespace SengokuScroll.Domain.World;

public readonly ref struct TileRef(ref byte terrain, ref byte region)
{
    public readonly ref byte Terrain = ref terrain;

    public readonly ref byte Region = ref region;
}