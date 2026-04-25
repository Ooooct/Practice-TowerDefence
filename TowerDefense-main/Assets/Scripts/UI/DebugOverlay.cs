using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 调试信息覆盖层，显示 FPS、敌人数量和 Buff 统计信息
/// 通过 ·/` 键切换显示/隐藏
/// </summary>
public class DebugOverlay : MonoBehaviour
{
    #region 变量
    [SerializeField] private TextMeshProUGUI m_debugText;
    [SerializeField] private GameObject m_overlayPanel;

    private bool m_isVisible = false;
    private float m_frameTime = 0f;
    private int m_frameCount = 0;
    private float m_fps = 0f;
    #endregion

    #region Unity 生命周期
    private void Start()
    {
        // 初始隐藏
        if (m_overlayPanel != null)
        {
            m_overlayPanel.SetActive(false);
        }

        // 订阅 DebugOverlay 切换事件
        EventBus.Instance.Subscribe<ToggleDebugOverlayEvent>(OnToggleDebugOverlay);
    }

    private void OnDestroy()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<ToggleDebugOverlayEvent>(OnToggleDebugOverlay);
        }
    }

    private void Update()
    {
        if (!m_isVisible)
        {
            return;
        }

        // 更新 FPS 和调试信息
        UpdateFPS();
        UpdateDebugText();
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 处理 DebugOverlay 切换事件
    /// </summary>
    private void OnToggleDebugOverlay(ToggleDebugOverlayEvent evt)
    {
        m_isVisible = !m_isVisible;
        if (m_overlayPanel != null)
        {
            m_overlayPanel.SetActive(m_isVisible);
        }
    }
    #endregion

    #region 数据更新
    /// <summary>
    /// 更新 FPS
    /// </summary>
    private void UpdateFPS()
    {
        m_frameCount++;
        m_frameTime += Time.deltaTime;

        if (m_frameTime >= 1f)
        {
            m_fps = m_frameCount / m_frameTime;
            m_frameCount = 0;
            m_frameTime = 0f;
        }
    }

    /// <summary>
    /// 更新调试文本
    /// </summary>
    private void UpdateDebugText()
    {
        if (m_debugText == null)
        {
            return;
        }

        int enemyCount = UnitManager.Instance.EnemyCount;
        int totalBuffCount = GetTotalActiveBufCount();

        string debugInfo = $"DEBUG OVERLAY\n";
        debugInfo += $"FPS: {m_fps:F1}\n";
        debugInfo += $"Enemies: {enemyCount}\n";
        debugInfo += $"Total Active Buffs: {totalBuffCount}\n";
        debugInfo += $"Towers: {UnitManager.Instance.TowerCount}\n";
        debugInfo += $"Bullets: {UnitManager.Instance.BulletCount}";

        m_debugText.text = debugInfo;
    }

    /// <summary>
    /// 获取所有单位的活跃 Buff 总数
    /// </summary>
    private int GetTotalActiveBufCount()
    {
        int totalCount = 0;

        // 统计敌人的 Buff
        foreach (var enemy in UnitManager.Instance.Enemies)
        {
            if (enemy != null && enemy.BuffManager != null)
            {
                totalCount += enemy.BuffManager.GetActiveBufCount();
            }
        }

        // 统计塔的 Buff
        foreach (var tower in UnitManager.Instance.Towers)
        {
            if (tower != null && tower.BuffManager != null)
            {
                totalCount += tower.BuffManager.GetActiveBufCount();
            }
        }

        // 统计子弹的 Buff
        foreach (var bullet in UnitManager.Instance.Bullets)
        {
            if (bullet != null && bullet.BuffManager != null)
            {
                totalCount += bullet.BuffManager.GetActiveBufCount();
            }
        }

        return totalCount;
    }
    #endregion
}
