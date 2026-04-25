using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生命回复 Buff 效果
/// 每次 Tick 为目标回复指定的生命值
/// </summary>
[CreateAssetMenu(fileName = "HealthRecoverEffect", menuName = "Buffs/Effects/HealthRecover")]
public class HealthRecoverEffect : BuffEffectBase
{
    [Header("回复配置")]
    [Tooltip("每次 Tick 回复的生命值")]
    [SerializeField] private float m_recoverAmount = 10f;

    public override void OnTick(BuffInstance instance)
    {
        // 获取 Buff 目标（需要回复生命的敌人）
        IBuffReceiver target = instance.RootOwner;
        if (target == null)
        {
            Debug.LogError("[HealthRecoverEffect] Buff 目标为空");
            return;
        }

        HealthHandler healthHandler = target.GetObjectComponent<HealthHandler>();
        if (healthHandler == null)
        {
            Debug.LogWarning($"[HealthRecoverEffect] 敌人没有 HealthHandler 组件");
            return;
        }

        // 计算回复量
        float currentHealth = healthHandler.CurrentHealth;
        float finalRecoverAmount = m_recoverAmount;

        // 回复生命值
        if (finalRecoverAmount > 0)
        {
            healthHandler.Heal(finalRecoverAmount);
        }

        base.OnTick(instance);
    }
}
