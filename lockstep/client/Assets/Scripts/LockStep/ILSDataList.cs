using System.Collections.Generic;

public interface ILSDataList<T> where T : ILSData
{
    /// <summary>
    /// 某帧所有角色的LockStep数据
    /// 其中List的index代表角色的序号
    /// </summary>
    List<T> datas { get; }
}
