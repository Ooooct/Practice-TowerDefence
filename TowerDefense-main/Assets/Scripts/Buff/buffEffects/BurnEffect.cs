using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BurnEffect", menuName = "Buffs/Effects/BurnEffect")]
public class BurnEffect : BuffEffectBase
{
    [Header("燃烧伤害配置")]
    [Tooltip("每次 Tick 造成的真实伤害")]
    [SerializeField] private float m_burnDamage = 5f;

    public override void OnTick(BuffInstance instance)
    {
        // 获取 Buff 目标（受到燃烧的敌人）
        IBuffReceiver target = instance.RootOwner;
        if (target == null)
        {
            Debug.LogError("[BurnEffect] Buff 目标为空");
            return;
        }

        GameObject targetObject = null;
        if (target is MonoBehaviour mono)
        {
            targetObject = mono.gameObject;
        }

        if (targetObject == null)
        {
            Debug.LogWarning("[BurnEffect] 无法获取目标 GameObject，燃烧Buff伤害未能被统计");
            return;
        }

        // 尝试从 BuffInstance 获取来源塔
        TowerMain sourceTower = instance.GetSource<TowerMain>();

        // 构造攻击数据
        AttackData attackData = new AttackData(m_burnDamage, 0f, sourceTower);

        // 发布命中事件（燃烧伤害）
        HitEnemyEvent hitEvent = new HitEnemyEvent(
            attackData: attackData,
            enemyObject: targetObject,
            callerBullet: null, // 燃烧伤害不是子弹造成的
            isClearBullet: false
        );

        EventBus.Instance.Publish(hitEvent);
        base.OnTick(instance);
    }
}
