using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthHandler : EnemyComponentBase
{
    #region 事件
    /// <summary>
    /// 生命值变化事件 (当前生命值, 最大生命值)
    /// </summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>
    /// 护甲值变化事件 (当前护甲, 最大生命值)
    /// </summary>
    public event Action<float, float> OnShieldChanged;
    #endregion

    #region 变量
    float m_maxHealth = 1;
    float m_currentHealth = 1;
    float m_shield;
    #endregion

    #region 属性
    public float MaxHealth
    {
        get => m_maxHealth;
        set
        {
            m_maxHealth = value;
            if (CurrentHealth > m_maxHealth)
            {
                CurrentHealth = m_maxHealth;
            }
            OnHealthChanged?.Invoke(m_currentHealth, m_maxHealth);
        }
    }

    public float CurrentHealth
    {
        get => m_currentHealth;
        set
        {
            m_currentHealth = value;
            m_currentHealth = Mathf.Clamp(m_currentHealth, 0, m_maxHealth);
            OnHealthChanged?.Invoke(m_currentHealth, m_maxHealth);
        }
    }

    public float Shield
    {
        get { return m_shield; }
        set
        {
            m_shield = Mathf.Max(0, value);
            m_shield = Mathf.Min(m_shield, m_maxHealth);
            OnShieldChanged?.Invoke(m_shield, m_maxHealth);
        }
    }
    #endregion

    #region Unity 生命周期
    public override void Start()
    {
        base.Start();
        CurrentHealth = m_maxHealth;
        OnHealthChanged?.Invoke(m_currentHealth, m_maxHealth);
        OnShieldChanged?.Invoke(m_shield, m_maxHealth);
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        // 清空所有事件订阅
        OnHealthChanged = null;
        OnShieldChanged = null;
    }
    #endregion

    #region Public 方法
    public void TakeDamage(float damage)
    {
        float effectiveDamage = damage - m_shield;
        float oldShield = m_shield;
        m_shield -= damage; // 护盾吸收伤害

        // 确保护盾和有效伤害不为负值
        m_shield = Mathf.Max(m_shield, 0);
        effectiveDamage = Mathf.Max(effectiveDamage, 0);

        // 触发护甲变化事件
        if (oldShield != m_shield)
        {
            OnShieldChanged?.Invoke(m_shield, m_maxHealth);
        }

        CurrentHealth -= effectiveDamage;
        //Debug.Log($"{m_enemy.name} 受到 {effectiveDamage} 点伤害，当前生命值：{m_currentHealth}");

        if (m_currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
        //Debug.Log($"{m_enemy.name} 恢复 {amount} 点生命值，当前生命值：{m_currentHealth}");
    }
    #endregion

    #region Private 方法
    void Die()
    {
        // 处理敌人死亡逻辑

        // TODO 如果有空余时间可以将金钱奖励的代码解耦出去
        CostManager.Instance.AddGold(m_enemy.GoldReward);
        PoolManager.Instance.Recycle(m_enemy);
    }
    #endregion
}
