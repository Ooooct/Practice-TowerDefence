using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 建造管理器，负责处理塔的建造逻辑
/// </summary>
public class BuildingManager : MonoBehaviour
{
    #region 变量
    private static BuildingManager m_instance;
    private static int m_towerLevel;
    [SerializeField] private TowerData m_towerBuildData; // 使用可空类型
    [SerializeField] private string m_towerName; // 存储塔的名称
    #endregion

    #region 属性
    public static BuildingManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("BuildingManager");
                m_instance = obj.AddComponent<BuildingManager>();
            }
            return m_instance;
        }
    }
    #endregion

    #region Public 方法
    /// <summary>
    /// 设置塔建造数据
    /// </summary>
    /// <param name="towerLevelData">塔等级数据</param>
    /// <param name="towerName">塔的名称（用于调试和显示）</param>
    public void SetTowerBuildData(TowerData towerBuildData, int towerLevel, string towerName = "")
    {
        m_towerBuildData = towerBuildData;
        m_towerLevel = towerLevel;
        m_towerName = towerName;
    }

    /// <summary>
    /// 清除当前的建造数据
    /// </summary>
    public void ClearBuildData()
    {
        m_towerBuildData = null;
        m_towerName = string.Empty;
    }

    /// <summary>
    /// 检查是否有当前的建造数据
    /// </summary>
    /// <returns>如果有建造数据返回 true，否则返回 false</returns>
    public bool HasBuildData()
    {
        return m_towerBuildData != null;
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

    private void Start()
    {
        EventBus.Instance.Subscribe<LeftClickEvent>(TryBuildTower);
        EventBus.Instance.Subscribe<RightClickEvent>(CancelBuild);
        EventBus.Instance.Subscribe<CompletedBuildEvent>(CompletedBuild);
    }

    private void OnDestroy()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<LeftClickEvent>(TryBuildTower);
            EventBus.Instance.Unsubscribe<RightClickEvent>(CancelBuild);
            EventBus.Instance.Unsubscribe<CompletedBuildEvent>(CompletedBuild);
        }
    }
    #endregion

    #region Private 方法
    /// <summary>
    /// 取消建造塔
    /// </summary>
    private void CancelBuild(RightClickEvent evt)
    {
        ClearBuildData();
    }

    private void CompletedBuild(CompletedBuildEvent evt)
    {
        ClearBuildData();
    }

    /// <summary>
    /// 尝试建造塔
    /// </summary>
    private void TryBuildTower(LeftClickEvent evt)
    {
        // 检查是否有建造数据
        if (m_towerBuildData == null)
        {
            return;
        }

        TowerLevelData levelData = m_towerBuildData.LevelData[m_towerLevel];

        // 检查是否有足够的金钱
        if (CostManager.Instance.CanAfford(levelData.cost) == false)
        {
            Debug.LogWarning($"[BuildingManager] - 金钱不足，无法建造塔 '{m_towerName}'");
            return;
        }

        // 射线检测
        Ray ray = Camera.main.ScreenPointToRay(evt.clickPosition);
        // 可视化调试射线
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 如果对象的 tag 是 Tower
            if (hit.collider.CompareTag("Tower"))
            {
                Debug.Log($"[BuildingManager] - 点击到了塔位，发送尝试建造事件: {m_towerName}");
                EventBus.Instance.Publish(new TryBuildTowerEvent(hit.collider.gameObject,
                m_towerBuildData, m_towerLevel, m_towerName, evt.clickPosition));
                return;
            }
        }
    }
    #endregion
}

/// <summary>
/// 尝试建造塔事件
/// </summary>
public struct TryBuildTowerEvent : IEvent
{
    public GameObject targetTowerSpot;
    public TowerData towerBuildData;
    public int towerLevel;
    public string towerName;
    public Vector3 mousePosition; // 鼠标点击的屏幕位置（用于回放）

    public TryBuildTowerEvent(GameObject targetTowerSpot, TowerData towerBuildData, int towerLevel, string towerName = "", Vector3 mousePosition = default)
    {
        this.targetTowerSpot = targetTowerSpot;
        this.towerBuildData = towerBuildData;
        this.towerLevel = towerLevel;
        this.towerName = towerName;
        this.mousePosition = mousePosition;
    }
}

public struct CompletedBuildEvent : IEvent
{
}