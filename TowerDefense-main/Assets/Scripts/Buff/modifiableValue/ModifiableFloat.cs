using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可修改的浮点数值，支持多种修改器叠加。
/// <para>【计算顺序】基础值 → 加算 → 百分比加算 → 最终乘算</para>
/// <para>公式：finalValue = (baseValue + addValue) * (1 + percentageAddValue) * multiplyValue</para>
/// </summary>
public class ModifiableFloat
{
    private float m_baseValue;
    private List<Modifier> m_modifiers;
    private float m_cachedValue;
    private Action<float> m_onValueChanged; // 值变化回调

    /// <summary>
    /// 创建一个 ModifiableFloat 实例。
    /// </summary>
    /// <param name="baseValue">基础值</param>
    public ModifiableFloat(float baseValue)
    {
        m_baseValue = baseValue;
        m_modifiers = new List<Modifier>();
        m_cachedValue = baseValue;
        m_onValueChanged = null;
    }

    /// <summary>
    /// 创建一个默认的 ModifiableFloat（基础值为 0）。
    /// </summary>
    public static ModifiableFloat Default => new ModifiableFloat(0f);

    #region 公共属性
    /// <summary>
    /// 获取或设置基础值。
    /// <para>设置基础值时会立即重新计算。</para>
    /// </summary>
    public float BaseValue
    {
        get => m_baseValue;
        set
        {
            if (!Mathf.Approximately(m_baseValue, value))
            {
                m_baseValue = value;
                RecalculateValue();
            }
        }
    }

    /// <summary>
    /// 获取最终修改后的值。
    /// <para>返回缓存的计算结果，无额外开销。</para>
    /// </summary>
    public float Value => m_cachedValue;

    /// <summary>当前修改器数量</summary>
    public int ModifierCount => m_modifiers?.Count ?? 0;

    /// <summary>
    /// 设置值变化回调。
    /// <para>当值发生变化时会调用此回调，参数为新的值。</para>
    /// </summary>
    public Action<float> OnValueChanged
    {
        get => m_onValueChanged;
        set => m_onValueChanged = value;
    }
    #endregion

    #region 修改器管理
    /// <summary>
    /// 添加一个修改器。
    /// <para>添加后会立即重新计算最终值。</para>
    /// </summary>
    /// <param name="modifier">要添加的修改器</param>
    public void AddModifier(Modifier modifier)
    {
        if (m_modifiers == null)
        {
            m_modifiers = new List<Modifier>();
        }

        m_modifiers.Add(modifier);
        RecalculateValue();
    }

    /// <summary>
    /// 移除一个修改器。
    /// <para>移除后会立即重新计算最终值。</para>
    /// </summary>
    /// <param name="modifier">要移除的修改器</param>
    /// <returns>如果成功移除返回 true</returns>
    public bool RemoveModifier(Modifier modifier)
    {
        if (m_modifiers == null)
        {
            return false;
        }

        bool removed = m_modifiers.Remove(modifier);
        if (removed)
        {
            RecalculateValue();
        }
        return removed;
    }

    /// <summary>
    /// 移除所有修改器。
    /// <para>移除后会立即重新计算最终值。</para>
    /// </summary>
    public void ClearModifiers()
    {
        if (m_modifiers == null)
        {
            return;
        }

        int count = m_modifiers.Count;
        m_modifiers.Clear();
        if (count > 0)
        {
            RecalculateValue();
        }
    }
    #endregion

    #region 内部计算
    /// <summary>
    /// 重新计算最终值。
    /// <para>【计算步骤】</para>
    /// <list type="number">
    /// <item>从基础值开始</item>
    /// <item>累加所有 addValue（加算）</item>
    /// <item>应用所有 percentageAddValue（百分比加算）</item>
    /// <item>累乘所有 multiplyValue（最终乘算）</item>
    /// </list>
    /// </summary>
    private void RecalculateValue()
    {
        float result = m_baseValue;

        // 如果没有修改器，直接使用基础值
        if (m_modifiers == null || m_modifiers.Count == 0)
        {
            m_cachedValue = result;
            // 触发值变化回调
            m_onValueChanged?.Invoke(m_cachedValue);
            return;
        }

        // 步骤1：累加所有修改器的各个分量
        float totalAdd = 0f;
        float totalPercentageAdd = 0f;
        float totalMultiply = 1f;

        foreach (var modifier in m_modifiers)
        {
            totalAdd += modifier.addValue;
            totalPercentageAdd += modifier.percentageAddValue;
            totalMultiply *= modifier.multiplyValue;
        }

        // 步骤2：应用加算
        result += totalAdd;

        // 步骤3：应用百分比加算
        result *= (1f + totalPercentageAdd);

        // 步骤4：应用最终乘算
        result *= totalMultiply;

        m_cachedValue = result;

        // 触发值变化回调
        m_onValueChanged?.Invoke(m_cachedValue);
    }
    #endregion
}