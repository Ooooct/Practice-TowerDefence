using System;
using UnityEngine;

/// <summary>
/// 护盾 Buff
/// </summary>
[CreateAssetMenu(fileName = "ShieldEffect", menuName = "Buffs/Effects/Shield")]
public class ShieldEffect : BuffEffectBase
{
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
                Debug.LogWarning("[ShieldBuff] Buff 拥有者为空");
                return false;
            }

            GameObject ownerGameObject = null;

            if (owner is MonoBehaviour mono)
            {
                ownerGameObject = mono.gameObject;
            }
            else
            {
                Debug.LogWarning($"[ShieldBuff] 拥有者不是 MonoBehaviour，类型: {owner.GetType().Name}");
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
            Debug.LogWarning("[ShieldBuff] 事件中的敌人对象为空");
            return;
        }

        // 免疫伤害
        float originalDamage = evt.attackData.damage;
        evt.attackData.damage = 0;
        Debug.Log($"[ShieldBuff] 护盾生效！伤害从 {originalDamage} 减少到 0，剩余层数: {instance.CurrentStacks}");
        instance.CurrentStacks--;
    }
}

