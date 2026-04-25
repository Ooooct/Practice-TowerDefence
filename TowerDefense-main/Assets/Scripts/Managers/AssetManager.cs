using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 运行时资产管理器，用于加载和提供对游戏中所有通过ScriptableObject管理的资产的访问。
/// 这是一个单例类，确保在整个应用程序中只有一个实例。
/// 支持编辑器模式和运行时模式。
/// </summary>
public class AssetManager : MonoBehaviour
{
    #region 单例实现
    private static AssetManager m_instance;

    public static AssetManager Instance
    {
        get
        {
            if (m_instance == null)
            {
#if UNITY_EDITOR
                // 编辑器模式：查找现有实例或创建临时实例
                if (Application.isPlaying == false)
                {
                    m_instance = FindObjectOfType<AssetManager>();
                    if (m_instance == null)
                    {
                        GameObject obj = new GameObject("AssetManager (Editor)");
                        m_instance = obj.AddComponent<AssetManager>();
                        m_instance.InitializeInEditor();
                    }
                    else if (m_instance.m_assetCache == null)
                    {
                        // 已存在但未初始化
                        m_instance.InitializeInEditor();
                    }
                }
                else
#endif
                {
                    // 运行时模式
                    GameObject obj = new GameObject("AssetManager");
                    m_instance = obj.AddComponent<AssetManager>();
                }
            }
            return m_instance;
        }
    }
    #endregion

    #region 变量
    [SerializeField]
    private ResourcesDatabase m_database;
    private Dictionary<string, Dictionary<string, Object>> m_assetCache;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        // 单例模式的标准实现，确保只有一个实例
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        m_instance = this;

        // 初始化并加载数据库
        Initialize();
    }
    #endregion

    #region public方法
    /// <summary>
    /// 根据类别和键获取资产。
    /// </summary>
    /// <typeparam name="T">要转换成的资产类型。</typeparam>
    /// <param name="categoryKey">资产类别键。</param>
    /// <param name="assetKey">资产键。</param>
    /// <returns>请求的资产，如果未找到则返回null。</returns>
    public T GetAsset<T>(string categoryKey, string assetKey) where T : Object
    {
        if (m_assetCache == null)
        {
            Debug.LogWarning("[AssetManager] - 资产缓存未初始化");
            return null;
        }

        if (m_assetCache.TryGetValue(categoryKey, out var category) && category.TryGetValue(assetKey, out var asset))
        {
            return asset as T;
        }

        Debug.LogWarning($"[AssetManager] - 在类别 '{categoryKey}' 中未找到键为 '{assetKey}' 的资产。");
        return null;
    }

    /// <summary>
    /// 根据类别枚举和键获取资产。
    /// </summary>
    /// <typeparam name="T">要转换成的资产类型。</typeparam>
    /// <param name="category">资产类别枚举。</param>
    /// <param name="assetKey">资产键。</param> 
    /// <returns>请求的资产，如果未找到则返回null。</returns>
    public T GetAsset<T>(CategoriesEnum category, string assetKey) where T : Object
    {
        return GetAsset<T>(category.ToString(), assetKey);
    }

    /// <summary>
    /// 获取指定类别下的所有资产。
    /// </summary>
    /// <typeparam name="T">要转换成的资产类型。</typeparam>
    /// <param name="categoryKey">资产类别键。</param>
    /// <returns>指定类别下的所有资产列表，如果类别不存在则返回空列表。</returns>
    public List<T> GetAllAssets<T>(string categoryKey) where T : Object
    {
        List<T> result = new List<T>();

        if (m_assetCache == null)
        {
            Debug.LogWarning("[AssetManager] - 资产缓存未初始化");
            return result;
        }

        if (m_assetCache.TryGetValue(categoryKey, out var category))
        {
            foreach (var kvp in category)
            {
                T asset = kvp.Value as T;
                if (asset != null)
                {
                    result.Add(asset);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[AssetManager] - 未找到类别 '{categoryKey}'。");
        }

        return result;
    }

    /// <summary>
    /// 获取指定类别枚举下的所有资产。
    /// </summary>
    /// <typeparam name="T">要转换成的资产类型。</typeparam>
    /// <param name="category">资产类别枚举。</param>
    /// <returns>指定类别下的所有资产列表，如果类别不存在则返回空列表。</returns>
    public List<T> GetAllAssets<T>(CategoriesEnum category) where T : Object
    {
        return GetAllAssets<T>(category.ToString());
    }

    /// <summary>
    /// 根据资产对象反向查询其注册的 key
    /// </summary>
    /// <param name="asset">资产对象</param>
    /// <param name="categoryKey">资产类别键</param>
    /// <returns>资产的 key，如果未找到则返回 null</returns>
    public string GetAssetKey(Object asset, string categoryKey)
    {
        if (asset == null)
        {
            Debug.LogWarning("[AssetManager] - 尝试查询空资产的 key");
            return null;
        }

        if (m_assetCache == null)
        {
            Debug.LogWarning("[AssetManager] - 资产缓存未初始化");
            return null;
        }

        if (m_assetCache.TryGetValue(categoryKey, out var category))
        {
            foreach (var kvp in category)
            {
                if (kvp.Value == asset)
                {
                    return kvp.Key;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 根据资产对象反向查询其注册的 key（枚举版本）
    /// </summary>
    /// <param name="asset">资产对象</param>
    /// <param name="category">资产类别枚举</param>
    /// <returns>资产的 key，如果未找到则返回 null</returns>
    public string GetAssetKey(Object asset, CategoriesEnum category)
    {
        return GetAssetKey(asset, category.ToString());
    }

    #endregion

    #region private方法
    /// <summary>
    /// 初始化管理器，加载数据库并缓存所有资产。
    /// </summary>
    private void Initialize()
    {
        // 创建缓存字典
        m_assetCache = new Dictionary<string, Dictionary<string, Object>>();

        // 遍历所有类别并将其资产添加到缓存中
        foreach (var categoryItem in m_database.Categories)
        {
            if (categoryItem.assets == null)
            {
                Debug.LogWarning($"[AssetManager] - 类别 '{categoryItem.key}' 的资产集合为空。");
                continue;
            }

            var assetDict = categoryItem.assets.Assets.ToDictionary(data => data.key, data => data.asset);
            m_assetCache[categoryItem.key] = assetDict;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器模式下的初始化
    /// </summary>
    private void InitializeInEditor()
    {
        // 在编辑器模式下查找 ResourcesDatabase
        if (m_database == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ResourcesDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_database = AssetDatabase.LoadAssetAtPath<ResourcesDatabase>(path);
            }
            else
            {
                Debug.LogError("[AssetManager] - 编辑器模式：未找到 ResourcesDatabase！");
                return;
            }
        }

        // 执行标准初始化
        Initialize();
    }
#endif
    #endregion
}
