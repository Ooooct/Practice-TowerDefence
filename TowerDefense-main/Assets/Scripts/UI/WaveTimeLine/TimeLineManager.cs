using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 时间线管理器
/// 管理所有波次的显示、位置更新和生命周期
/// </summary>
public class TimeLineManager : MonoBehaviour
{
    #region 序列化字段
    [Header("配置")]
    [SerializeField]
    [Tooltip("Wave 预制体")]
    private GameObject m_wavePrefab;

    [SerializeField]
    [Tooltip("Wave 容器（所有 Wave 的父对象）")]
    private RectTransform m_waveContainer;

    [SerializeField]
    [Tooltip("每秒对应的像素长度（距离）")]
    private float m_pixelsPerSecond = 10f;

    [SerializeField]
    [Tooltip("时间线的 X 坐标位置（相对于容器，通常是屏幕左侧边缘）")]
    private float m_timelineXPosition = 0f;

    [SerializeField]
    [Tooltip("左侧边界（用于判断 Wave 是否离开视野）")]
    private float m_leftBoundary = -100f;
    #endregion

    #region 私有字段
    private List<Wave> m_activeWaves = new List<Wave>();
    private List<Wave> m_wavePool = new List<Wave>();
    private bool m_isInitialized = false;
    #endregion

    #region Unity 生命周期
    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!m_isInitialized)
        {
            return;
        }

        if (SpawnManager.Instance == null)
        {
            Debug.LogError("[TimeLineManager] - Update: SpawnManager.Instance 为 null");
            return;
        }

        if (!SpawnManager.Instance.HasGameStarted)
        {
            return;
        }
        float currentTime = SpawnManager.Instance.CurrentGameTime;
        UpdateWavePositions(currentTime);
        RecycleOffScreenWaves();
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 初始化时间线管理器
    /// </summary>
    public void Initialize()
    {
        if (m_isInitialized)
        {
            return;
        }
        if (m_wavePrefab == null)
        {
            Debug.LogError("[TimeLineManager] - Wave 预制体未配置！");
            return;
        }
        if (m_waveContainer == null)
        {
            m_waveContainer = GetComponent<RectTransform>();
            if (m_waveContainer == null)
            {
                Debug.LogError("[TimeLineManager] - Wave 容器未配置且当前对象没有 RectTransform！");
                return;
            }
        }
        if (SpawnManager.Instance == null)
        {
            Debug.LogError("[TimeLineManager] - SpawnManager.Instance 为 null");
            return;
        }
        List<WaveTimeline> timelines = SpawnManager.Instance.GetWaveTimelines();
        if (timelines == null || timelines.Count == 0)
        {
            return;
        }
        SpawnData spawnData = SpawnManager.Instance.CurrentSpawnData;
        if (spawnData == null || spawnData.WaveData == null)
        {
            Debug.LogError("[TimeLineManager] - SpawnData 无效");
            return;
        }

        // 为每个波次创建 Wave
        for (int i = 0; i < timelines.Count; i++)
        {
            WaveTimeline timeline = timelines[i];
            WaveData waveData = spawnData.WaveData[i];

            Wave wave = SpawnWave();
            if (wave != null)
            {
                wave.Initialize(timeline, waveData, m_pixelsPerSecond);
                m_activeWaves.Add(wave);
            }
        }

        m_isInitialized = true;
    }

    /// <summary>
    /// 设置每秒对应的像素长度
    /// </summary>
    public void SetPixelsPerSecond(float pixelsPerSecond)
    {
        m_pixelsPerSecond = Mathf.Max(1f, pixelsPerSecond);

        // 重新初始化所有 Wave 的宽度
        if (m_isInitialized)
        {
            SpawnData spawnData = SpawnManager.Instance.CurrentSpawnData;
            for (int i = 0; i < m_activeWaves.Count; i++)
            {
                Wave wave = m_activeWaves[i];
                if (wave != null && wave.IsInitialized)
                {
                    int waveIndex = wave.WaveTimeline.waveIndex;
                    wave.Initialize(wave.WaveTimeline, spawnData.WaveData[waveIndex], m_pixelsPerSecond);
                }
            }
        }
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 更新所有活跃 Wave 的位置
    /// </summary>
    private void UpdateWavePositions(float currentTime)
    {
        foreach (var wave in m_activeWaves)
        {
            if (wave != null && wave.IsInitialized)
            {
                wave.UpdatePosition(currentTime, m_pixelsPerSecond, m_timelineXPosition);
            }
        }
    }

    /// <summary>
    /// 回收已经离开视野的 Wave
    /// </summary>
    private void RecycleOffScreenWaves()
    {
        for (int i = m_activeWaves.Count - 1; i >= 0; i--)
        {
            Wave wave = m_activeWaves[i];

            if (wave != null && wave.IsOffScreen(m_leftBoundary))
            {
                RecycleWave(wave);
                m_activeWaves.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 从对象池中生成 Wave
    /// </summary>
    private Wave SpawnWave()
    {
        Wave wave = null;

        // 尝试从对象池获取
        if (m_wavePool.Count > 0)
        {
            wave = m_wavePool[0];
            m_wavePool.RemoveAt(0);
            wave.gameObject.SetActive(true);
        }
        else
        {
            // 对象池为空，创建新实例
            GameObject waveObj = Instantiate(m_wavePrefab, m_waveContainer);
            wave = waveObj.GetComponent<Wave>();

            if (wave == null)
            {
                Debug.LogError("[TimeLineManager] - Wave 预制体上没有 Wave 组件！");
                Destroy(waveObj);
                return null;
            }
        }

        return wave;
    }

    /// <summary>
    /// 回收 Wave 到对象池
    /// </summary>
    private void RecycleWave(Wave wave)
    {
        if (wave == null)
        {
            return;
        }

        wave.ResetWave();
        wave.gameObject.SetActive(false);
        m_wavePool.Add(wave);
    }

    /// <summary>
    /// 清理所有 Wave
    /// </summary>
    private void ClearAllWaves()
    {
        // 回收所有活跃的 Wave
        for (int i = m_activeWaves.Count - 1; i >= 0; i--)
        {
            RecycleWave(m_activeWaves[i]);
        }
        m_activeWaves.Clear();

        // 销毁对象池中的 Wave
        foreach (var wave in m_wavePool)
        {
            if (wave != null)
            {
                Destroy(wave.gameObject);
            }
        }
        m_wavePool.Clear();
    }
    #endregion

    #region 清理
    private void OnDestroy()
    {
        ClearAllWaves();
    }
    #endregion
}
