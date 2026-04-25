using UnityEngine;

/// <summary>
/// 数据重载事件，当 ScriptableObject 数据从 JSON 重新加载时触发
/// </summary>
public struct DataReloadedEvent : IEvent
{
    /// <summary>
    /// 资产类别
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// 资产在 AssetManager 中注册的键（名称）
    /// </summary>
    public string AssetKey { get; }

    /// <summary>
    /// 重新加载的数据对象
    /// </summary>
    public Object ReloadedData { get; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public System.Type DataType { get; }

    public DataReloadedEvent(string category, string assetKey, Object reloadedData)
    {
        Category = category;
        AssetKey = assetKey;
        ReloadedData = reloadedData;
        DataType = reloadedData?.GetType();
    }

    public override string ToString()
    {
        return $"DataReloadedEvent: {Category}/{AssetKey} ({DataType?.Name})";
    }
}
