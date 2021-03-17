namespace Core.Code
{
    public enum ScenarioMode
    {
        /// <summary>
        /// 故事模式<br/>
        /// 会出现故事剧情的模式<br/>
        /// NPC行动会受到故事限制<br/>
        /// 仅允许单人模式中选择
        /// </summary>
        story,
        /// <summary>
        /// 开放模式<br/>
        /// 会完整显示剧本内容<br/>
        /// 但不会出现故事剧情<br/>
        /// </summary>
        open
    }
}
