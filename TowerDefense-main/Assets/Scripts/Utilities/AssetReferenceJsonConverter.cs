using System;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Unity 资产引用的 JSON 转换器
/// 用于序列化/反序列化时处理 Unity Object 引用（转换为 category/key 格式）
/// </summary>
public class AssetReferenceJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Object).IsAssignableFrom(objectType) &&
               !typeof(MonoBehaviour).IsAssignableFrom(objectType) &&
               !typeof(Component).IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

#if UNITY_EDITOR
        Object unityObject = value as Object;
        if (unityObject == null)
        {
            writer.WriteNull();
            return;
        }

        string reference = GetAssetReference(unityObject);
        writer.WriteValue(reference ?? "");
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

#if UNITY_EDITOR
        return LoadAssetReference(reference, objectType);
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    private string GetAssetReference(Object unityObject)
    {
        if (unityObject == null) return null;

        var assetManager = AssetManager.Instance;

        // 尝试通过 AssetManager 获取引用
        foreach (CategoriesEnum category in Enum.GetValues(typeof(CategoriesEnum)))
        {
            string key = assetManager.GetAssetKey(unityObject, category);
            if (!string.IsNullOrEmpty(key))
            {
                return $"{category}/{key}";
            }
        }

        // 备用：使用资产路径
        string assetPath = AssetDatabase.GetAssetPath(unityObject);
        if (!string.IsNullOrEmpty(assetPath))
        {
            return $"path:{assetPath}";
        }

        return null;
    }

    private Object LoadAssetReference(string reference, Type objectType)
    {
        if (string.IsNullOrEmpty(reference)) return null;

        // 处理 "path:" 前缀
        if (reference.StartsWith("path:"))
        {
            string assetPath = reference.Substring(5);
            return AssetDatabase.LoadAssetAtPath(assetPath, objectType);
        }

        // 处理 "category/key" 格式
        string[] parts = reference.Split('/');
        if (parts.Length == 2)
        {
            string category = parts[0];
            string key = parts[1];
            return AssetManager.Instance.GetAsset<Object>(category, key);
        }

        return null;
    }
#endif
}
