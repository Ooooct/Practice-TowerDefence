using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

/// <summary>
/// WayPointsData 的可序列化数据结构
/// </summary>
[System.Serializable]
public class SerializableWayPointsData
{
    public List<SerializableVector3> wayPoints;
}

/// <summary>
/// 可序列化的 Vector3
/// </summary>
[System.Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

/// <summary>
/// WayPointsData 的编辑器，继承自 WayPointsEditor 以保留路径点编辑功能，并添加 JSON 导入导出支持
/// </summary>
[CustomEditor(typeof(WayPointsData))]
public class WayPointsDataEditor : WayPointsEditor
{
    private SerializedProperty m_jsonFileProperty;

    protected override void OnEnable()
    {
        base.OnEnable();
        m_jsonFileProperty = serializedObject.FindProperty("m_jsonFile");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // JSON 配置区域
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("JSON 配置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_jsonFileProperty, new GUIContent("绑定的 JSON 文件"));

        // JSON 导入导出按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("导出到 JSON", GUILayout.Height(25)))
        {
            ExportToJson();
        }
        if (GUILayout.Button("从 JSON 导入", GUILayout.Height(25)))
        {
            ImportFromJson();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 调用基类的 Inspector GUI（路径点编辑功能）
        base.OnInspectorGUI();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 创建可序列化的数据结构
    /// </summary>
    private object CreateSerializableData(WayPointsData data)
    {
        var serializableData = new SerializableWayPointsData
        {
            wayPoints = new List<SerializableVector3>()
        };

        foreach (var wayPoint in data.wayPoints)
        {
            serializableData.wayPoints.Add(new SerializableVector3(wayPoint));
        }

        return serializableData;
    }

    /// <summary>
    /// 从可序列化数据应用到目标对象
    /// </summary>
    private void ApplySerializableData(WayPointsData target, object deserializedData)
    {
        if (deserializedData is SerializableWayPointsData serializableData)
        {
            target.wayPoints.Clear();
            foreach (var serializableWayPoint in serializableData.wayPoints)
            {
                target.wayPoints.Add(serializableWayPoint.ToVector3());
            }
        }
    }

    /// <summary>
    /// 导出到 JSON
    /// </summary>
    private void ExportToJson()
    {
        WayPointsData data = target as WayPointsData;
        if (data == null) return;

        string defaultFileName = $"{data.name}.json";
        string path = EditorUtility.SaveFilePanel(
            "导出为 JSON",
            Application.dataPath,
            defaultFileName,
            "json"
        );

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            object serializableData = CreateSerializableData(data);
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Include
            };
            string json = JsonConvert.SerializeObject(serializableData, settings);
            File.WriteAllText(path, json);

            EditorUtility.DisplayDialog("导出成功", $"数据已导出到:\n{path}", "确定");
            Debug.Log($"[WayPointsDataEditor] 成功导出数据到: {path}");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("导出失败", $"导出数据时出错:\n{e.Message}", "确定");
            Debug.LogError($"[WayPointsDataEditor] 导出失败: {e}");
        }
    }

    /// <summary>
    /// 从 JSON 导入
    /// </summary>
    private void ImportFromJson()
    {
        WayPointsData data = target as WayPointsData;
        if (data == null) return;

        string path = EditorUtility.OpenFilePanel(
            "从 JSON 导入",
            Application.dataPath,
            "json"
        );

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = File.ReadAllText(path);
            var deserializedData = JsonConvert.DeserializeObject<SerializableWayPointsData>(json);

            Undo.RecordObject(data, "Import from JSON");
            ApplySerializableData(data, deserializedData);

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("导入成功", $"数据已从以下位置导入:\n{path}", "确定");
            Debug.Log($"[WayPointsDataEditor] 成功导入数据: {path}");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("导入失败", $"导入数据时出错:\n{e.Message}", "确定");
            Debug.LogError($"[WayPointsDataEditor] 导入失败: {e}");
        }
    }
}
