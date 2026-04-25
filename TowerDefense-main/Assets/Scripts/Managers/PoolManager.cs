using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池配置项，用于在 Inspector 中配置对象池
/// </summary>
[Serializable]
public class PoolConfig
{
    [Tooltip("预制体对象，必须包含实现 IPoolable 接口的组件")]
    public GameObject prefab;

    [Tooltip("对象池初始大小")]
    public int poolSize = 10;

    [Tooltip("当池为空时是否自动扩展")]
    public bool autoExpand = true;

    [Tooltip("可选：手动指定类型名称（留空则自动检测）")]
    public string typeName = "";
}

/// <summary>
/// 对象池管理器，统一管理游戏中所有对象池，特别是敌人和子弹。
/// 这是一个单例，确保在整个应用程序中只有一个实例。
/// 支持通过 Inspector 灵活配置任意类型的对象池。
/// </summary>
public class PoolManager : MonoBehaviour
{
    #region 单例实现
    private static PoolManager m_instance;

    public static PoolManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("PoolManager");
                m_instance = obj.AddComponent<PoolManager>();
            }
            return m_instance;
        }
    }
    #endregion

    #region 变量
    [Header("对象池配置列表")]
    [Tooltip("在这里添加所有需要池化的对象")]
    [SerializeField]
    private List<PoolConfig> m_poolConfigs = new List<PoolConfig>();

    private readonly Dictionary<string, object> m_pools = new Dictionary<string, object>();
    private Transform m_poolContainer;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        m_instance = this;

        // 创建一个父容器来存放所有对象池，便于在场景中管理
        m_poolContainer = new GameObject("PoolContainer").transform;
        m_poolContainer.SetParent(transform);

        // 自动初始化对象池
        InitializePools();
    }
    #endregion

    #region private方法
    /// <summary>
    /// 初始化所有对象池
    /// </summary>
    private void InitializePools()
    {
        if (m_poolConfigs == null || m_poolConfigs.Count == 0)
        {
            Debug.LogWarning("[PoolManager] - 未配置任何对象池。请在 Inspector 中添加 PoolConfig。");
            return;
        }

        foreach (var config in m_poolConfigs)
        {
            if (config == null)
            {
                Debug.LogWarning("[PoolManager] - 跳过空的配置项。");
                continue;
            }

            if (config.prefab == null)
            {
                Debug.LogWarning("[PoolManager] - 配置项中的预制体为空，已跳过。");
                continue;
            }

            // 尝试自动检测 IPoolable 组件
            Component poolableComponent = FindPoolableComponent(config.prefab);

            if (poolableComponent == null)
            {
                Debug.LogError($"[PoolManager] - 预制体 '{config.prefab.name}' 上没有找到实现 IPoolable 接口的组件，已跳过。");
                continue;
            }

            // 使用反射调用泛型 RegisterPool 方法
            var componentType = poolableComponent.GetType();
            var registerMethod = typeof(PoolManager).GetMethod(nameof(RegisterPool));
            var genericMethod = registerMethod.MakeGenericMethod(componentType);

            genericMethod.Invoke(this, new object[] { poolableComponent, config.poolSize, config.autoExpand });
        }
    }

    /// <summary>
    /// 在预制体上查找实现 IPoolable 接口的组件
    /// </summary>
    private Component FindPoolableComponent(GameObject prefab)
    {
        var components = prefab.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component is IPoolable)
            {
                return component;
            }
        }
        return null;
    }
    #endregion

    #region public方法
    /// <summary>
    /// 注册一个新的对象池（内部方法，也可供外部手动注册其他类型的对象池）。
    /// </summary>
    /// <typeparam name="T">对象类型，必须是实现了IPoolable接口的组件。</typeparam>
    /// <param name="prefab">要池化的预制件。</param>
    /// <param name="initialSize">对象池的初始大小。</param>
    /// <param name="autoExpand">当池为空时是否自动创建新对象。</param>
    public void RegisterPool<T>(T prefab, int initialSize = 10, bool autoExpand = true) where T : Component, IPoolable
    {
        string poolName = typeof(T).Name;
        if (m_pools.ContainsKey(poolName))
        {
            Debug.LogWarning($"[PoolManager] - 名为 '{poolName}' 的对象池已存在。");
            return;
        }

        Transform poolParent = new GameObject($"Pool_{poolName}").transform;
        poolParent.SetParent(m_poolContainer);

        var newPool = new ObjectPool<T>(prefab, poolParent, initialSize, autoExpand);
        m_pools[poolName] = newPool;
    }

    /// <summary>
    /// 从对象池中生成一个对象。
    /// </summary>
    /// <typeparam name="T">要生成的对象类型。</typeparam>
    /// <param name="position">生成位置。</param>
    /// <param name="rotation">生成旋转。</param>
    /// <returns>生成的对象实例。</returns>
    public T Spawn<T>(Vector3 position, Quaternion rotation) where T : Component, IPoolable
    {
        string poolName = typeof(T).Name;
        if (!m_pools.TryGetValue(poolName, out object poolObj) || !(poolObj is ObjectPool<T> pool))
        {
            Debug.LogError($"[PoolManager] - 未找到或类型不匹配的对象池: {poolName}。请先注册。");
            return null;
        }

        return pool.Spawn(position, rotation);
    }

    /// <summary>
    /// 从对象池中生成一个对象（使用默认旋转）。
    /// </summary>
    public T Spawn<T>(Vector3 position) where T : Component, IPoolable
    {
        return Spawn<T>(position, Quaternion.identity);
    }

    /// <summary>
    /// 将一个对象实例回收到其对应的对象池中。
    /// </summary>
    /// <typeparam name="T">要回收的对象类型。</typeparam>
    /// <param name="instance">要回收的对象实例。</param>
    public void Recycle<T>(T instance) where T : Component, IPoolable
    {
        string poolName = typeof(T).Name;
        if (!m_pools.TryGetValue(poolName, out object poolObj) || !(poolObj is ObjectPool<T> pool))
        {
            Debug.LogWarning($"[PoolManager] - 尝试回收对象到一个不存在或类型不匹配的池中: {poolName}");
            // 如果池不存在，直接销毁对象以防内存泄漏
            Destroy(instance.gameObject);
            return;
        }

        pool.Recycle(instance);
    }
    #endregion
}
