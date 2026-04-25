using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成管理器，负责根据 SpawnData 调度多个 EnemySpawner 生成敌人
/// </summary>
public class SpawnManager : MonoBehaviour
{
    #region 单例实现
    private static SpawnManager m_instance;

    public static SpawnManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("SpawnManager");
                m_instance = obj.AddComponent<SpawnManager>();
            }
            return m_instance;
        }
    }
    #endregion

    #region 变量
    [SerializeField]
    private List<EnemySpawner> m_enemySpawners;

    [SerializeField]
    private SpawnData m_spawnData;

    private bool m_isSpawning = false;
    private bool m_hasGameStarted = false;  // 游戏是否已开始的标志
    private bool m_allWavesCompleted = false;  // 所有波次是否生成完毕
    private int m_currentWaveIndex = 0;
    private float m_gameStartTime = 0f;  // 游戏开始时间（用于时间线同步）
    private List<WaveTimeline> m_waveTimelines;  // 缓存的波次时间线
    private bool[] m_waveTriggered;  // 记录每波是否已触发
    #endregion

    #region 属性
    /// <summary>
    /// 当前波次索引
    /// </summary>
    public int CurrentWaveIndex => m_currentWaveIndex;

    /// <summary>
    /// 总波次数
    /// </summary>
    public int TotalWaves => m_spawnData != null ? m_spawnData.WaveData.Count : 0;

    /// <summary>
    /// 获取当前 SpawnData
    /// </summary>
    public SpawnData CurrentSpawnData => m_spawnData;

    /// <summary>
    /// 获取当前游戏时间（相对于生成开始的时间）
    /// </summary>
    public float CurrentGameTime => m_hasGameStarted ? Time.time - m_gameStartTime : 0f;

    /// <summary>
    /// 游戏是否已经开始
    /// </summary>
    public bool HasGameStarted => m_hasGameStarted;

    /// <summary>
    /// 所有波次是否生成完毕
    /// </summary>
    public bool AllWavesCompleted => m_allWavesCompleted;
    #endregion

    #region Unity 生命周期
    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }
        else if (m_instance != this)
        {
            Debug.LogWarning("[SpawnManager] - 检测到重复实例，销毁");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 验证配置
        if (m_spawnData == null)
        {
            Debug.LogError("[SpawnManager] - m_spawnData 为 null！请在 Inspector 中配置 SpawnData");
            return;
        }

        if (m_enemySpawners == null || m_enemySpawners.Count == 0)
        {
            Debug.LogError("[SpawnManager] - m_enemySpawners 为空或未配置！请在 Inspector 中添加 EnemySpawner");
            return;
        }

        StartSpawning();
    }

    void Update()
    {
        if (!m_hasGameStarted || !m_isSpawning)
        {
            return;
        }

        float currentTime = CurrentGameTime;

        // 检查是否有波次需要触发
        for (int i = 0; i < m_waveTimelines.Count; i++)
        {
            if (!m_waveTriggered[i] && currentTime >= m_waveTimelines[i].startTime)
            {
                m_waveTriggered[i] = true;
                m_currentWaveIndex = i;
                StartCoroutine(SpawnWaveCoroutine(i));
            }
        }

        // 检查是否所有波次都已完成
        if (m_currentWaveIndex >= m_waveTimelines.Count - 1 &&
            currentTime >= m_waveTimelines[m_waveTimelines.Count - 1].endTime)
        {
            m_isSpawning = false;
            m_allWavesCompleted = true;
        }
    }
    #endregion

    #region Public 方法
    /// <summary>
    /// 开始生成所有波次的敌人
    /// </summary>
    public void StartSpawning()
    {
        if (m_isSpawning)
        {
            Debug.LogWarning("[SpawnManager] - 已经在生成敌人中");
            return;
        }

        if (m_spawnData == null)
        {
            Debug.LogError("[SpawnManager] - SpawnData 未配置，无法开始生成");
            return;
        }

        if (m_enemySpawners == null || m_enemySpawners.Count == 0)
        {
            Debug.LogError("[SpawnManager] - 没有配置 EnemySpawner，无法开始生成");
            return;
        }

        m_isSpawning = true;
        m_currentWaveIndex = 0;
        m_allWavesCompleted = false;

        // 立即设置游戏开始标志和开始时间
        m_hasGameStarted = true;
        m_gameStartTime = Time.time;

        // 初始化波次时间线和触发状态
        m_waveTimelines = GetWaveTimelines();
        m_waveTriggered = new bool[m_waveTimelines.Count];
    }

    /// <summary>
    /// 获取所有波次的时间线信息（包括延迟）
    /// </summary>
    /// <returns>波次时间线列表，每个元素包含波次索引、开始时间和结束时间</returns>
    public List<WaveTimeline> GetWaveTimelines()
    {
        List<WaveTimeline> timelines = new List<WaveTimeline>();

        if (m_spawnData == null || m_spawnData.WaveData == null)
        {
            Debug.LogWarning("[SpawnManager] - SpawnData 未配置，无法计算时间线");
            return timelines;
        }

        float currentTime = 0f;

        // 遍历所有波次
        for (int waveIndex = 0; waveIndex < m_spawnData.WaveData.Count; waveIndex++)
        {
            WaveData wave = m_spawnData.WaveData[waveIndex];

            // 如果不是第一波，加上延迟时间（延迟是波次之间的间隔）
            if (waveIndex > 0)
            {
                currentTime += wave.spawnDelay;
            }

            float waveStartTime = currentTime;
            float waveDuration = CalculateWaveDuration(wave);
            float waveEndTime = waveStartTime + waveDuration;

            timelines.Add(new WaveTimeline
            {
                waveIndex = waveIndex,
                startTime = waveStartTime,
                endTime = waveEndTime,
                duration = waveDuration
            });

            // 更新当前时间为波次结束时间
            currentTime = waveEndTime;
        }

        return timelines;
    }

    /// <summary>
    /// 计算单个波次的持续时间（不包含波次间延迟）
    /// </summary>
    private float CalculateWaveDuration(WaveData wave)
    {
        float maxEndTime = 0f;

        foreach (var spawnData in wave.spawnData)
        {
            // 计算该组敌人的总生成时间
            // 总时间 = 延迟时间 + (生成数量 - 1) * 生成间隔
            float spawnStartTime = spawnData.spawnDelay;
            float spawnDuration = 0f;

            if (spawnData.spawnCount > 1)
            {
                spawnDuration = (spawnData.spawnCount - 1) * spawnData.spawnInterval;
            }

            float totalEndTime = spawnStartTime + spawnDuration;

            // 取最晚结束的时间作为波次持续时间
            if (totalEndTime > maxEndTime)
            {
                maxEndTime = totalEndTime;
            }
        }

        return maxEndTime;
    }
    #endregion

    #region Private 方法
    /// <summary>
    /// 生成单个波次的协程
    /// </summary>
    private IEnumerator SpawnWaveCoroutine(int waveIndex)
    {
        WaveData wave = m_spawnData.WaveData[waveIndex];

        // 收集本波次所有敌人名称
        List<string> enemyNames = new List<string>();
        foreach (var spawnData in wave.spawnData)
        {
            if (spawnData.enemyData != null && !enemyNames.Contains(spawnData.enemyData.name))
            {
                enemyNames.Add(spawnData.enemyData.name);
            }
        }

        // 发布波次开始事件
        EventBus.Instance.Publish(new WaveStartEvent(waveIndex, enemyNames));

        // 为每个 Spawner 启动独立的生成协程（并行生成，每个都有自己的延迟）
        List<Coroutine> spawnerCoroutines = new List<Coroutine>();

        foreach (var spawnData in wave.spawnData)
        {
            if (spawnData.SpawnerIndex < 0 || spawnData.SpawnerIndex >= m_enemySpawners.Count)
            {
                Debug.LogWarning($"[SpawnManager] - 无效的 SpawnerIndex: {spawnData.SpawnerIndex}");
                continue;
            }

            EnemySpawner spawner = m_enemySpawners[spawnData.SpawnerIndex];
            if (spawner == null)
            {
                Debug.LogWarning($"[SpawnManager] - Spawner {spawnData.SpawnerIndex} 为空");
                continue;
            }

            // 启动该 Spawner 的生成协程（会在协程内部处理延迟）
            Coroutine coroutine = StartCoroutine(SpawnEnemiesForSpawnerCoroutine(spawner, spawnData));
            spawnerCoroutines.Add(coroutine);
        }

        // 等待所有生成协程完成
        foreach (var coroutine in spawnerCoroutines)
        {
            yield return coroutine;
        }
    }

    /// <summary>
    /// 为特定 Spawner 生成敌人的协程
    /// </summary>
    private IEnumerator SpawnEnemiesForSpawnerCoroutine(EnemySpawner spawner, EnemySpawnData spawnData)
    {
        if (spawnData.enemyData == null)
        {
            Debug.LogWarning("[SpawnManager] - EnemyData 为空");
            yield break;
        }

        // 首先等待该组敌人的延迟时间
        if (spawnData.spawnDelay > 0)
        {
            yield return new WaitForSeconds(spawnData.spawnDelay);
        }

        for (int i = 0; i < spawnData.spawnCount; i++)
        {
            if (!m_isSpawning) yield break; // 如果被停止，则退出

            // 生成敌人
            EnemyMain enemy = spawner.SpawnEnemy(spawnData.enemyData);

            if (enemy != null)
            {
                // 如果该生成数据指定了路径，则把路径注入到新生成的敌人上
                if (spawnData.wayPointsData != null)
                {
                    var movement = enemy.GetObjectComponent<EnemyMovement>();
                    if (movement != null)
                    {
                        movement.SetWaypoints(spawnData.wayPointsData);
                    }
                }
                else
                {
                    Debug.LogWarning($"[SpawnManager] - Spawner {spawnData.SpawnerIndex} 未指定路径数据");
                }
            }

            // 如果不是最后一个敌人，等待生成间隔
            if (i < spawnData.spawnCount - 1 && spawnData.spawnInterval > 0)
            {
                yield return new WaitForSeconds(spawnData.spawnInterval);
            }
        }

    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 设置新的 SpawnData（用于运行时动态加载波次数据）
    /// </summary>
    public void SetSpawnData(SpawnData newSpawnData)
    {
        if (newSpawnData == null)
        {
            Debug.LogError("[SpawnManager] 尝试设置空的 SpawnData");
            return;
        }

        m_spawnData = newSpawnData;

        // 重置状态
        m_currentWaveIndex = 0;
        m_hasGameStarted = false;
        m_allWavesCompleted = false;
        m_isSpawning = false;

        // 重新计算时间线
        m_waveTimelines = GetWaveTimelines();
        if (m_waveTimelines != null)
        {
            m_waveTriggered = new bool[m_waveTimelines.Count];
        }

        Debug.Log($"[SpawnManager] 已设置新的 SpawnData，总波次数: {TotalWaves}");
    }
    #endregion
}

/// <summary>
/// 波次时间线信息
/// </summary>
[System.Serializable]
public struct WaveTimeline
{
    public int waveIndex;
    public float startTime;
    public float endTime;
    public float duration;
}
