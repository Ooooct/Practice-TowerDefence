using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 穿透命中策略：可以命中多个目标，达到上限后回收子弹
/// </summary>
[CreateAssetMenu(fileName = "PenetrateHitStrategy", menuName = "TowerDefense/HitStrategies/Penetrate")]
public class PenetrateHitStrategy : HitStrategyBase
{
    [Header("Penetrate Configuration")]
    [SerializeField] private int m_maxPenetrateCount = 5; // 最大穿透目标数

    // 记录每个子弹实例的命中次数
    private Dictionary<int, int> m_bulletHitCounts = new Dictionary<int, int>();

    protected override void ProcessHit(Collider triggerCollider, BulletMain bullet)
    {
        // 标签过滤
        if (!CheckTag(triggerCollider))
        {
            return;
        }

        GameObject target = triggerCollider.gameObject;

        // 验证目标有效性
        if (!IsValidTarget(target))
        {
            return;
        }

        int bulletID = bullet.GameObject.GetInstanceID();

        // 初始化命中计数
        if (!m_bulletHitCounts.ContainsKey(bulletID))
        {
            m_bulletHitCounts[bulletID] = 0;
        }

        // 检查是否达到上限
        if (m_bulletHitCounts[bulletID] >= m_maxPenetrateCount)
        {
            PublishHitEvent(target, bullet.AttackData, bullet);
            return;
        }

        // 记录命中
        m_bulletHitCounts[bulletID]++;

        // 发布命中事件（不标记回收）
        PublishHitEvent(target, bullet.AttackData, bullet, false);

        // 如果达到上限，请求回收子弹（通过事件）
        if (m_bulletHitCounts[bulletID] >= m_maxPenetrateCount)
        {
            PublishHitEvent(target, bullet.AttackData, bullet);
        }
        // 否则子弹继续飞行，等待下一次碰撞
    }

    /// <summary>
    /// 清理穿透计数数据
    /// </summary>
    protected override void OnRecycleCustom(BulletMain bullet)
    {
        int bulletID = bullet.GameObject.GetInstanceID();
        if (m_bulletHitCounts.ContainsKey(bulletID))
        {
            m_bulletHitCounts.Remove(bulletID);
        }
    }

    #region 运行时配置
    public void SetMaxPenetrateCount(int count) => m_maxPenetrateCount = Mathf.Max(1, count);
    #endregion
}
