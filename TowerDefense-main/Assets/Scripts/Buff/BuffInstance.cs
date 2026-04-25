using System;
using System.Collections.Generic;
using UnityEngine;

public class BuffInstance
{
    #region 私有字段
    /// <summary>原始 Buff 配置数据（ScriptableObject）</summary>
    private readonly Buff m_buffData;

    /// <summary>拥有此 Buff 的管理器</summary>
    private readonly BuffManager m_owner;

    /// <summary>Buff 来源对象（可能是 TowerMain、BulletMain 等）</summary>
    private readonly object m_source;

    /// <summary>
    /// 事件注册表：效果 -> 该效果注册的所有事件
    /// </summary>
    private readonly Dictionary<BuffEffectBase, List<EventRegistration>> m_eventRegistrations
        = new Dictionary<BuffEffectBase, List<EventRegistration>>();

    /// <summary>剩余持续时间（秒）</summary>
    private float m_remainingTime;

    /// <summary>当前堆叠层数</summary>
    private int m_currentStacks;

    /// <summary>是否被无效化（Muted）</summary>
    private bool m_isMuted = false;

    /// <summary>周期触发计时器（累积时间）</summary>
    private float m_tickTimer = 0f;
    #endregion

    #region 构造函数
    /// <summary>
    /// 创建一个新的 Buff 运行时实例。
    /// </summary>
    /// <param name="buff">Buff 配置数据（ScriptableObject）</param>
    /// <param name="owner">拥有此 Buff 的管理器</param>
    /// <param name="source">Buff 来源对象（可选，可能是 TowerMain、BulletMain 等）</param>
    public BuffInstance(Buff buff, BuffManager owner, object source = null)
    {
        m_buffData = buff;
        m_owner = owner;
        m_source = source;
        m_remainingTime = buff.duration;
        m_currentStacks = buff.stacks;
    }
    #endregion

    #region 生命周期方法
    /// <summary>
    /// 应用 Buff，激活所有效果。
    /// <para>自动为每个效果注册其需要监听的事件。</para>
    /// </summary>
    public void Apply()
    {
        foreach (var effect in m_buffData.Effects)
        {
            if (effect == null)
            {
                Debug.LogWarning($"[BuffInstance] Buff '{m_buffData.name}' 中存在空的效果");
                continue;
            }

            effect.OnBuffApply(this);

            // 自动注册该效果需要监听的事件
            var eventType = effect.GetEventType();
            if (eventType != null)
            {
                RegisterEventForEffect(effect, eventType);
            }
        }
    }

    /// <summary>
    /// 更新 Buff 状态。
    /// <para>每帧调用，更新所有效果状态并递减持续时间。</para>
    /// </summary>
    /// <param name="deltaTime">时间增量（秒）</param>
    public void Update(float deltaTime)
    {
        // 如果被 Mute，不进行任何更新
        if (m_isMuted)
        {
            return;
        }

        // 周期触发判定（在持续时间递减之前）
        if (m_buffData.tickInterval > 0f)
        {
            m_tickTimer += deltaTime;

            // 检查是否达到周期触发时间
            if (m_tickTimer >= m_buffData.tickInterval)
            {
                // 触发所有效果的 OnTick
                foreach (var effect in m_buffData.Effects)
                {
                    effect?.OnTick(this);
                }

                // 重置计时器（保留超出部分，保证精确周期）
                m_tickTimer -= m_buffData.tickInterval;
            }
        }

        // 【优先级2】更新所有效果
        foreach (var effect in m_buffData.Effects)
        {
            effect?.OnBuffUpdate(this);
        }

        // 【优先级3】持续时间递减
        // 仅当 Buff 有持续时间限制时才递减（duration > 0 表示有限时 Buff）
        if (m_buffData.duration > 0f)
        {
            m_remainingTime -= deltaTime;
        }
    }

    /// <summary>
    /// 移除 Buff，清理所有资源。
    /// </summary>
    public void Remove()
    {
        // 遍历所有效果
        foreach (var effect in m_buffData.Effects)
        {
            if (effect == null)
            {
                continue;
            }

            // 触发效果的移除回调
            effect.OnBuffRemove(this);

            // 清理该效果注册的所有事件
            UnregisterAllEvents(effect);
        }

        // 清空事件注册表
        m_eventRegistrations.Clear();

        Debug.Log($"[BuffInstance] Buff '{m_buffData.name}' 移除完成");
    }

    /// <summary>
    /// 无效化 Buff（暂时停用效果）
    /// </summary>
    public void Mute()
    {
        if (m_isMuted)
        {
            return; // 已经被无效化，不重复执行
        }

        m_isMuted = true;

        // 触发所有效果的移除回调（不取消事件订阅）
        foreach (var effect in m_buffData.Effects)
        {
            effect?.OnBuffRemove(this);
        }

        Debug.Log($"[BuffInstance] Buff '{m_buffData.name}' 被无效化（Muted）");
    }

    /// <summary>
    /// 取消无效化 Buff（重新启用效果）
    /// </summary>
    public void UnMute()
    {
        if (!m_isMuted)
        {
            return; // 未被无效化，不需要恢复
        }

        m_isMuted = false;

        // 重新触发所有效果的应用回调（事件已经注册，无需重新注册）
        foreach (var effect in m_buffData.Effects)
        {
            effect?.OnBuffApply(this);
        }

        Debug.Log($"[BuffInstance] Buff '{m_buffData.name}' 取消无效化（UnMuted）");
    }
    #endregion

