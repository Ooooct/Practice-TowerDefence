using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "BulletEffect", menuName = "Buffs/Effects/BulletEffect")]
public class BulletEffect : BuffEffectBase
{
    [SerializeField] private string m_hitStrategyName;
    public override Type GetEventType()
    {
        return typeof(SendBulletDataEvent);
    }

    public override void HandleEvent(BuffInstance instance, IEvent evt)
    {
        // 尝试转换为 SendBulletData 事件
        if (evt is SendBulletDataEvent sendBulletData)
        {
            HandleOnSendBulletData(instance, sendBulletData);
        }
    }

    public override bool IsEventTargetValid(BuffInstance instance, IEvent evt)
    {
        // 检查事件目标是否是拥有此 Buff 的对象
        if (evt is SendBulletDataEvent sendBulletData)
        {
            // 获取拥有者
            IBuffReceiver owner = instance.RootOwner;
            if (owner == null)
            {
                Debug.LogWarning("[BulletBuff] Buff 拥有者为空");
                return false;
            }

            GameObject ownerGameObject = null;

            if (owner is MonoBehaviour mono)
            {
                ownerGameObject = mono.gameObject;
            }
            else
            {
                Debug.LogWarning($"[BulletBuff] 拥有者不是 MonoBehaviour，类型: {owner.GetType().Name}");
            }

            // 比较事件中的敌人对象是否是拥有者
            bool isValid = ownerGameObject != null && sendBulletData.attackData.sourceTower.gameObject == ownerGameObject;
            return isValid;
        }
        return false;
    }

    private void HandleOnSendBulletData(BuffInstance instance, SendBulletDataEvent evt)
    {
        evt.HitStrategies = new List<string>()
        {
            m_hitStrategyName
        };
    }
}
