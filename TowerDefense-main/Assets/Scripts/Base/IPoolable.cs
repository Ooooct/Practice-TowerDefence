using UnityEngine;

/// <summary>
/// 可池化对象接口，所有需要使用对象池的对象都应该实现此接口。
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 当对象从对象池中被取出时调用。
    /// 用于重置对象状态。
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// 当对象返回到对象池时调用。
    /// 用于清理和禁用对象。
    /// </summary>
    void OnRecycle();

    /// <summary>
    /// 获取对象的GameObject引用。
    /// </summary>
    GameObject GameObject { get; }
}
