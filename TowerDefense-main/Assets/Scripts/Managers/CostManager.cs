using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 成本/金币管理器，负责管理玩家的金币
/// 使用事件系统通知UI和其他系统金币变化
/// </summary>
public class CostManager : MonoBehaviour
{
    #region 变量
    private static CostManager m_instance;
    private int m_playerGold = 1500;
    #endregion

    #region 属性
    public static CostManager Instance
    {
        get
        {
            return m_instance;
        }
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

    /// <summary>
    /// 获取/设置玩家金币
    /// </summary>
    public int PlayerGold
    {
        get => m_playerGold;
        set
        {
            int oldGold = m_playerGold;
            m_playerGold = Mathf.Max(0, value); // 金币不能为负

            if (oldGold != m_playerGold)
            {
                OnPlayerGoldChanged?.Invoke(m_playerGold, oldGold);
            }
        }
    }
    #endregion

    #region 事件
    /// <summary>
    /// 玩家金币变化事件，参数为 (新金币数, 旧金币数)
    /// </summary>
    public Action<int, int> OnPlayerGoldChanged;
    #endregion

    #region Public 方法
    /// <summary>
    /// 尝试花费金币
    /// </summary>
    /// <param name="cost">花费的金币数</param>
    /// <returns>是否成功花费</returns>
    public bool TryCost(int cost)
    {
        if (PlayerGold >= cost)
        {
            PlayerGold -= cost;
            return true;
        }

        Debug.LogWarning($"[CostManager] - 金币不足，需要 {cost}，现有 {PlayerGold}");
        return false;
    }

    /// <summary>
    /// 检查是否有足够的金币
    /// </summary>
    /// <param name="cost">需要的金币数</param>
    /// <returns>是否有足够的金币</returns>
    public bool CanAfford(int cost)
    {
        return PlayerGold >= cost;
    }

    /// <summary>
    /// 增加金币
    /// </summary>
    /// <param name="amount">增加的金币数</param>
    /// <returns>是否成功增加</returns>
    public bool AddGold(int amount)
    {
        if (amount > 0)
        {
            PlayerGold += amount;
            return true;
        }

        Debug.LogWarning($"[CostManager] - 增加金币数必须大于0，收到: {amount}");
        return false;
    }
    #endregion
}
