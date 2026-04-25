using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 子弹攻击检测组件，使用组合模式将所有命中逻辑委托给策略对象
/// 纯粹的适配器角色，不包含任何业务逻辑
/// </summary>
[RequireComponent(typeof(Collider))]
public class AttackCheck : BulletComponentBase
{
    [Header("Hit Strategy Configuration")]
    [SerializeField] private List<HitStrategyBase> m_hitStrategyAssets;

    private List<IHitStrategy> m_hitStrategies;
    private HashSet<GameObject> m_hitTargets = new HashSet<GameObject>(); // 记录已命中的目标

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        InitializeStrategy();
    }

    public override void OnRecycle()
    {
        base.OnRecycle();

        // 先通知策略
        if (m_hitStrategies != null)
        {
            foreach (var strategy in m_hitStrategies)
            {
                strategy?.OnRecycle(m_bullet);
            }
        }

        // 清理命中记录
        m_hitTargets?.Clear();

        // 清理列表（在通知策略之后）
        m_hitStrategies?.Clear();
        m_hitStrategyAssets?.Clear();
    }
    #endregion

    #region public方法
    /// <summary>
    /// 处理3D碰撞检测（由BulletMain调用）
    /// 完全委托给策略对象处理
    /// </summary>
    public void HandleTriggerEnter(Collider other)
    {
        if (other == null)
        {
            Debug.LogWarning("[AttackCheck] - Collider 为空");
            return;
        }

        if (m_hitStrategies == null || m_hitStrategies.Count == 0)
        {
            Debug.LogWarning("[AttackCheck] - 未设置命中策略，无法处理碰撞");
            return;
        }

        if (m_bullet == null)
        {
            Debug.LogError("[AttackCheck] - 子弹实例为空，无法处理碰撞");
            return;
        }

        // 获取碰撞对象的根GameObject（处理复合碰撞器的情况）
        GameObject target = other.gameObject;

        // 检查是否已命中过此目标（避免重复判定）
        if (m_hitTargets.Contains(target))
        {
            Debug.LogWarning($"[AttackCheck] - 目标 {target.name} (InstanceID: {target.GetInstanceID()}) 已被命中，跳过重复判定");
            return;
        }

        // 记录命中目标
        m_hitTargets.Add(target);

        // 遍历所有策略并执行
        foreach (var strategy in m_hitStrategies)
        {
            if (strategy != null)
            {
                strategy.HandleTriggerEnter(other, m_bullet);
            }
        }
    }

    /// <summary>
    /// 检查目标是否已被命中
    /// </summary>
    public bool HasHitTarget(GameObject target)
    {
        return m_hitTargets.Contains(target);
    }

    /// <summary>
    /// 获取已命中目标数量
    /// </summary>
    public int GetHitCount()
    {
        return m_hitTargets.Count;
    }

    /// <summary>
    /// 手动添加命中记录（供特殊情况使用）
    /// </summary>
    public void RecordHit(GameObject target)
    {
        if (target != null)
        {
            m_hitTargets.Add(target);
        }
    }

    /// <summary>
    /// 重置命中记录（子弹生成时调用）
    /// </summary>
    public void ResetHitTargets()
    {
        m_hitTargets?.Clear();
    }

    /// <summary>
    /// 设置命中策略（运行时配置）
    /// </summary>
    public void AddHitStrategy(IHitStrategy strategy)
    {
        if (m_hitStrategies == null)
        {
            m_hitStrategies = new List<IHitStrategy>();
        }

        if (strategy != null)
        {
            m_hitStrategies.Add(strategy);
        }
    }

    /// <summary>
    /// 设置命中策略（通过名称从资源管理器获取）
    /// </summary>
    public void AddHitStrategy(string strategyName)
    {
        if (m_hitStrategies == null)
        {
            m_hitStrategies = new List<IHitStrategy>();
        }

        var strategy = AssetManager.Instance.GetAsset<HitStrategyBase>(CategoriesEnum.HitStrategy, strategyName);
        if (strategy != null)
        {
            m_hitStrategies.Add(strategy);
            Debug.Log($"[AttackCheck] - 添加命中策略: {strategyName}");
        }
        else
        {
            Debug.LogWarning($"[AttackCheck] - 未找到命中策略资源: {strategyName}");
        }
    }

    /// <summary>
    /// 设置命中策略资源（ScriptableObject）
    /// </summary>
    public void AddHitStrategyAsset(HitStrategyBase strategyAsset)
    {
        if (m_hitStrategyAssets == null)
        {
            m_hitStrategyAssets = new List<HitStrategyBase>();
        }

        if (strategyAsset != null)
        {
            m_hitStrategyAssets.Add(strategyAsset);
            InitializeStrategy();
        }
    }

    public void SetHitStrategy(IHitStrategy strategy)
    {
        if (m_hitStrategies == null)
        {
            m_hitStrategies = new List<IHitStrategy>();
        }
        else
        {
            m_hitStrategies.Clear();
        }

        if (strategy != null)
        {
            m_hitStrategies.Add(strategy);
        }
    }

    public void SetHitStrategy(string strategy)
    {
        if (m_hitStrategies == null)
        {
            m_hitStrategies = new List<IHitStrategy>();
        }
        else
        {
            m_hitStrategies.Clear();
        }

        AddHitStrategy(strategy);
    }

    public void SetHitStrategyAsset(HitStrategyBase strategyAsset)
    {
        if (m_hitStrategies == null)
        {
            m_hitStrategies = new List<IHitStrategy>();
        }
        else
        {
            m_hitStrategies.Clear();
        }

        if (m_hitStrategyAssets == null)
        {
            m_hitStrategyAssets = new List<HitStrategyBase>();
        }
        else
        {
            m_hitStrategyAssets.Clear();
        }

        if (strategyAsset != null)
        {
            m_hitStrategyAssets.Add(strategyAsset);
            InitializeStrategy();
        }
    }
    #endregion

    #region private方法
    /// <summary>
    /// 初始化策略
    /// </summary>
    private void InitializeStrategy()
    {
        // 确保列表已初始化
        if (m_hitStrategies == null)
        {
            m_hitStrategies = new List<IHitStrategy>();
        }
        else
        {
            m_hitStrategies.Clear();
        }

        if (m_hitStrategyAssets != null && m_hitStrategyAssets.Count > 0)
        {
            foreach (var strategyAsset in m_hitStrategyAssets)
            {
                if (strategyAsset != null)
                {
                    m_hitStrategies.Add(strategyAsset);
                }
                else
                {
                    Debug.LogWarning("[AttackCheck] - 命中策略资源列表中存在空项");
                }
            }
        }
    }
    #endregion
}
