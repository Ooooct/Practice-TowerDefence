using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 建造 UI 控制器
/// <para>负责处理塔的建造和升级 UI 交互</para>
/// </summary>
public class BuildUI : MonoBehaviour
{
    #region 序列化字段

    [Header("射线检测配置")]
    [SerializeField]
    [Tooltip("射线检测的最大距离")]
    private float m_raycastMaxDistance = 100f;

    [Header("UI 引用")]
    [SerializeField]
    [Tooltip("建造菜单视图")]
    private BuildUIView m_buildView;

    [SerializeField]
    [Tooltip("升级菜单")]
    private UpgradeUI m_upgradeUI;

    #endregion

    #region 私有字段
    private InputType m_LastShow = InputType.Update;
    private bool m_isBuilding = false;

    #endregion

    #region 枚举
    enum InputType
    {
        Build = 0,   // 建造模式
        Update = 1   // 升级模式
    }

    #endregion

    #region Unity 生命周期

    void Start()
    {
        InitializeComponents();
        SubscribeEvents();
        ShowBuildMenu();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitializeComponents()
    {
        m_buildView ??= GetComponent<BuildUIView>();

        if (m_upgradeUI != null)
        {
            m_upgradeUI.Hide();
        }
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeEvents()
    {
        EventBus.Instance.Subscribe<LeftClickEvent>(OnLeftClick);
        EventBus.Instance.Subscribe<RightClickEvent>(OnRightClick);
        EventBus.Instance.Subscribe<BuildingEvent>(OnIsBuilding);
        EventBus.Instance.Subscribe<CompletedBuildEvent>(OnCompletedBuild);
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    private void UnsubscribeEvents()
    {
        EventBus.Instance.Unsubscribe<LeftClickEvent>(OnLeftClick);
        EventBus.Instance.Unsubscribe<RightClickEvent>(OnRightClick);
        EventBus.Instance.Unsubscribe<BuildingEvent>(OnIsBuilding);
        EventBus.Instance.Unsubscribe<CompletedBuildEvent>(OnCompletedBuild);
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理左键点击事件
    /// </summary>
    private void OnLeftClick(LeftClickEvent evt)
    {
        // 检查是否点击到了UI元素
        if (IsPointerOverUI())
        {
            return;
        }

        HandleWorldClick();
    }

    /// <summary>
    /// 处理右键点击事件
    /// </summary>
    private void OnRightClick(RightClickEvent evt)
    {
        ShowBuildMenu();
    }

    /// <summary>
    /// 处理开始建造事件
    /// </summary>
    private void OnIsBuilding(BuildingEvent evt)
    {
        m_isBuilding = true;
    }

    /// <summary>
    /// 处理建造完成事件
    /// </summary>
    private void OnCompletedBuild(CompletedBuildEvent evt)
    {
        StartCoroutine(ResetBuildingFlag());
    }

    #endregion

    #region 输入处理

    /// <summary>
    /// 检查指针是否在 UI 元素上
    /// </summary>
    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 处理世界空间点击
    /// </summary>
    private void HandleWorldClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, m_raycastMaxDistance))
        {
            if (hit.collider.CompareTag("Tower") && hit.collider.gameObject.name != "Default")
            {
                ShowUpdateMenu(hit.collider.gameObject);
            }
            else
            {
                HandleEmptySpaceClick();
            }
        }
        else
        {
            // 没有点击到任何物体
            HideUpgradeUI();
        }
    }

    /// <summary>
    /// 处理点击空白区域
    /// </summary>
    private void HandleEmptySpaceClick()
    {
        // 只有在没有正在建造的单位时才显示建造菜单
        if (CanShowBuildMenu())
        {
            ShowBuildMenu();
        }

        HideUpgradeUI();
    }

    #endregion

    #region UI 显示控制

    /// <summary>
    /// 显示升级菜单
    /// </summary>
    /// <param name="towerObject">被点击的塔对象</param>
    private void ShowUpdateMenu(GameObject towerObject)
    {
        // 只有在没有正在建造的单位时且不在建造状态才显示升级菜单
        if (!CanShowUpgradeMenu() || m_isBuilding == true)
        {
            return;
        }

        // 获取塔的建造组件
        TowerBuilder towerBuilder = GetTowerBuilder(towerObject);
        if (towerBuilder == null)
        {
            return;
        }

        // 显示对应的升级菜单
        if (m_upgradeUI != null)
        {
            m_upgradeUI.gameObject.SetActive(true);

            int currentLevel = towerBuilder.CurrentLevel;
            int maxLevel = towerBuilder.MaxLevel;

            // 修复：当前等级小于最大等级时才能升级
            if (currentLevel < maxLevel)
            {
                ShowUpgradeMenu(towerObject, towerBuilder);
            }
            else
            {
                ShowMaxLevelMenu(towerObject, towerBuilder);
            }
        }
    }

    /// <summary>
    /// 显示升级菜单（未满级）
    /// </summary>
    private void ShowUpgradeMenu(GameObject towerObject, TowerBuilder towerBuilder)
    {
        int currentLevel = towerBuilder.CurrentLevel;
        int nextLevel = currentLevel + 1;
        TowerData buildData = towerBuilder.TowerBuildData;

        // 安全检查
        if (buildData.LevelData == null || nextLevel >= buildData.LevelData.Count)
        {
            Debug.LogError($"[BuildUI] 无法显示升级菜单：下一级索引 {nextLevel} 超出范围（共 {buildData.LevelData?.Count ?? 0} 级）");
            ShowMaxLevelMenu(towerObject, towerBuilder);
            return;
        }

        string towerName = buildData.LevelData[nextLevel].name;
        m_upgradeUI.Show(towerObject, buildData, nextLevel, towerName);
    }

    /// <summary>
    /// 显示满级菜单（只有拆除）
    /// </summary>
    private void ShowMaxLevelMenu(GameObject towerObject, TowerBuilder towerBuilder)
    {
        m_upgradeUI.ShowMaxLevel(towerObject);
    }

    /// <summary>
    /// 隐藏升级 UI
    /// </summary>
    private void HideUpgradeUI()
    {
        if (m_upgradeUI != null)
        {
            m_upgradeUI.Hide();
        }
    }

    /// <summary>
    /// 显示建造菜单
    /// </summary>
    private void ShowBuildMenu()
    {
        if (m_LastShow == InputType.Build)
        {
            return;
        }

        m_LastShow = InputType.Build;

        HideUpgradeUI();

        // 获取所有可建造的塔数据
        var towerDataList = AssetManager.Instance.GetAllAssets<TowerData>(CategoriesEnum.Towers);
        if (towerDataList == null || towerDataList.Count == 0)
        {
            Debug.LogWarning("[BuildUI] 没有可用的塔数据");
            return;
        }

        // 显示建造视图
        if (m_buildView != null)
        {
            m_buildView.Show();
            m_buildView.ShowTowerBuildingData(towerDataList, towerDataList.Count > 0 ? towerDataList[0].name : "塔");
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取塔的 TowerBuilder 组件
    /// </summary>
    private TowerBuilder GetTowerBuilder(GameObject towerObject)
    {
        TowerMain towerMain = towerObject.GetComponent<TowerMain>();
        if (towerMain == null)
        {
            Debug.LogWarning($"[BuildUI] GameObject '{towerObject.name}' 没有 TowerMain 组件");
            return null;
        }

        TowerBuilder towerBuilder = towerMain.GetObjectComponent<TowerBuilder>();
        if (towerBuilder == null)
        {
            Debug.LogWarning($"[BuildUI] TowerMain 上没有找到 TowerBuilder 组件");
            return null;
        }

        return towerBuilder;
    }


    /// <summary>
    /// 检查是否可以显示建造菜单
    /// </summary>
    private bool CanShowBuildMenu()
    {
        return BuildingManager.Instance != null && !BuildingManager.Instance.HasBuildData();
    }

    /// <summary>
    /// 检查是否可以显示升级菜单
    /// </summary>
    private bool CanShowUpgradeMenu()
    {
        return BuildingManager.Instance != null && !BuildingManager.Instance.HasBuildData();
    }

    /// <summary>
    /// 重置建造标志（延迟到帧末尾）
    /// </summary>
    private IEnumerator ResetBuildingFlag()
    {
        yield return new WaitForEndOfFrame();
        m_isBuilding = false;
    }

    #endregion
}

/// <summary>
/// 正在建造事件
/// </summary>
public struct BuildingEvent : IEvent
{
}
