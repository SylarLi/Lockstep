using System.Collections.Generic;

public interface ILSDataBuffer<T1, T2> : IPipe where T1 : ILSDataList<T2> where T2 : ILSData
{
    /// <summary>
    /// 服务器发送来的LockStep Buffer数据
    /// </summary>
    List<T1> buffer { get; }

    /// <summary>
    /// 合并从服务器发送而来的LockStep Buffer数据
    /// </summary>
    /// <param name="datas"></param>
    void Merge(ILSDataBuffer<T1, T2> lsbf);
}
