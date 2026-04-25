using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP UI 管理器，负责管理所有敌人的生命值显示
/// </summary>
public class HPUIManager : MonoBehaviour
{
    #region 单例
    public static HPUIManager Instance { get; private set; }
    #endregion

    #region 序列化字段
    [Header("引用")]
    [SerializeField] private Canvas m_canvas;
    [SerializeField] private Transform m_hpItemContainer;
    [SerializeField] private Camera m_mainCamera;
    #endregion

    #region 变量
    private Dictionary<EnemyMain, HPUIItem> m_activeItems = new Dictionary<EnemyMain, HPUIItem>();
    readonly Vector3 POSITION_OUTSIDE_SCREEN = new Vector3(1919, 114, 514);
    #endregion

    #region Unity 生命周期
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 获取主摄像机
        if (m_mainCamera == null)
        {
            m_mainCamera = Camera.main;
        }

        // 自动获取 Canvas 组件
        if (m_canvas == null && m_hpItemContainer != null)
        {
            m_canvas = m_hpItemContainer.GetComponentInParent<Canvas>();
            if (m_canvas == null)
            {
                Debug.LogError("[HPUIManager] - 无法找到 Canvas 组件，请确保 HP Item Container 在 Canvas 下");
            }
        }

        // 在 Awake 中订阅事件，确保不会错过任何敌人添加事件
        UnitManager.OnEnemyAdded += HandleEnemyAdded;
        UnitManager.OnEnemyRemoved += HandleEnemyRemoved;
    }

    private void OnDestroy()
    {
        // 取消订阅
        UnitManager.OnEnemyAdded -= HandleEnemyAdded;
        UnitManager.OnEnemyRemoved -= HandleEnemyRemoved;
    }

    private void Update()
    {
        // 更新所有激活的 HPUIItem 位置
        foreach (var item in m_activeItems.Values)
        {
            if (item != null)
            {
                item.UpdatePosition(m_mainCamera, m_canvas);
            }
        }
    }
    #endregion

    #region Private 方法
    /// <summary>
    /// 处理敌人添加事件
    /// </summary>
    private void HandleEnemyAdded(EnemyMain enemy)
    {
        if (enemy == null)
        {
            return;
        }

        // 检查是否已经有对应的 HPUIItem
        if (m_activeItems.ContainsKey(enemy))
        {
            Debug.LogWarning($"[HPUIManager] - 敌人 {enemy.name} 已经有对应的 HPUIItem");
            return;
        }

        // 从对象池获取 HPUIItem
        HPUIItem item = PoolManager.Instance.Spawn<HPUIItem>(POSITION_OUTSIDE_SCREEN, Quaternion.identity);

        if (item == null)
        {
            Debug.LogError("[HPUIManager] - 无法从对象池获取 HPUIItem，请确保 PoolManager 中已配置 HPUIItem Prefab");
            return;
        }

        // 设置为 Canvas 的子物体
        item.transform.SetParent(m_hpItemContainer, false);

        // 绑定目标
        item.BindTarget(enemy);

        // 添加到激活列表
        m_activeItems.Add(enemy, item);
    }

    /// <summary>
    /// 处理敌人移除事件
    /// </summary>
    private void HandleEnemyRemoved(EnemyMain enemy)
    {
        if (enemy == null)
        {
            return;
        }

        // 查找对应的 HPUIItem
        if (m_activeItems.TryGetValue(enemy, out HPUIItem item))
        {
            // 回收到对象池
            if (item != null)
            {
                PoolManager.Instance.Recycle(item);
            }

            m_activeItems.Remove(enemy);
        }
    }
    #endregion
}
