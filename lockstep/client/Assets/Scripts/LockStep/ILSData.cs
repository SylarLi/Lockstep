/// <summary>
/// LockStep帧角色输入数据
/// </summary>
public interface ILSData : IPipe
{
    /// <summary>
    /// 帧序号
    /// </summary>
    RUShortInt frame { get; }
}
