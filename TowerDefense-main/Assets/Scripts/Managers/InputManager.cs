using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 输入管理器，处理鼠标点击和移动事件
/// </summary>
public class InputManager : MonoBehaviour
{
    #region 变量
    private Vector3 m_lastMousePosition;
    private bool m_isInputEnabled = true;
    #endregion

    #region Unity 生命周期
    private void Start()
    {
        // 订阅回放模式变化事件
        EventBus.Instance.Subscribe<ReplayModeChangedEvent>(OnReplayModeChanged);
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<ReplayModeChangedEvent>(OnReplayModeChanged);
        }
    }

    private void Update()
    {
        // 如果输入被禁用（回放模式），不处理输入
        if (!m_isInputEnabled)
        {
            return;
        }

        // 左键点击事件
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 clickPosition = Input.mousePosition;
            EventBus.Instance.Publish(new LeftClickEvent(clickPosition));
        }

        // 右键点击事件
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 clickPosition = Input.mousePosition;
            EventBus.Instance.Publish(new RightClickEvent(clickPosition));
        }

        // ·/` 键切换 DebugOverlay（不受回放模式影响）
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            EventBus.Instance.Publish(new ToggleDebugOverlayEvent());
        }

        // 鼠标移动事件
        Vector3 mousePosition = Input.mousePosition;
        if (mousePosition != m_lastMousePosition)
        {
            EventBus.Instance.Publish(new MouseMoveEvent(mousePosition));
            m_lastMousePosition = mousePosition;
        }
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 处理回放模式变化事件
    /// </summary>
    private void OnReplayModeChanged(ReplayModeChangedEvent evt)
    {
        m_isInputEnabled = !evt.isReplaying;
    }
    #endregion
}

/// <summary>
/// 左键点击事件
/// </summary>
public struct LeftClickEvent : IEvent
{
    public Vector3 clickPosition;

    public LeftClickEvent(Vector3 clickPosition)
    {
        this.clickPosition = clickPosition;
    }
}

/// <summary>
/// 右键点击事件
/// </summary>
public struct RightClickEvent : IEvent
{
    public Vector3 clickPosition;

    public RightClickEvent(Vector3 clickPosition)
    {
        this.clickPosition = clickPosition;
    }
}

/// <summary>
/// 鼠标移动事件
/// </summary>
public struct MouseMoveEvent : IEvent
{
    public Vector3 mousePosition;

    public MouseMoveEvent(Vector3 mousePosition)
    {
        this.mousePosition = mousePosition;
    }
}

/// <summary>
/// 切换 DebugOverlay 事件（·/` 键触发）
/// </summary>
public struct ToggleDebugOverlayEvent : IEvent
{
}