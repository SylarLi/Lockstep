public enum LockStepState
{
    /// <summary>
    /// 无
    /// </summary>
    None,

    /// <summary>
    /// 准备状态：只发送玩家操作，不播放
    /// </summary>
    Prepare,

    /// <summary>
    /// 开始状态：发送和播放
    /// </summary>
    Start,
}
