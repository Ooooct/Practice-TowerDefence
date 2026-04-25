using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单体命中策略：只命中触发碰撞的目标，命中后立即回收子弹
/// </summary>
[CreateAssetMenu(fileName = "SingleTargetHitStrategy", menuName = "TowerDefense/HitStrategies/SingleTarget")]
public class SingleTargetHitStrategy : HitStrategyBase
{
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

        // 发布命中事件
        PublishHitEvent(target, bullet.AttackData, bullet);
    }
}
