using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 建造 UI 项
/// <para>表示单个可建造塔的 UI 元素</para>
/// </summary>
public class BuildUIItem : MonoBehaviour
{
    #region 序列化字段

    [SerializeField]
    private Button m_button;

    [SerializeField]
    private TextMeshProUGUI m_text;

    #endregion

    #region 私有字段

    private TowerData m_towerBuildData;
    private TowerLevelData m_towerLevelData;
    private int m_towerLevel;
    private string m_towerName;

    #endregion

    #region 属性

    public Button ButtonComponent => m_button;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        InitializeComponents();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置塔等级数据并更新 UI 显示
    /// </summary>
    /// <param name="towerLevelData">塔等级数据</param>
    /// <param name="towerName">塔的名称（可选，用于显示）</param>
    /// <param name="isDefaultClickMethod">是否使用默认点击方法</param>
    public void SetTowerBuildData(TowerData towerBuildData, int towerLevel, string towerName = "", bool isDefaultClickMethod = false)
    {
        m_towerBuildData = towerBuildData;
        m_towerLevelData = towerBuildData.LevelData[towerLevel];
        m_towerLevel = towerLevel;
        m_towerName = towerName;

        UpdateDisplay();

        if (!isDefaultClickMethod)
        {
            UpdateListener();
        }
    }

    /// <summary>
    /// 添加自定义监听器
    /// </summary>
    public void AddListener(Action action)
    {
        if (m_button != null)
        {
            m_button.onClick.AddListener(() => action());
        }
    }

    /// <summary>
    /// 获取当前的塔等级数据
    /// </summary>
    public TowerLevelData GetTowerLevelData()
    {
        return m_towerLevelData;
    }

    /// <summary>
    /// 获取塔名称
    /// </summary>
    public string GetTowerName()
    {
        return m_towerName;
    }

    /// <summary>
    /// 获取按钮组件，用于外部绑定事件
    /// </summary>
    public Button GetButton()
    {
        return m_button;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitializeComponents()
    {
        m_button = GetComponent<Button>();
        m_text = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// 更新显示内容
    /// </summary>
    private void UpdateDisplay()
    {
        if (m_text != null)
        {
            string displayName = string.IsNullOrEmpty(m_towerName) ? "塔" : m_towerName;
            m_text.text = $"{displayName}\n花费: {m_towerLevelData.cost}";
        }
    }

    /// <summary>
    /// 更新按钮监听器
    /// </summary>
    private void UpdateListener()
    {
        if (m_button == null)
        {
            Debug.LogError("[BuildUIItem] Button 组件为空，无法设置监听器");
            return;
        }

        m_button.onClick.RemoveAllListeners();
        m_button.onClick.AddListener(OnButtonClicked);
    }

    /// <summary>
    /// 按钮点击回调
    /// </summary>
    private void OnButtonClicked()
    {
        // 设置建造数据
        BuildingManager.Instance.SetTowerBuildData(m_towerBuildData, m_towerLevel, m_towerName);

        // 发布正在建造事件
        EventBus.Instance.Publish(new BuildingEvent());
    }

    #endregion
}
