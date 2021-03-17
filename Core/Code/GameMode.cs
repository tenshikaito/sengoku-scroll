namespace Core.Code
{
    public enum GameMode
    {
        /// <summary>
        /// 个人模式<br/>
        /// 游戏时间将按照玩家自己的节奏流逝<br/>
        /// 不需要手动调整时间流逝速度（事件驱动）<br/>
        /// 中途不可以变更模式加入其它玩家
        /// </summary>
        personal,
        /// <summary>
        /// 公共模式<br/>
        /// 游戏时间平均流逝<br/>
        /// 中途可以变更模式加入其它玩家
        /// </summary>
        @public
    }
}
