using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 代表一个可被序列化的资产类别项，包含一个字符串键和一个CategoriesAssets引用。
/// </summary>
[Serializable]
public class AssetCategoryItem
{
    public string key;
    public CategoriesAssets assets;
}

/// <summary>
/// ScriptableObject，用于定义和存储所有资产类别。
/// </summary>
[CreateAssetMenu(fileName = "ResourcesDatabase", menuName = "Data/Database/Resources Database")]
public class ResourcesDatabase : ScriptableObject
{
    #region 变量
    [SerializeField]
    private AssetCategoryItem[] m_categories;
    #endregion

    #region 属性
    public AssetCategoryItem[] Categories => m_categories;
    #endregion
}
