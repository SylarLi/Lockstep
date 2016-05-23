public class NetConfig
{
    /// <summary>
    /// 与Unity的Fixed TimeStep设置相同
    /// </summary>
    public const float TPF = 0.0333333f;

    public const float ConnnectTimeout = 1;
    public const float MaxConnectTimes = 5;

    public const float BreakTimeout = 5;

    public const float BeatInterval = 1;

    public const float LossRTT = 1;

    /// <summary>
    /// Client Played Delay Buffer Frame Length
    /// Client表现平滑条件 PDBL_Client.Time > PDBL_Server.Time + RTT
    /// </summary>
    public const short PDBL = 6;
}
