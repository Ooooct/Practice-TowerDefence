using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级 UI 控制器
/// <para>负责显示塔的拆除和升级选项</para>
/// <list type="bullet">
/// <item>显示两个固定按钮：拆除（Default）和升级（下一级）</item>
/// <item>当点击其他塔或右键时自动隐藏</item>
/// </list>
/// </summary>
public class UpgradeUI : MonoBehaviour
{
    #region 序列化字段

    [Header("UI 组件")]
    [SerializeField]
    [Tooltip("拆除按钮 GameObject")]
    private GameObject m_demolishButton;

    [SerializeField]
    [Tooltip("升级按钮 GameObject")]
    private GameObject m_upgradeButton;

    #endregion

    #region 私有字段

    private GameObject m_currentTower;
    private RectTransform m_rectTransform;
    private Button m_demolishButtonComponent;
    private Button m_upgradeButtonComponent;
    private TextMeshProUGUI m_demolishText;
    private TextMeshProUGUI m_upgradeText;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        SubscribeEvents();
    }

    private void OnDestroy()
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
        m_rectTransform = GetComponent<RectTransform>();
        if (m_rectTransform == null)
        {
            Debug.LogError("[UpgradeUI] 未找到 RectTransform 组件！");
        }

        InitializeDemolishButton();
        InitializeUpgradeButton();
    }

    /// <summary>
    /// 初始化拆除按钮
    /// </summary>
    private void InitializeDemolishButton()
    {
        if (m_demolishButton == null)
        {
            Debug.LogError("[UpgradeUI] 拆除按钮 GameObject 未设置！");
            return;
        }

        m_demolishButtonComponent = m_demolishButton.GetComponent<Button>();
        m_demolishText = m_demolishButton.GetComponentInChildren<TextMeshProUGUI>();

        if (m_demolishButtonComponent == null)
        {
            Debug.LogError("[UpgradeUI] 拆除按钮未找到 Button 组件！");
        }

        if (m_demolishText == null)
        {
            Debug.LogWarning("[UpgradeUI] 拆除按钮未找到 TextMeshProUGUI 组件！");
        }
    }

    /// <summary>
    /// 初始化升级按钮
    /// </summary>
    private void InitializeUpgradeButton()
    {
        if (m_upgradeButton == null)
        {
            Debug.LogError("[UpgradeUI] 升级按钮 GameObject 未设置！");
            return;
        }

        m_upgradeButtonComponent = m_upgradeButton.GetComponent<Button>();
        m_upgradeText = m_upgradeButton.GetComponentInChildren<TextMeshProUGUI>();

        if (m_upgradeButtonComponent == null)
        {
            Debug.LogError("[UpgradeUI] 升级按钮未找到 Button 组件！");
        }

        if (m_upgradeText == null)
        {
            Debug.LogWarning("[UpgradeUI] 升级按钮未找到 TextMeshProUGUI 组件！");
        }
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeEvents()
    {
        EventBus.Instance.Subscribe<RightClickEvent>(OnRightClick);
        EventBus.Instance.Subscribe<CompletedBuildEvent>(OnBuildCompleted);
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<RightClickEvent>(OnRightClick);
            EventBus.Instance.Unsubscribe<CompletedBuildEvent>(OnBuildCompleted);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示升级菜单（未满级）
    /// </summary>
    /// <param name="tower">要显示升级菜单的塔对象</param>
    /// <param name="buildData">塔建造数据</param>
    /// <param name="level">下一级的等级索引</param>
    /// <param name="towerName">塔的名称</param>
    public void Show(GameObject tower, TowerData buildData, int level, string towerName)
    {
        m_currentTower = tower;

        // 验证数据有效性
        if (buildData == null || buildData.LevelData == null)
        {
            Debug.LogError("[UpgradeUI] TowerBuildData 或 LevelData 为空");
            return;
        }

        // 检查等级索引是否有效
        if (level < 0 || level >= buildData.LevelData.Count)
        {
            Debug.LogWarning($"[UpgradeUI] 等级索引 {level} 超出范围，塔已满级，显示拆除菜单");
            ShowMaxLevel(tower);
            return;
        }

        TowerData defaultData = GetDefaultTowerData();
        if (defaultData.Equals(default(TowerData)) || defaultData == null)
        {
            return;
        }

        SetupDemolishButton(tower, defaultData);
        SetupUpgradeButton(tower, buildData, level, towerName);

        SetPositionToTower(tower);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 显示满级塔的菜单（只有拆除选项）
    /// </summary>
    /// <param name="tower">要显示菜单的塔对象</param>
    public void ShowMaxLevel(GameObject tower)
    {
        m_currentTower = tower;

        TowerData defaultData = GetDefaultTowerData();
        if (defaultData.Equals(default(TowerData)))
        {
            return;
        }

        SetupDemolishButton(tower, defaultData);

        if (m_upgradeButton != null)
        {
            m_upgradeButton.SetActive(false);
        }

        SetPositionToTower(tower);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏升级菜单（延迟执行）
    /// </summary>
    public void Hide()
    {
        if (isActiveAndEnabled)
        {
            StartCoroutine(HideNextFrame());
        }
    }

    #endregion

    #region 按钮设置

    /// <summary>
    /// 设置拆除按钮
    /// </summary>
    private void SetupDemolishButton(GameObject tower, TowerData defaultData)
    {
        if (m_demolishButton == null || m_demolishButtonComponent == null)
        {
            Debug.LogError($"[UpgradeUI] 无法设置拆除按钮");
            return;
        }

        // 安全检查：确保 defaultData 有效
        if (defaultData.LevelData == null || defaultData.LevelData.Count == 0)
        {
            Debug.LogError($"[UpgradeUI] 拆除按钮设置失败：defaultData.LevelData 为空或无数据");
            m_demolishButton.SetActive(false);
            return;
        }

        m_demolishButton.SetActive(true);

        // 更新文本
        if (m_demolishText != null)
        {
            m_demolishText.text = $"拆除\n花费: {defaultData.LevelData[0].cost}";
        }

        // 设置点击事件
        m_demolishButtonComponent.onClick.RemoveAllListeners();
        m_demolishButtonComponent.onClick.AddListener(() =>
        {
            OnDemolishClicked(tower, defaultData);
        });
    }

    /// <summary>
    /// 设置升级按钮
    /// </summary>
    private void SetupUpgradeButton(GameObject tower, TowerData nextBuildData, int level, string towerName)
    {
        if (m_upgradeButton == null || m_upgradeButtonComponent == null)
        {
            Debug.LogError($"[UpgradeUI] 无法设置升级按钮");
            return;
        }

        // 安全检查：确保 level 索引有效
        if (nextBuildData.LevelData == null || level < 0 || level >= nextBuildData.LevelData.Count)
        {
            Debug.LogError($"[UpgradeUI] 升级按钮设置失败：等级索引 {level} 超出范围（共 {nextBuildData.LevelData?.Count ?? 0} 级）");
            m_upgradeButton.SetActive(false);
            return;
        }

        m_upgradeButton.SetActive(true);

        // 更新文本
        if (m_upgradeText != null)
        {
            m_upgradeText.text = $"升级 {towerName}\n花费: {nextBuildData.LevelData[level].cost}";
        }

        // 设置点击事件
        m_upgradeButtonComponent.onClick.RemoveAllListeners();
        m_upgradeButtonComponent.onClick.AddListener(() =>
        {
            OnUpgradeClicked(tower, nextBuildData, level, towerName);
        });
    }

    #endregion

    #region 按钮回调

    /// <summary>
    /// 拆除按钮点击回调
    /// </summary>
    private void OnDemolishClicked(GameObject tower, TowerData defaultData)
    {
        TryBuildTowerEvent evt = new TryBuildTowerEvent(tower, defaultData, 0, "拆除");
        EventBus.Instance.Publish(evt);
    }

    /// <summary>
    /// 升级按钮点击回调
    /// </summary>
    private void OnUpgradeClicked(GameObject tower, TowerData nextLevelData, int nextLevel, string towerName)
    {
        TryBuildTowerEvent evt = new TryBuildTowerEvent(tower, nextLevelData, nextLevel, $"升级 {towerName}");
        EventBus.Instance.Publish(evt);
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理右键点击事件
    /// </summary>
    private void OnRightClick(RightClickEvent evt)
    {
        Hide();
    }

    /// <summary>
    /// 处理建造完成事件
    /// </summary>
    private void OnBuildCompleted(CompletedBuildEvent evt)
    {
        Hide();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取 Default 塔数据
    /// </summary>
    private TowerData GetDefaultTowerData()
    {
        TowerData defaultTower = AssetManager.Instance.GetAsset<TowerData>(
            CategoriesEnum.Towers,
            "Default"
        );

        return defaultTower;
    }

    /// <summary>
    /// 设置 UI 位置到塔的屏幕空间位置
    /// <para>Canvas 使用 ScaleWithScreenSize，需要特殊处理坐标转换</para>
    /// </summary>
    private void SetPositionToTower(GameObject tower)
    {
        if (m_rectTransform == null || tower == null)
        {
            Debug.LogWarning("[UpgradeUI] RectTransform 或 Tower 为空，无法设置位置");
            return;
        }

        Canvas canvas = m_rectTransform.GetComponentInParent<Canvas>();

        // 世界坐标 → 屏幕坐标 → Canvas 本地坐标
        Vector3 worldPosition = tower.transform.position;
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPosition,
            canvas.worldCamera,
            out Vector2 localPosition
        );

        m_rectTransform.anchoredPosition = localPosition;
    }

    /// <summary>
    /// 延迟隐藏（等待当前帧完成）
    /// </summary>
    private IEnumerator HideNextFrame()
    {
        yield return null;
        yield return new WaitForSeconds(0.05f);

        m_currentTower = null;
        gameObject.SetActive(false);
    }

    #endregion
}