    #region 时间管理
    /// <summary>
    /// 刷新 Buff 的持续时间，重置为初始值。
    /// <para>常用于重新应用相同 Buff 时刷新时间。</para>
    /// </summary>
    public void RefreshDuration()
    {
        m_remainingTime = m_buffData.duration;
    }

    /// <summary>
    /// 增加 Buff 的堆叠层数
    /// </summary>
    /// <param name="amount">增加的层数</param>
    public void AddStacks(int amount)
    {
        m_currentStacks += amount;
        if (m_buffData.maxStacks > 0 && m_currentStacks > m_buffData.maxStacks)
        {
            m_currentStacks = m_buffData.maxStacks;
        }
    }
    #endregion

    #region 事件管理
    /// <summary>
    /// 为指定效果注册事件监听。
    /// </summary>
    /// <param name="effect">要注册事件的效果</param>
    /// <param name="eventType">事件类型</param>
    private void RegisterEventForEffect(BuffEffectBase effect, Type eventType)
    {
        if (effect == null || eventType == null)
        {
            Debug.LogWarning("[BuffInstance] RegisterEventForEffect: 效果或事件类型为空");
            return;
        }

        // 使用反射创建泛型方法
        var method = GetType().GetMethod(
            nameof(BindEffectToEvent),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        // 构造泛型方法并调用
        var genericMethod = method.MakeGenericMethod(eventType);
        genericMethod.Invoke(this, new object[] { effect });
    }

    /// <summary>
    /// 为效果注册具体类型的事件。
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="effect">要注册的效果</param>
    private void BindEffectToEvent<T>(BuffEffectBase effect) where T : IEvent
    {

        // 创建事件处理器
        Action<T> handler = (evt) =>
        {
            // 步骤1：验证事件目标
            bool isValid = effect.IsEventTargetValid(this, evt);

            if (!isValid)
            {
                return;
            }

            effect.HandleEvent(this, evt);
        };

        // 向 EventBus 注册
        EventBus.Instance.Subscribe(handler, EventPriority.Early);

        // 记录注册信息以便后续清理
        if (!m_eventRegistrations.TryGetValue(effect, out var registrations))
        {
            registrations = new List<EventRegistration>();
            m_eventRegistrations[effect] = registrations;
        }

        registrations.Add(new EventRegistration(typeof(T), handler));
    }

    /// <summary>
    /// 取消指定效果注册的所有事件。
    /// </summary>
    /// <param name="source">要清理事件的效果</param>
    public void UnregisterAllEvents(BuffEffectBase source)
    {
        if (!m_eventRegistrations.TryGetValue(source, out var registrations))
        {
            return;
        }


        foreach (var registration in registrations)
        {
            UnregisterEventInternal(registration);
        }

        m_eventRegistrations.Remove(source);
    }

    #endregion

    #region 私有方法
    /// <summary>
    /// 内部辅助方法：根据 EventRegistration 中保存的类型信息取消事件订阅。
    /// </summary>
    /// <param name="registration">事件注册记录</param>
    private void UnregisterEventInternal(EventRegistration registration)
    {
        // 获取 Unsubscribe 的泛型方法定义
        var unsubscribeMethod = typeof(EventBus).GetMethod(
            nameof(EventBus.Unsubscribe),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
        );

        // 使用保存的事件类型构造泛型方法
        var genericMethod = unsubscribeMethod.MakeGenericMethod(registration.EventType);
        genericMethod.Invoke(EventBus.Instance, new object[] { registration.Handler });
    }
    #endregion

    #region 公共属性
    /// <summary>原始 Buff 配置数据</summary>
    public Buff BuffData => m_buffData;

    /// <summary>拥有此 Buff 的管理器</summary>
    public BuffManager Owner => m_owner;
    /// <summary>根拥有者</summary>
    public IBuffReceiver RootOwner => m_owner.Owner;

    /// <summary>Buff 来源对象（可能是 TowerMain、BulletMain 等）</summary>
    public object Source => m_source;

    /// <summary>尝试获取 Buff 来源为特定类型</summary>
    public T GetSource<T>() where T : class
    {
        return m_source as T;
    }

    /// <summary>剩余持续时间（秒）</summary>
    public float RemainingTime => m_remainingTime;

    /// <summary>当前堆叠层数</summary>
    public int CurrentStacks
    {
        get
        { return m_currentStacks; }
        set
        {
            m_currentStacks = value;
            if (m_currentStacks == 0)
                Owner.RemoveBuff(m_buffData);
        }
    }

    /// <summary>是否是永久 Buff（duration < 0 表示永久）</summary>
    public bool IsPermanent => m_buffData.duration < 0f;

    /// <summary>Buff 是否已过期（仅对有限时 Buff 有效）</summary>
    public bool IsExpired => m_buffData.duration > 0f && m_remainingTime <= 0f;

    /// <summary>是否被无效化</summary>
    public bool IsMuted => m_isMuted;

    /// <summary>效果列表（直接引用 Buff 配置）</summary>
    public IReadOnlyList<BuffEffectBase> Effects => m_buffData.Effects;
    #endregion

    /// <summary>
    /// 事件注册记录，用于追踪已注册的事件以便后续清理。
    /// </summary>
    private readonly struct EventRegistration
    {
        public EventRegistration(Type eventType, Delegate handler)
        {
            EventType = eventType;
            Handler = handler;
        }

        /// <summary>事件类型（如 OnHitEnemy）</summary>
        public Type EventType { get; }

        /// <summary>事件处理器委托</summary>
        public Delegate Handler { get; }
    }
}
