using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// 回放管理器
/// 负责记录和回放游戏操作，支持确定性回放
/// </summary>
public class ReplayManager : MonoBehaviour
{
    #region 单例实现
    private static ReplayManager m_instance;

    public static ReplayManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("ReplayManager");
                m_instance = obj.AddComponent<ReplayManager>();
            }
            return m_instance;
        }
    }
    #endregion

    #region 变量
    private const string REPLAY_FOLDER = "Replay";

    private ReplayData m_currentReplay;
    private bool m_isRecording = false;
    private bool m_isReplaying = false;
    private float m_recordStartTime = 0f;
    private int m_replayEventIndex = 0;
    #endregion

    #region 属性
    /// <summary>
    /// 是否正在录制
    /// </summary>
    public bool IsRecording => m_isRecording;

    /// <summary>
    /// 是否正在回放
    /// </summary>
    public bool IsReplaying => m_isReplaying;
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
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 检查是否有跨场景的回放标记
        if (ReplayMessenger.ConsumeReplayMark(out bool shouldReplay, out string filePath))
        {
            if (shouldReplay)
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    StartReplay(filePath);
                }
                else
                {
                    StartReplay(); // 使用最新的回放文件
                }
                return;
            }
        }

        // 默认开始录制
        StartRecording();
    }

    void Update()
    {
        // 回放模式下，按时间触发事件
        if (m_isReplaying && m_currentReplay != null)
        {
            ProcessReplayEvents();
        }
    }
    #endregion

    #region 录制控制
    /// <summary>
    /// 开始录制
    /// </summary>
    public void StartRecording()
    {
        if (m_isRecording)
        {
            Debug.LogWarning("[ReplayManager] 已经在录制中");
            return;
        }

        if (m_isReplaying)
        {
            Debug.LogWarning("[ReplayManager] 正在回放中，无法开始录制");
            return;
        }

        // 初始化新的回放数据
        m_currentReplay = new ReplayData
        {
            randomSeed = RandomController.Instance.CurrentSeed,
            recordTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            events = new List<ReplayEvent>()
        };

        m_recordStartTime = Time.time;
        m_isRecording = true;

        // 订阅建造塔事件
        EventBus.Instance.Subscribe<TryBuildTowerEvent>(OnTryBuildTower);

        Debug.Log($"[ReplayManager] 开始录制，随机种子: {m_currentReplay.randomSeed}");
    }

    /// <summary>
    /// 停止录制并保存
    /// </summary>
    public void StopRecording()
    {
        if (!m_isRecording)
        {
            Debug.LogWarning("[ReplayManager] 当前没有在录制");
            return;
        }

        m_isRecording = false;

        // 取消订阅事件
        EventBus.Instance.Unsubscribe<TryBuildTowerEvent>(OnTryBuildTower);

        // 保存回放文件
        SaveReplay();

        Debug.Log($"[ReplayManager] 录制结束，共记录 {m_currentReplay.events.Count} 个事件");
    }

    /// <summary>
    /// 应用退出时自动保存录制
    /// </summary>
    private void OnApplicationQuit()
    {
        FinalizeRecordingIfAny();
    }

    /// <summary>
    /// 对象销毁时（例如切场景/退出）自动保存录制
    /// </summary>
    private void OnDestroy()
    {
        if (m_instance == this)
            FinalizeRecordingIfAny();
    }

    /// <summary>
    /// 如当前在录制则安全结束并保存
    /// </summary>
    private void FinalizeRecordingIfAny()
    {
        if (m_isRecording)
        {
            Debug.Log("[ReplayManager] 检测到退出/销毁，自动保存录制...");
            StopRecording();
        }
    }
    #endregion

    #region 回放控制
    /// <summary>
    /// 标记：在下一次场景开始时回放最新回放文件
    /// </summary>
    public static void MarkReplayLatestOnNextScene()
    {
        ReplayMessenger.MarkReplayLatestOnNextScene();
    }

    /// <summary>
    /// 标记：在下一次场景开始时回放指定文件
    /// </summary>
    public static void MarkReplayFileOnNextScene(string filePath)
    {
        ReplayMessenger.MarkReplayFileOnNextScene(filePath);
    }

    /// <summary>
    /// 开始回放最新的回放文件
    /// </summary>
    public void StartReplay()
    {
        // 获取最新的回放文件
        string latestFile = GetLatestReplayFile();
        if (string.IsNullOrEmpty(latestFile))
        {
            Debug.LogError("[ReplayManager] 没有找到可用的回放文件");
            return;
        }

        Debug.Log($"[ReplayManager] 使用最新回放文件: {Path.GetFileName(latestFile)}");
        StartReplay(latestFile);
    }

    /// <summary>
    /// 开始回放指定文件
    /// </summary>
    /// <param name="filePath">回放文件路径</param>
    public void StartReplay(string filePath)
    {
        // 若正在录制，先自动结束并保存，避免丢失数据
        if (m_isRecording)
        {
            Debug.LogWarning("[ReplayManager] 正在录制中，已自动停止并保存，然后开始回放");
            StopRecording();
        }

        if (m_isReplaying)
        {
            Debug.LogWarning("[ReplayManager] 已经在回放中");
            return;
        }

        // 加载回放文件
        if (!LoadReplay(filePath))
        {
            Debug.LogError($"[ReplayManager] 加载回放文件失败: {filePath}");
            return;
        }

        // 设置随机种子
        RandomController.Instance.SetSeed(m_currentReplay.randomSeed);

        m_isReplaying = true;
        m_replayEventIndex = 0;
        m_recordStartTime = Time.time;

        // 通知 InputManager 禁用输入
        EventBus.Instance.Publish(new ReplayModeChangedEvent(true));

        Debug.Log($"[ReplayManager] 开始回放，种子: {m_currentReplay.randomSeed}, 事件数: {m_currentReplay.events.Count}");
    }

    /// <summary>
    /// 停止回放
    /// </summary>
    public void StopReplay()
    {
        if (!m_isReplaying)
        {
            Debug.LogWarning("[ReplayManager] 当前没有在回放");
            return;
        }

        m_isReplaying = false;
        m_replayEventIndex = 0;

        // 通知 InputManager 恢复输入
        EventBus.Instance.Publish(new ReplayModeChangedEvent(false));

        Debug.Log("[ReplayManager] 回放结束");
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 处理建造塔事件（录制模式）
    /// </summary>
    private void OnTryBuildTower(TryBuildTowerEvent evt)
    {
        if (!m_isRecording)
        {
            return;
        }

        // 计算事件发生的时间
        float eventTime = Time.time - m_recordStartTime;

        // 获取塔数据的资源键
        string towerDataKey = AssetManager.Instance.GetAssetKey(evt.towerBuildData, CategoriesEnum.Towers);
        if (string.IsNullOrEmpty(towerDataKey))
        {
            Debug.LogWarning($"[ReplayManager] 无法找到 TowerData 的资源键: {evt.towerBuildData?.name ?? "null"}");
            return;
        }

        // 创建回放事件
        ReplayEvent replayEvent = new ReplayEvent
        {
            timestamp = eventTime,
            eventType = "TryBuildTower",
            towerDataKey = towerDataKey,
            mousePositionX = evt.mousePosition.x,
            mousePositionY = evt.mousePosition.y,
            mousePositionZ = evt.mousePosition.z,
            towerLevel = evt.towerLevel,
            towerName = evt.towerName
        };

        m_currentReplay.events.Add(replayEvent);
    }

    /// <summary>
    /// 处理回放事件（回放模式）
    /// </summary>
    private void ProcessReplayEvents()
    {
        if (m_currentReplay == null || m_currentReplay.events == null)
        {
            return;
        }

        float currentTime = Time.time - m_recordStartTime;

        // 遍历待执行的事件
        while (m_replayEventIndex < m_currentReplay.events.Count)
        {
            ReplayEvent evt = m_currentReplay.events[m_replayEventIndex];

            // 如果事件时间还没到，等待
            if (evt.timestamp > currentTime)
            {
                break;
            }

            // 执行事件
            ExecuteReplayEvent(evt);

            m_replayEventIndex++;
        }

        // 所有事件执行完毕，停止回放
        if (m_replayEventIndex >= m_currentReplay.events.Count)
        {
            StopReplay();
        }
    }

    /// <summary>
    /// 执行单个回放事件
    /// </summary>
    private void ExecuteReplayEvent(ReplayEvent evt)
    {
        if (evt.eventType == "TryBuildTower")
        {
            // 获取塔数据
            TowerData towerData = AssetManager.Instance.GetAsset<TowerData>(CategoriesEnum.Towers, evt.towerDataKey);
            if (towerData == null)
            {
                Debug.LogError($"[ReplayManager] 无法找到 TowerData: {evt.towerDataKey}");
                return;
            }

            // 重构鼠标位置
            Vector3 mousePosition = new Vector3(evt.mousePositionX, evt.mousePositionY, evt.mousePositionZ);

            // 通过鼠标位置进行射线检测，找到目标塔位
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.LogError($"[ReplayManager] 射线检测失败，鼠标位置: {mousePosition}");
                return;
            }

            if (!hit.collider.CompareTag("Tower"))
            {
                Debug.LogError($"[ReplayManager] 射线检测到的对象不是塔位: {hit.collider.name}");
                return;
            }

            // 发布建造塔事件
            TryBuildTowerEvent buildEvent = new TryBuildTowerEvent(
                hit.collider.gameObject,
                towerData,
                evt.towerLevel,
                evt.towerName,
                mousePosition
            );

            EventBus.Instance.Publish(buildEvent);

            Debug.Log($"[ReplayManager] 执行回放事件 - 时间: {evt.timestamp:F2}s, 塔: {evt.towerDataKey}, 塔位: {hit.collider.name}");
        }
    }
    #endregion

    #region 文件操作
    /// <summary>
    /// 保存回放文件
    /// </summary>
    private void SaveReplay()
    {
        if (m_currentReplay == null)
        {
            Debug.LogError("[ReplayManager] 没有回放数据可保存");
            return;
        }

        try
        {
            // 确保文件夹存在
            string replayFolderPath = Path.Combine(Application.dataPath, "Addressables", REPLAY_FOLDER);
            if (!Directory.Exists(replayFolderPath))
            {
                Directory.CreateDirectory(replayFolderPath);
                Debug.Log($"[ReplayManager] 创建回放文件夹: {replayFolderPath}");
            }

            // 生成文件名
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"Replay-{timestamp}.json";
            string filePath = Path.Combine(replayFolderPath, fileName);

            // 序列化并保存
            string json = JsonConvert.SerializeObject(m_currentReplay, Formatting.Indented);
            File.WriteAllText(filePath, json);

            Debug.Log($"[ReplayManager] 回放已保存到: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReplayManager] 保存回放失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载回放文件
    /// </summary>
    private bool LoadReplay(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[ReplayManager] 回放文件不存在: {filePath}");
            return false;
        }

        string json = File.ReadAllText(filePath);
        m_currentReplay = JsonConvert.DeserializeObject<ReplayData>(json);

        if (m_currentReplay == null)
        {
            Debug.LogError("[ReplayManager] 反序列化回放数据失败");
            return false;
        }

        Debug.Log($"[ReplayManager] 回放文件已加载: {filePath}");
        return true;
    }

    /// <summary>
    /// 获取所有回放文件
    /// </summary>
    public List<string> GetAllReplayFiles()
    {
        List<string> replayFiles = new List<string>();

        try
        {
            string replayFolderPath = Path.Combine(Application.dataPath, "Addressables", REPLAY_FOLDER);
            if (!Directory.Exists(replayFolderPath))
            {
                return replayFiles;
            }

            string[] files = Directory.GetFiles(replayFolderPath, "*.json");
            replayFiles.AddRange(files);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReplayManager] 获取回放文件列表失败: {e.Message}");
        }

        return replayFiles;
    }

    /// <summary>
    /// 获取最新的回放文件
    /// </summary>
    /// <returns>最新回放文件的完整路径，如果没有找到则返回 null</returns>
    private string GetLatestReplayFile()
    {
        try
        {
            string replayFolderPath = Path.Combine(Application.dataPath, "Addressables", REPLAY_FOLDER);
            if (!Directory.Exists(replayFolderPath))
            {
                Debug.LogWarning("[ReplayManager] 回放文件夹不存在");
                return null;
            }

            string[] files = Directory.GetFiles(replayFolderPath, "*.json");
            if (files.Length == 0)
            {
                Debug.LogWarning("[ReplayManager] 没有找到任何回放文件");
                return null;
            }

            // 按文件修改时间排序，获取最新的文件
            string latestFile = files[0];
            DateTime latestTime = File.GetLastWriteTime(latestFile);

            for (int i = 1; i < files.Length; i++)
            {
                DateTime currentTime = File.GetLastWriteTime(files[i]);
                if (currentTime > latestTime)
                {
                    latestTime = currentTime;
                    latestFile = files[i];
                }
            }

            return latestFile;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReplayManager] 获取最新回放文件失败: {e.Message}");
            return null;
        }
    }
    #endregion
}

#region 数据结构
/// <summary>
/// 回放数据
/// </summary>
[Serializable]
public class ReplayData
{
    public int randomSeed;                  // 随机种子
    public string recordTime;               // 录制时间
    public List<ReplayEvent> events;        // 事件列表
}

/// <summary>
/// 回放事件
/// </summary>
[Serializable]
public class ReplayEvent
{
    public float timestamp;                 // 事件时间戳
    public string eventType;                // 事件类型
    public string towerDataKey;             // 塔数据键
    public float mousePositionX;            // 鼠标点击位置 X
    public float mousePositionY;            // 鼠标点击位置 Y
    public float mousePositionZ;            // 鼠标点击位置 Z
    public int towerLevel;                  // 塔等级
    public string towerName;                // 塔名称
}

/// <summary>
/// 回放模式变化事件
/// </summary>
public struct ReplayModeChangedEvent : IEvent
{
    public bool isReplaying;

    public ReplayModeChangedEvent(bool isReplaying)
    {
        this.isReplaying = isReplaying;
    }
}
#endregion
