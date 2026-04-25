using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "AddCritRateEffect", menuName = "Buffs/Effects/AddCritRateEffect")]
public class AddCritRateEffect : BuffEffectBase
{
    [SerializeField] private float m_critRateIncrease = 0.25f;
    public override Type GetEventType()
    {
        return typeof(HitEnemyEvent);
    }

    public override void HandleEvent(BuffInstance instance, IEvent evt)
    {
        // 尝试转换为 OnHitEnemy 事件
        if (evt is HitEnemyEvent hitEvent)
        {
            HandleOnHitEnemy(instance, hitEvent);
        }
        base.HandleEvent(instance, evt);
    }

    public override bool IsEventTargetValid(BuffInstance instance, IEvent evt)
    {
        // 检查事件目标是否是拥有此 Buff 的对象
        if (evt is HitEnemyEvent hitEvent)
        {
            // 获取拥有者
            IBuffReceiver owner = instance.Owner.Owner;
            if (owner == null)
            {
                Debug.LogWarning("[AddCritRateEffect] Buff 拥有者为空");
                return false;
            }

            GameObject ownerGameObject = null;

            if (owner is MonoBehaviour mono)
            {
                ownerGameObject = mono.gameObject;
            }
            else
            {
                Debug.LogWarning($"[AddCritRateEffect] 拥有者不是 MonoBehaviour，类型: {owner.GetType().Name}");
            }

            // 比较事件中的敌人对象是否是拥有者
            bool isValid = ownerGameObject != null && hitEvent.enemyObject == ownerGameObject;
            return isValid;
        }
        return false;
    }

    /// <summary>
    /// 处理命中敌人事件的具体逻辑。
    /// </summary>
    private void HandleOnHitEnemy(BuffInstance instance, HitEnemyEvent evt)
    {
        if (evt.enemyObject == null)
        {
            Debug.LogWarning("[AddCritRateEffect] 事件中的敌人对象为空");
            return;
        }

        // 免疫伤害
        float originalCritRate = evt.attackData.critRate;
        evt.attackData.critRate += m_critRateIncrease;
        Debug.Log($"[AddCritRateEffect] 暴击率从 {originalCritRate} 增加到 {evt.attackData.critRate}，剩余层数: {instance.CurrentStacks}");
    }
}