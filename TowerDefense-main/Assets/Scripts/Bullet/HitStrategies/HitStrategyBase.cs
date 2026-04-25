using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 命中策略基类，提供共有的目标验证和事件发布逻辑
/// 组合模式：策略拥有完整控制权，包括重复命中判定、回收逻辑等
/// </summary>
public abstract class HitStrategyBase : ScriptableObject, IHitStrategy
{
    [Header("Target Configuration")]
    [SerializeField] protected string m_targetTag = "Enemy";


    #region 接口实现
    /// <summary>
    /// 处理触发器碰撞（模板方法）
    /// </summary>
    public void HandleTriggerEnter(Collider triggerCollider, BulletMain bullet)
    {
        if (bullet == null)
        {
            Debug.LogWarning("[HitStrategyBase] - 子弹实例为空");
            return;
        }
        if (CheckTag(triggerCollider) == false) return;

        // 子类实现具体的命中处理逻辑
        ProcessHit(triggerCollider, bullet);
    }

    /// <summary>
    /// 子弹回收时的清理
    /// </summary>
    public virtual void OnRecycle(BulletMain bullet)
    {
        if (bullet == null) return;

        // 子类可重写以实现额外清理
        OnRecycleCustom(bullet);
    }
    #endregion

    #region 抽象方法（子类必须实现）
    /// <summary>
    /// 处理命中逻辑（子类实现）
    /// </summary>
    protected abstract void ProcessHit(Collider triggerCollider, BulletMain bullet);
    #endregion

    #region 受保护的工具方法（供子类使用）
    /// <summary>
    /// 检查标签是否匹配
    /// </summary>
    protected bool CheckTag(Collider collider)
    {
        if (string.IsNullOrEmpty(m_targetTag))
            return true;

        return collider.CompareTag(m_targetTag);
    }

    /// <summary>
    /// 验证目标是否有效（可被子类重写）
    /// </summary>
    protected virtual bool IsValidTarget(GameObject target)
    {
        if (target == null)
            return false;

        // 默认检查是否有 EnemyMain 组件
        return target.GetComponent<EnemyMain>() != null;
    }

    /// <summary>
    /// 发布命中事件
    /// </summary>
    protected void PublishHitEvent(GameObject target, AttackData attackData, BulletMain bullet = null, bool requestRecycle = true)
    {
        var hitEvent = new HitEnemyEvent(attackData, target, bullet, requestRecycle);
        EventBus.Instance.Publish(hitEvent);
    }

    /// <summary>
    /// 子类可重写以实现自定义回收清理逻辑
    /// </summary>
    protected virtual void OnRecycleCustom(BulletMain bullet)
    {
        // 默认不做任何事
    }
    #endregion
}
