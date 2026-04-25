using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 事件接口，所有事件结构体都应该实现此接口
/// </summary>
public interface IEvent { }

/// <summary>
/// 事件优先级枚举
/// </summary>
public enum EventPriority
{
    Early = 0,   // 最先执行
    Default = 1, // 默认执行顺序
    Late = 2     // 最后执行
}

/// <summary>
/// 事件处理器包装类，用于存储优先级信息
/// </summary>
internal class EventHandler<T> where T : IEvent
{
    public Action<T> Handler { get; set; }
    public EventPriority Priority { get; set; }

    public EventHandler(Action<T> handler, EventPriority priority)
    {
        Handler = handler;
        Priority = priority;
    }
}

/// <summary>
/// 事件总线系统，用于游戏内事件的订阅、发布和取消订阅
/// 支持事件优先级（Early, Default, Late）和防重复订阅
/// </summary>
public class EventBus : MonoBehaviour
{
    #region 变量
    private static EventBus m_instance;
    // 字典存储每种事件类型的处理器列表
    private Dictionary<Type, object> m_eventHandlers = new Dictionary<Type, object>();
    #endregion

    #region 属性
    public static EventBus Instance
    {
        get
        {
            return m_instance;
        }
    }
    #endregion

    #region Unity 生命周期
    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Public 方法

    /// <summary>
    /// 订阅事件（默认优先级）
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现 IEvent 接口</typeparam>
    /// <param name="handler">事件处理器</param>
    public void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        Subscribe(handler, EventPriority.Default);
    }

    /// <summary>
    /// 订阅事件（指定优先级）
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现 IEvent 接口</typeparam>
    /// <param name="handler">事件处理器</param>
    /// <param name="priority">事件优先级</param>
    public void Subscribe<T>(Action<T> handler, EventPriority priority) where T : IEvent
    {
        if (handler == null)
        {
            Debug.LogWarning("[EventBus] - 尝试订阅空的事件处理器");
            return;
        }

        Type eventType = typeof(T);

        // 获取或创建该事件类型的处理器列表
        if (!m_eventHandlers.ContainsKey(eventType))
        {
            m_eventHandlers[eventType] = new List<EventHandler<T>>();
        }

        List<EventHandler<T>> handlers = m_eventHandlers[eventType] as List<EventHandler<T>>;

        // 检查是否已经订阅过（防止重复订阅）
        if (handlers.Exists(h => h.Handler == handler))
        {
            Debug.LogWarning($"[EventBus] - 事件 {eventType.Name} 的处理器已订阅，忽略重复订阅");
            return;
        }

        // 添加新的处理器
        handlers.Add(new EventHandler<T>(handler, priority));

        // 按优先级排序
        handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现 IEvent 接口</typeparam>
    /// <param name="handler">要移除的事件处理器</param>
    public void Unsubscribe<T>(Action<T> handler) where T : IEvent
    {
        if (handler == null)
        {
            Debug.LogWarning("[EventBus] - 尝试取消订阅空的事件处理器");
            return;
        }

        Type eventType = typeof(T);

        // 检查是否有该事件类型的订阅
        if (m_eventHandlers.ContainsKey(eventType))
        {
            List<EventHandler<T>> handlers = m_eventHandlers[eventType] as List<EventHandler<T>>;

            // 移除指定的处理器
            int removed = handlers.RemoveAll(h => h.Handler == handler);

            if (removed > 0)
            {

                // 如果没有订阅者了，移除该事件类型
                if (handlers.Count == 0)
                {
                    m_eventHandlers.Remove(eventType);
                }
            }
            else
            {
                Debug.LogWarning($"[EventBus] - 未找到要取消订阅的事件处理器: {eventType.Name}");
            }
        }
        else
        {
            Debug.LogWarning($"[EventBus] - 尝试取消订阅不存在的事件: {eventType.Name}");
        }
    }

    /// <summary>
    /// 发布事件，按优先级触发所有订阅了该事件的处理器
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现 IEvent 接口</typeparam>
    /// <param name="eventData">事件数据</param>
    public void Publish<T>(T eventData) where T : IEvent
    {
        Type eventType = typeof(T);

        // 检查是否有订阅者
        if (m_eventHandlers.ContainsKey(eventType))
        {
            List<EventHandler<T>> handlers = m_eventHandlers[eventType] as List<EventHandler<T>>;

            if (handlers != null && handlers.Count > 0)
            {
                List<EventHandler<T>> handlersCopy = new List<EventHandler<T>>(handlers);

                // 按优先级顺序调用所有处理器
                foreach (var eventHandler in handlersCopy)
                {
                    eventHandler.Handler?.Invoke(eventData);
                }
            }
        }
    }

    /// <summary>
    /// 获取当前注册的事件类型数量
    /// </summary>
    public int GetEventCount()
    {
        return m_eventHandlers.Count;
    }

    /// <summary>
    /// 获取指定事件类型的订阅者数量
    /// </summary>
    public int GetSubscriberCount<T>() where T : IEvent
    {
        Type eventType = typeof(T);
        if (m_eventHandlers.ContainsKey(eventType))
        {
            var handlers = m_eventHandlers[eventType] as List<EventHandler<T>>;
            return handlers?.Count ?? 0;
        }
        return 0;
    }
    #endregion

    #region Unity 生命周期
    private void OnDestroy()
    {
        m_eventHandlers.Clear();
    }
    #endregion
}