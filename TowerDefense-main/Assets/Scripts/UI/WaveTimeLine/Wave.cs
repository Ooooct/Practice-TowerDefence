using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 波次显示 UI 组件
/// 显示单个波次的时间线和敌人概要
/// </summary>
public class Wave : MonoBehaviour
{
    #region 序列化字段
    [SerializeField]
    [Tooltip("显示敌人概要的文本组件")]
    private TextMeshProUGUI m_summaryText;
    #endregion

    #region 私有字段
    private WaveTimeline m_waveTimeline;
    private RectTransform m_rectTransform;
    private bool m_isInitialized = false;
    #endregion

    #region 属性
    /// <summary>
    /// 波次时间线数据
    /// </summary>
    public WaveTimeline WaveTimeline => m_waveTimeline;

    /// <summary>
    /// 波次是否已初始化
    /// </summary>
    public bool IsInitialized => m_isInitialized;
    #endregion

    #region Unity 生命周期
    private void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
        if (m_rectTransform != null)
        {
            m_rectTransform.pivot = new Vector2(0f, 0.5f);
        }
        else
        {
            Debug.LogError("[Wave] - RectTransform 组件缺失！");
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 初始化波次显示
    /// </summary>
    /// <param name="timeline">波次时间线数据</param>
    /// <param name="waveData">波次数据（用于生成敌人概要）</param>
    /// <param name="pixelsPerSecond">每秒对应的像素长度</param>
    public void Initialize(WaveTimeline timeline, WaveData waveData, float pixelsPerSecond)
    {
        m_waveTimeline = timeline;
        m_isInitialized = true;
        if (m_rectTransform != null)
        {
            float width = timeline.duration * pixelsPerSecond;
            m_rectTransform.sizeDelta = new Vector2(width, m_rectTransform.sizeDelta.y);
        }
        else
        {
            Debug.LogError("[Wave] - RectTransform 为 null，无法设置宽度");
        }
        string summary = GenerateEnemySummary(timeline.waveIndex, waveData);
        if (m_summaryText != null)
        {
            m_summaryText.text = summary;
        }
        else
        {
            Debug.LogWarning("[Wave] - m_summaryText 为 null，无法显示敌人概要");
        }
    }

    /// <summary>
    /// 更新波次的位置
    /// </summary>
    /// <param name="currentTime">当前游戏时间</param>
    /// <param name="pixelsPerSecond">每秒对应的像素长度</param>
    /// <param name="timelineXPosition">时间线的 X 坐标位置（通常是屏幕左侧边缘）</param>
    public void UpdatePosition(float currentTime, float pixelsPerSecond, float timelineXPosition)
    {
        if (!m_isInitialized)
        {
            return;
        }
        if (m_rectTransform == null)
        {
            Debug.LogError($"[Wave] - UpdatePosition 被调用但 RectTransform 为 null");
            return;
        }
        float timeDifference = m_waveTimeline.startTime - currentTime;
        float pixelOffset = timeDifference * pixelsPerSecond;
        float newX = timelineXPosition + pixelOffset;
        m_rectTransform.anchoredPosition = new Vector2(newX, m_rectTransform.anchoredPosition.y);
    }

    /// <summary>
    /// 检查波次是否已经完全离开视野（左侧）
    /// </summary>
    /// <param name="leftBoundary">左侧边界 X 坐标</param>
    /// <returns>是否已离开视野</returns>
    public bool IsOffScreen(float leftBoundary)
    {
        if (!m_isInitialized || m_rectTransform == null)
        {
            return false;
        }

        // 波次右侧边缘位置
        float rightEdge = m_rectTransform.anchoredPosition.x + m_rectTransform.sizeDelta.x;

        return rightEdge < leftBoundary;
    }

    /// <summary>
    /// 重置波次状态（用于对象池回收）
    /// </summary>
    public void ResetWave()
    {
        m_isInitialized = false;
        m_waveTimeline = default;

        if (m_summaryText != null)
        {
            m_summaryText.text = "";
        }
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 生成敌人概要文本
    /// </summary>
    private string GenerateEnemySummary(int waveIndex, WaveData waveData)
    {
        // 统计每种敌人的总数量
        Dictionary<string, int> enemyCounts = new Dictionary<string, int>();

        foreach (var spawnData in waveData.spawnData)
        {
            if (spawnData.enemyData == null)
            {
                continue;
            }

            string enemyName = spawnData.enemyData.name;

            if (enemyCounts.ContainsKey(enemyName))
            {
                enemyCounts[enemyName] += spawnData.spawnCount;
            }
            else
            {
                enemyCounts[enemyName] = spawnData.spawnCount;
            }
        }

        // 构建概要文本
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append($"第{waveIndex + 1}波：");

        bool first = true;
        foreach (var kvp in enemyCounts)
        {
            if (!first)
            {
                sb.Append(", ");
            }
            sb.Append($"{kvp.Key}*{kvp.Value}");
            first = false;
        }

        return sb.ToString();
    }
    #endregion
}
