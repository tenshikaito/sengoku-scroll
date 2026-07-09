namespace SengokuScroll.Domain.Entities.Types;

public enum TerrainFeatureType : byte
{
    /// <summary>
    /// 草地
    /// </summary>
    /// <remarks>
    /// <para>温度偏低及以上湿度中等及以上会变化</para>
    /// <para>对畜牧业有加成</para>
    /// <para>减少骑兵移动消耗</para>
    /// </remarks>
    Grass,
    /// <summary>
    /// 树林
    /// </summary>
    /// <remarks>
    /// <para>会严重阻碍骑兵移动</para>
    /// <para>器械完全无法移动</para>
    /// <para>可能被火烧消失也会随机重新生长</para>
    /// <para>可在森林伏兵</para>
    /// </remarks>
    Forest,
}