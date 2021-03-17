namespace Core.Code
{
    public enum StartMode
    {
        /// <summary>
        /// 剧情模式<br/>
        /// 会出现故事剧情的模式<br/>
        /// 会以地图数据随机在地图上生成游戏内容
        /// </summary>
        scenario,
        /// <summary>
        /// 随机模式<br/>
        /// 不会加载剧本内容<br/>
        /// 会以地图数据随机在地图上生成游戏内容
        /// </summary>
        random,
        /// <summary>
        /// 创世模式<br/>
        /// 不会加载剧本内容<br/>
        /// 地图内容为空白
        /// </summary>
        creation
    }
}
