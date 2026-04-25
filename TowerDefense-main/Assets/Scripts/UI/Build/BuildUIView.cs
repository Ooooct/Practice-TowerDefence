using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 建造菜单视图
/// <para>负责显示可建造的塔列表</para>
/// </summary>
public class BuildUIView : MonoBehaviour
{
    #region 序列化字段

    [Header("预制体配置")]
    [SerializeField]
    [Tooltip("BuildUIItem 预制体")]
    private GameObject m_buildUIItemPrefab;

    [Header("容器配置")]
    [SerializeField]
    [Tooltip("用于放置 BuildUIItem 的父容器")]
    private Transform m_itemContainer;

    #endregion

    #region 私有字段

    /// <summary>
    /// 当前生成的 UI Item 列表
    /// </summary>
    private List<BuildUIItem> m_currentItems = new List<BuildUIItem>();

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示塔建造数据列表
    /// <para>会清除旧的 UI 元素，并为每个塔等级数据创建一个 BuildUIItem</para>
    /// </summary>
    /// <param name="towerBuildDataList">塔等级数据列表</param>
    /// <param name="towerBuildDataName">塔名称（用于显示）</param>
    public void ShowTowerBuildingData(List<TowerData> towerBuildDataList, string towerBuildDataName = "")
    {
        ClearItems();

        if (towerBuildDataList == null || towerBuildDataList.Count == 0)
        {
            return;
        }

        Debug.Log($"[BuildUIView] 开始生成 {towerBuildDataList.Count} 个塔建造 UI Item");

        CreateTowerItems(towerBuildDataList, towerBuildDataName);

        Debug.Log($"[BuildUIView] 完成生成，共 {m_currentItems.Count} 个 UI Item");
    }

    /// <summary>
    /// 显示建造菜单
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏建造菜单
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 清除所有当前的 UI Item
    /// </summary>
    public void ClearItems()
    {
        if (m_currentItems.Count > 0)
        {
            Debug.Log($"[BuildUIView] 清除 {m_currentItems.Count} 个 UI Item");
        }

        foreach (var item in m_currentItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        m_currentItems.Clear();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 验证输入参数
    /// </summary>
    private bool ValidateInputs(List<TowerLevelData> towerLevelDataList)
    {
        if (towerLevelDataList == null || towerLevelDataList.Count == 0)
        {
            Debug.LogWarning("[BuildUIView] 塔等级数据列表为空");
            return false;
        }

        if (m_buildUIItemPrefab == null)
        {
            Debug.LogError("[BuildUIView] BuildUIItem 预制体未设置！");
            return false;
        }

        if (m_itemContainer == null)
        {
            Debug.LogError("[BuildUIView] Item 容器未设置！");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 创建塔 UI 项
    /// </summary>
    private void CreateTowerItems(List<TowerData> towerBuildDataList, string towerBuildDataName)
    {
        foreach (var towerBuildData in towerBuildDataList)
        {
            BuildUIItem item = CreateSingleItem(towerBuildData, towerBuildDataName);
            if (item != null)
            {
                m_currentItems.Add(item);
                // 使用实际的塔名称记录日志
                string actualName = towerBuildData.LevelData[0].name;
                Debug.Log($"[BuildUIView] 创建了塔 UI Item: {actualName}, 花费: {towerBuildData.LevelData[0].cost}");
            }
        }
    }

    /// <summary>
    /// 创建单个 UI 项
    /// </summary>
    private BuildUIItem CreateSingleItem(TowerData towerBuildData, string towerName)
    {
        GameObject itemObj = Instantiate(m_buildUIItemPrefab, m_itemContainer);
        BuildUIItem item = itemObj.GetComponent<BuildUIItem>();

        if (item == null)
        {
            Debug.LogError("[BuildUIView] 预制体上没有 BuildUIItem 组件！");
            Destroy(itemObj);
            return null;
        }

        // 使用 TowerBuildData.LevelData[0].name 作为塔名称
        string actualTowerName = towerBuildData.LevelData[0].name;
        item.SetTowerBuildData(towerBuildData, 0, actualTowerName);
        return item;
    }

    #endregion
}
