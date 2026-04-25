using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// 可序列化的生成数据（用于 JSON 反序列化）
/// </summary>
[Serializable]
public class SerializableSpawnData
{
    public List<SerializableWaveData> waveData;
    public int waitTimeBeforeStart;
}

/// <summary>
/// 可序列化的波次数据
/// </summary>
[Serializable]
public class SerializableWaveData
{
    public List<SerializableEnemySpawnData> spawnData;
    public int spawnDelay;
}

/// <summary>
/// 可序列化的敌人生成数据
/// </summary>
[Serializable]
public class SerializableEnemySpawnData
{
    public string enemyDataReference;
    public string wayPointsDataReference;
    public int spawnerIndex;
    public int spawnCount;
    public float spawnInterval;
    public float spawnDelay;
}

/// <summary>
/// 存档数据管理器，使用单例模式
/// 监听波次开始事件并自动保存进度
/// </summary>
public class SaveData : MonoBehaviour
{
    #region 单例实现
    private static SaveData m_instance;

    public static SaveData Instance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("SaveData");
                m_instance = obj.AddComponent<SaveData>();
            }
            return m_instance;
        }
    }
    #endregion

    #region 变量
    private const string SAVE_FOLDER = "Save";
    private const string SAVE_FILE_NAME = "save.json";

    private SaveFile m_saveFile;
    private string m_saveFilePath;
    #endregion

    #region Unity 生命周期
    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            InitializeSaveSystem();
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 订阅波次开始事件
        EventBus.Instance.Subscribe<WaveStartEvent>(OnWaveStart);
    }

    void OnDestroy()
    {
        if (m_instance == this)
        {
            // 取消订阅
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Unsubscribe<WaveStartEvent>(OnWaveStart);
            }
        }
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化存档系统
    /// </summary>
    private void InitializeSaveSystem()
    {
        // 获取 Addressables/Save 路径
        string saveFolderPath = Path.Combine(Application.dataPath, "Addressables", SAVE_FOLDER);

        // 确保文件夹存在
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
            Debug.Log($"[SaveData] 创建存档文件夹: {saveFolderPath}");
        }

        m_saveFilePath = Path.Combine(saveFolderPath, SAVE_FILE_NAME);
        Debug.Log($"[SaveData] 存档路径: {m_saveFilePath}");

        // 加载或创建存档文件
        LoadSaveFile();
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 处理波次开始事件
    /// </summary>
    private void OnWaveStart(WaveStartEvent evt)
    {
        if (m_saveFile == null)
        {
            Debug.LogWarning("[SaveData] SaveFile 为空，无法保存");
            return;
        }

        // 更新最高到达波次
        if (evt.WaveIndex + 1 > m_saveFile.HighestWaveReached)
        {
            m_saveFile.HighestWaveReached = evt.WaveIndex + 1;
            Debug.Log($"[SaveData] 更新最高波次: {m_saveFile.HighestWaveReached}");
        }

        // 添加新遇见的敌人
        foreach (string enemyName in evt.EnemyNames)
        {
            if (!m_saveFile.EnemiesHaveSeen.Contains(enemyName))
            {
                m_saveFile.EnemiesHaveSeen.Add(enemyName);
                Debug.Log($"[SaveData] 记录新敌人: {enemyName}");

                // 发送新敌人发现事件
                NewEnemyDiscoveredEvent discoverEvent = new NewEnemyDiscoveredEvent(enemyName);
                EventBus.Instance.Publish(discoverEvent);
            }
        }

        // 自动保存
        WriteSaveFile();
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 读取存档文件
    /// </summary>
    /// <returns>存档数据，如果读取失败则返回新的存档</returns>
    public SaveFile ReadSaveFile()
    {
        if (m_saveFile == null)
        {
            LoadSaveFile();
        }
        return m_saveFile;
    }

    /// <summary>
    /// 写入存档文件
    /// </summary>
    /// <returns>是否写入成功</returns>
    public bool WriteSaveFile()
    {
        if (m_saveFile == null)
        {
            Debug.LogError("[SaveData] SaveFile 为空，无法写入");
            return false;
        }

        try
        {
            string json = JsonConvert.SerializeObject(m_saveFile, Formatting.Indented);
            File.WriteAllText(m_saveFilePath, json);
            Debug.Log($"[SaveData] 存档已保存到: {m_saveFilePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveData] 保存存档失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取当前存档数据（只读）
    /// </summary>
    public SaveFile GetSaveFile()
    {
        return m_saveFile;
    }

    /// <summary>
    /// 重置存档（清空所有数据）
    /// </summary>
    public void ResetSaveFile()
    {
        m_saveFile = new SaveFile();
        WriteSaveFile();
    }

    /// <summary>
    /// 设置自定义波次文件路径
    /// </summary>
    /// <param name="filePath">JSON 文件路径（相对于 Addressables/WaveData 文件夹）</param>
    public void SetCustomWaveFile(string filePath)
    {
        if (m_saveFile == null)
        {
            m_saveFile = new SaveFile();
        }

        m_saveFile.CustomWaveFilePath = filePath;
        WriteSaveFile();
    }

    /// <summary>
    /// 获取自定义波次文件路径
    /// </summary>
    public string GetCustomWaveFile()
    {
        return m_saveFile?.CustomWaveFilePath ?? "";
    }

    /// <summary>
    /// 从自定义 JSON 文件加载波次数据
    /// </summary>
    /// <returns>如果成功加载并应用，返回 true</returns>
    public bool LoadCustomWaveData()
    {
        if (m_saveFile == null || string.IsNullOrEmpty(m_saveFile.CustomWaveFilePath))
        {
            Debug.LogWarning("[SaveData] 没有设置自定义波次文件");
            return false;
        }

        string fullPath = System.IO.Path.Combine(Application.dataPath, "Addressables", "WaveData", m_saveFile.CustomWaveFilePath);

        if (!System.IO.File.Exists(fullPath))
        {
            Debug.LogError($"[SaveData] 自定义波次文件不存在: {fullPath}");
            return false;
        }

        try
        {
            // 读取 JSON 文件
            string json = System.IO.File.ReadAllText(fullPath);
            SerializableSpawnData serializableData = JsonConvert.DeserializeObject<SerializableSpawnData>(json);

            if (serializableData == null || serializableData.waveData == null)
            {
                Debug.LogError("[SaveData] JSON 文件格式不正确");
                return false;
            }

            // 创建运行时 SpawnData
            SpawnData spawnData = CreateRuntimeSpawnData(serializableData);

            if (spawnData == null)
            {
                Debug.LogError("[SaveData] 创建 SpawnData 失败");
                return false;
            }

            // 将 SpawnData 传递给 SpawnManager
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.SetSpawnData(spawnData);
                Debug.Log($"[SaveData] 成功加载自定义波次数据: {m_saveFile.CustomWaveFilePath}");
                return true;
            }
            else
            {
                Debug.LogError("[SaveData] SpawnManager 实例不存在");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveData] 加载自定义波次数据失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从序列化数据创建运行时 SpawnData
    /// </summary>
    private SpawnData CreateRuntimeSpawnData(SerializableSpawnData serializableData)
    {
        SpawnData spawnData = ScriptableObject.CreateInstance<SpawnData>();

        // 使用反射设置私有字段
        var waveDataField = typeof(SpawnData).GetField("m_waveData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var waitTimeField = typeof(SpawnData).GetField("m_waitTimeBeforeStart",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (waveDataField == null || waitTimeField == null)
        {
            Debug.LogError("[SaveData] 无法找到 SpawnData 的私有字段");
            return null;
        }

        waitTimeField.SetValue(spawnData, serializableData.waitTimeBeforeStart);

        List<WaveData> waveDataList = new List<WaveData>();

        foreach (var serializableWave in serializableData.waveData)
        {
            WaveData wave = new WaveData
            {
                spawnData = new List<EnemySpawnData>(),
                spawnDelay = serializableWave.spawnDelay
            };

            foreach (var serializableEnemySpawn in serializableWave.spawnData)
            {
                EnemyData enemyData = LoadAssetFromReference<EnemyData>(serializableEnemySpawn.enemyDataReference, CategoriesEnum.Enemy);
                WayPointsData wayPointsData = LoadAssetFromReference<WayPointsData>(serializableEnemySpawn.wayPointsDataReference, CategoriesEnum.WayPoints);

                if (enemyData == null)
                {
                    Debug.LogWarning($"[SaveData] 无法加载敌人数据: {serializableEnemySpawn.enemyDataReference}");
                    continue;
                }

                EnemySpawnData enemySpawn = new EnemySpawnData
                {
                    enemyData = enemyData,
                    wayPointsData = wayPointsData,
                    SpawnerIndex = serializableEnemySpawn.spawnerIndex,
                    spawnCount = serializableEnemySpawn.spawnCount,
                    spawnInterval = serializableEnemySpawn.spawnInterval,
                    spawnDelay = serializableEnemySpawn.spawnDelay
                };

                wave.spawnData.Add(enemySpawn);
            }

            waveDataList.Add(wave);
        }

        waveDataField.SetValue(spawnData, waveDataList);
        return spawnData;
    }

    /// <summary>
    /// 从资源引用字符串加载资源
    /// </summary>
    private T LoadAssetFromReference<T>(string reference, CategoriesEnum category) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        // 支持两种格式："path:" 开头的路径或直接的资源名
        if (reference.StartsWith("path:"))
        {
            string assetPath = reference.Substring(5);
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
            Debug.LogWarning($"[SaveData] 运行时不支持 path: 引用格式，请使用资源名: {reference}");
            return null;
#endif
        }
        else
        {
            // 从 AssetManager 加载
            return AssetManager.Instance.GetAsset<T>(category, reference);
        }
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 加载存档文件
    /// </summary>
    private void LoadSaveFile()
    {
        if (File.Exists(m_saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(m_saveFilePath);
                m_saveFile = JsonConvert.DeserializeObject<SaveFile>(json);
                Debug.Log($"[SaveData] 存档已加载，最高波次: {m_saveFile.HighestWaveReached}, 已见敌人: {m_saveFile.EnemiesHaveSeen.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveData] 加载存档失败: {e.Message}，创建新存档");
                m_saveFile = new SaveFile();
                WriteSaveFile();
            }
        }
        else
        {
            Debug.Log("[SaveData] 存档文件不存在，创建新存档");
            m_saveFile = new SaveFile();
            WriteSaveFile();
        }
    }
    #endregion
}

/// <summary>
/// 存档文件数据结构
/// </summary>
[Serializable]
public class SaveFile
{
    public List<string> EnemiesHaveSeen = new List<string>();
    public int HighestWaveReached = 0;
    public string CustomWaveFilePath = ""; // 自定义波次文件路径
}

/// <summary>
/// 波次开始事件
/// </summary>
public struct WaveStartEvent : IEvent
{
    public int WaveIndex;
    public List<string> EnemyNames;

    public WaveStartEvent(int waveIndex, List<string> enemyNames)
    {
        WaveIndex = waveIndex;
        EnemyNames = enemyNames;
    }
}

/// <summary>
/// 新敌人发现事件
/// </summary>
public class NewEnemyDiscoveredEvent : IEvent
{
    public string EnemyName { get; private set; }

    public NewEnemyDiscoveredEvent(string enemyName)
    {
        EnemyName = enemyName;
    }
}