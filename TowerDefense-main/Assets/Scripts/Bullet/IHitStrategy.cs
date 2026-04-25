using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 命中策略接口，定义子弹命中目标时的行为
/// 组合模式：策略对象拥有完整的控制权，包括回收、重复命中判定等
/// </summary>
public interface IHitStrategy
{
    /// <summary>
    /// 处理触发器碰撞（完整控制）
    /// </summary>
    /// <param name="triggerCollider">触发碰撞的碰撞体</param>
    /// <param name="bullet">子弹实例</param>
    void HandleTriggerEnter(Collider triggerCollider, BulletMain bullet);

    /// <summary>
    /// 子弹回收时的清理逻辑
    /// </summary>
    /// <param name="bullet">被回收的子弹实例</param>
    void OnRecycle(BulletMain bullet);
}
