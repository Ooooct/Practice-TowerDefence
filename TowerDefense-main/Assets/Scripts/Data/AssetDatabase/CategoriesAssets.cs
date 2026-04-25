using UnityEngine;

/// <summary>
/// 代表一个可被序列化的资产项，包含一个字符串键和一个Unity对象引用。
/// </summary>
[System.Serializable]
public class AssetDataItem
{
    public string key;
    public Object asset;
}

/// <summary>
/// ScriptableObject，用于定义和存储一类资产的集合。
/// 例如，可以创建一个用于存储所有“塔”的预制件，或者所有“敌人”的数据。
/// </summary>
[CreateAssetMenu(fileName = "NewAssetCollection", menuName = "Data/Database/Asset Collection")]
public class CategoriesAssets : ScriptableObject
{
    #region 变量
    [SerializeField]
    private AssetDataItem[] m_assets;
    #endregion

    #region 属性
    public AssetDataItem[] Assets => m_assets;
    #endregion
}
