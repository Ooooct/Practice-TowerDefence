using System;
using UnityEngine;

/// <summary>
/// Buff 效果基类，负责定义 Buff 的具体行为。
/// <para>【架构说明】BuffEffectBase 作为 ScriptableObject 仅负责配置和定义行为，
/// 不持有运行时状态。运行时的事件注册由 BuffInstance 管理。</para>
/// </summary>
public abstract class BuffEffectBase : ScriptableObject
{
    [Header("事件配置")]
    [SerializeField] protected string m_eventTypeName;
    [SerializeField] protected bool isEventReduceStack = true;
    [SerializeField] protected bool isTickReduceStack = true;

    /// <summary>
    /// 获取需要监听的事件类型。
    /// <para>BuffInstance 会根据此类型自动注册事件。</para>
    /// <para>子类可以重写此方法直接返回类型，避免字符串查找。</para>
    /// </summary>
    public virtual Type GetEventType()
    {
        if (string.IsNullOrEmpty(m_eventTypeName))
        {
            return null;
        }

        // 尝试从当前程序集查找类型
        var type = Type.GetType(m_eventTypeName);
        if (type != null && typeof(IEvent).IsAssignableFrom(type))
        {
            return type;
        }

        // 在所有程序集中查找
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(m_eventTypeName);
            if (type != null && typeof(IEvent).IsAssignableFrom(type))
            {
                return type;
            }
        }

        Debug.LogWarning($"[BuffEffectBase] 未找到事件类型: {m_eventTypeName}");
        return null;
    }

    /// <summary>
    /// Buff 被应用时调用。
    /// </summary>
    public virtual void OnBuffApply(BuffInstance instance)
    {
    }

    /// <summary>
    /// Buff 被移除时调用。
    /// </summary>
    public virtual void OnBuffRemove(BuffInstance instance)
    {
        instance?.UnregisterAllEvents(this);
    }

    /// <summary>
    /// Buff 每帧更新时调用。
    /// </summary>
    public virtual void OnBuffUpdate(BuffInstance instance) { }

    /// <summary>
    /// 周期性触发时调用（当 Buff.tickInterval > 0 时）。
    /// </summary>
    /// <param name="instance">所属的 Buff 实例</param>
    public virtual void OnTick(BuffInstance instance)
    {
        if (isTickReduceStack && instance.CurrentStacks > 0)
        {
            instance.CurrentStacks--;
        }
    }

    /// <summary>
    /// 处理事件的通用方法。
    /// </summary>
    /// <param name="instance">所属的 Buff 实例</param>
    /// <param name="evt">事件对象</param>
    public virtual void HandleEvent(BuffInstance instance, IEvent evt)
    {
        if (isEventReduceStack && instance.CurrentStacks > 0)
        {
            instance.CurrentStacks--;
        }
    }

    /// <summary>
    /// 检查事件目标是否是 Buff 的拥有者
    /// </summary>
    /// <param name="instance">Buff 实例</param>
    /// <param name="evt">事件对象</param>
    /// <returns>如果目标匹配返回 true</returns>
    public virtual bool IsEventTargetValid(BuffInstance instance, IEvent evt)
    {
        // 默认返回 true，子类根据需要重写
        return true;
    }
}
