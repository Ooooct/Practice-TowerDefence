using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Unity 资产引用的 JSON 转换器
/// 将 Unity Object 引用序列化为 "category/key" 格式的字符串
/// 并在反序列化时通过 AssetManager 加载对应资产
/// </summary>
public class AssetConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        // 检查是否是 Unity Object 或其子类
        if (typeof(Object).IsAssignableFrom(objectType))
        {
            // 排除 MonoBehaviour 和 Component (这些不应该被序列化为资产引用)
            if (typeof(MonoBehaviour).IsAssignableFrom(objectType) ||
                typeof(Component).IsAssignableFrom(objectType))
            {
                return false;
            }
            return true;
        }
        return false;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        Object unityObject = value as Object;
        if (unityObject == null)
        {
            writer.WriteNull();
            return;
        }

#if UNITY_EDITOR
        // 在编辑器模式下，尝试通过 AssetManager 获取资产的 category 和 key
        string assetReference = GetAssetReference(unityObject);

        if (!string.IsNullOrEmpty(assetReference))
        {
            writer.WriteValue(assetReference);
        }
        else
        {
            // 如果无法通过 AssetManager 找到，尝试使用资产路径
            string assetPath = AssetDatabase.GetAssetPath(unityObject);
            if (!string.IsNullOrEmpty(assetPath))
            {
                writer.WriteValue($"path:{assetPath}");
            }
            else
            {
                Debug.LogWarning($"无法序列化资产引用: {unityObject.name}，该资产未在 AssetManager 中注册");
                writer.WriteNull();
            }
        }
#else
        writer.WriteNull();
#endif
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        string reference = reader.Value as string;
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        // 处理 "path:" 前缀的资产路径
        if (reference.StartsWith("path:"))
        {
#if UNITY_EDITOR
            string assetPath = reference.Substring(5); // 移除 "path:" 前缀
            return AssetDatabase.LoadAssetAtPath(assetPath, objectType);
#else
            Debug.LogWarning($"运行时无法加载资产路径: {reference}");
            return null;
#endif
        }

        // 处理 "category/key" 格式
        string[] parts = reference.Split('/');
        if (parts.Length != 2)
        {
            Debug.LogWarning($"无效的资产引用格式: {reference}，应为 'category/key' 格式");
            return null;
        }

        string category = parts[0];
        string key = parts[1];

        // 通过 AssetManager 加载资产
        try
        {
            var assetManager = AssetManager.Instance;
            var asset = assetManager.GetAsset<Object>(category, key);

            if (asset == null)
            {
                Debug.LogWarning($"无法加载资产: category='{category}', key='{key}'");
            }

            return asset;
        }
        catch (Exception e)
        {
            Debug.LogError($"加载资产时出错: {reference}. 错误: {e.Message}");
            return null;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 获取 Unity Object 的资产引用字符串 (category/key 格式)
    /// </summary>
    private string GetAssetReference(Object unityObject)
    {
        var assetManager = AssetManager.Instance;

        // 尝试所有可能的类别
        foreach (CategoriesEnum category in Enum.GetValues(typeof(CategoriesEnum)))
        {
            string key = assetManager.GetAssetKey(unityObject, category);
            if (!string.IsNullOrEmpty(key))
            {
                return $"{category}/{key}";
            }
        }

        return null;
    }
#endif
}
