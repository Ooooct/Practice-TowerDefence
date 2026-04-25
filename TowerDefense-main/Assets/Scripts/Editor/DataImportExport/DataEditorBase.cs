using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 数据导入导出的基类编辑器
/// 提供 JSON 导入导出、热重载和 EventBus 集成功能
/// </summary>
/// <typeparam name="T">ScriptableObject 类型</typeparam>
public abstract class DataEditorBase<T> : Editor where T : ScriptableObject
{
    protected T TargetData => target as T;

    private SerializedProperty m_jsonFileProperty;
    private FileSystemWatcher m_fileWatcher;
    private bool m_needsReload;
    private float m_lastReloadTime;
    private const float RELOAD_COOLDOWN = 0.5f;

    protected virtual void OnEnable()
    {
        m_jsonFileProperty = serializedObject.FindProperty("m_jsonFile");
        SetupFileWatcher();
        EditorApplication.update += OnEditorUpdate;
    }

    protected virtual void OnDisable()
    {
        CleanupFileWatcher();
        EditorApplication.update -= OnEditorUpdate;
    }

    private void SetupFileWatcher()
    {
        CleanupFileWatcher();

        if (m_jsonFileProperty?.objectReferenceValue is TextAsset jsonFile)
        {
            string jsonPath = AssetDatabase.GetAssetPath(jsonFile);
            if (!string.IsNullOrEmpty(jsonPath))
            {
                string fullPath = Path.GetFullPath(jsonPath);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        string directory = Path.GetDirectoryName(fullPath);
                        string fileName = Path.GetFileName(fullPath);

                        m_fileWatcher = new FileSystemWatcher(directory, fileName)
                        {
                            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                            EnableRaisingEvents = true
                        };

                        m_fileWatcher.Changed += (sender, e) => m_needsReload = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[{GetType().Name}] 无法设置文件监听: {e.Message}");
                    }
                }
            }
        }
    }

    private void CleanupFileWatcher()
    {
        if (m_fileWatcher != null)
        {
            m_fileWatcher.Dispose();
            m_fileWatcher = null;
        }
    }

    private void OnEditorUpdate()
    {
        if (m_needsReload && (EditorApplication.timeSinceStartup - m_lastReloadTime) > RELOAD_COOLDOWN)
        {
            m_needsReload = false;
            m_lastReloadTime = (float)EditorApplication.timeSinceStartup;
            ReloadFromBoundJson();
        }
    }

    private void ReloadFromBoundJson()
    {
        if (m_jsonFileProperty?.objectReferenceValue is TextAsset jsonFile)
        {
            try
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(jsonFile), ImportAssetOptions.ForceUpdate);
                string json = jsonFile.text;

                if (!string.IsNullOrEmpty(json) && IsValidJson(json))
                {
                    Undo.RecordObject(TargetData, "Reload from JSON");

                    var settings = new JsonSerializerSettings
                    {
                        Converters = new JsonConverter[] { new AssetReferenceJsonConverter() }
                    };
                    var data = JsonConvert.DeserializeObject(json, GetSerializableDataType(), settings);
                    ApplySerializableData(TargetData, data);

                    EditorUtility.SetDirty(TargetData);
                    AssetDatabase.SaveAssets();

                    Debug.Log($"[{GetType().Name}] 成功从 JSON 重载数据: {TargetData.name}");
                    PublishReloadEvent();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetType().Name}] 重载失败: {e.Message}");
            }
        }
    }

    private bool IsValidJson(string json)
    {
        try
        {
            JsonConvert.DeserializeObject(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void PublishReloadEvent()
    {
        if (!Application.isPlaying) return;

        var assetManager = AssetManager.Instance;
        foreach (CategoriesEnum category in Enum.GetValues(typeof(CategoriesEnum)))
        {
            string assetKey = assetManager.GetAssetKey(TargetData, category);
            if (!string.IsNullOrEmpty(assetKey))
            {
                var eventBus = EventBus.Instance;
                if (eventBus != null)
                {
                    var reloadEvent = new DataReloadedEvent(category.ToString(), assetKey, TargetData);
                    eventBus.Publish(reloadEvent);
                    Debug.Log($"[{GetType().Name}] 发布重载事件: {reloadEvent}");
                }
                break;
            }
        }
    }

    /// <summary>
    /// 获取 JSON 序列化设置
    /// </summary>
    protected virtual JsonSerializerSettings GetJsonSettings()
    {
        return new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Include
        };
    }

    /// <summary>
    /// 创建可序列化的数据结构
    /// 子类必须实现此方法
    /// </summary>
    protected abstract object CreateSerializableData(T data);

    /// <summary>
    /// 从可序列化数据应用到目标对象
    /// 子类必须实现此方法
    /// </summary>
    protected abstract void ApplySerializableData(T target, object deserializedData);

    /// <summary>
    /// 获取可序列化数据的类型
    /// </summary>
    protected abstract Type GetSerializableDataType();

    /// <summary>
    /// 导出到 JSON
    /// </summary>
    protected void ExportToJson()
    {
        string defaultFileName = $"{TargetData.name}.json";
        string path = EditorUtility.SaveFilePanel(
            "导出为 JSON",
            Application.dataPath,
            defaultFileName,
            "json"
        );

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            object serializableData = CreateSerializableData(TargetData);
            string json = JsonConvert.SerializeObject(serializableData, GetJsonSettings());
            File.WriteAllText(path, json);

            EditorUtility.DisplayDialog("导出成功", $"数据已导出到:\n{path}", "确定");
            Debug.Log($"[{GetType().Name}] 成功导出数据到: {path}");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("导出失败", $"导出数据时出错:\n{e.Message}", "确定");
            Debug.LogError($"[{GetType().Name}] 导出失败: {e}");
        }
    }

    /// <summary>
    /// 从 JSON 导入
    /// </summary>
    protected void ImportFromJson()
    {
        string path = EditorUtility.OpenFilePanel(
            "从 JSON 导入",
            Application.dataPath,
            "json"
        );

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = File.ReadAllText(path);
            var settings = new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new AssetReferenceJsonConverter() }
            };
            var deserializedData = JsonConvert.DeserializeObject(json, GetSerializableDataType(), settings);

            Undo.RecordObject(TargetData, "Import from JSON");
            ApplySerializableData(TargetData, deserializedData);

            EditorUtility.SetDirty(TargetData);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("导入成功", $"数据已从以下位置导入:\n{path}", "确定");
            Debug.Log($"[{GetType().Name}] 成功导入数据: {path}");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("导入失败", $"导入数据时出错:\n{e.Message}", "确定");
            Debug.LogError($"[{GetType().Name}] 导入失败: {e}");
        }
    }

    private void CreateJsonToAddressables()
    {
        string addressablesPath = "Assets/Addressables";
        if (!AssetDatabase.IsValidFolder(addressablesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Addressables");
        }

        string jsonDataPath = $"{addressablesPath}/JsonData";
        if (!AssetDatabase.IsValidFolder(jsonDataPath))
        {
            AssetDatabase.CreateFolder(addressablesPath, "JsonData");
        }

        string fileName = $"{TargetData.name}.json";
        string fullPath = $"{jsonDataPath}/{fileName}";

        // 导出 JSON
        object serializableData = CreateSerializableData(TargetData);
        string json = JsonConvert.SerializeObject(serializableData, GetJsonSettings());
        File.WriteAllText(fullPath, json);
        AssetDatabase.Refresh();

        // 绑定 JSON 文件
        TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
        m_jsonFileProperty.objectReferenceValue = jsonAsset;
        serializedObject.ApplyModifiedProperties();
        SetupFileWatcher();

        Debug.Log($"[{GetType().Name}] 已创建并绑定 JSON: {fullPath}");
        EditorUtility.DisplayDialog("成功", $"已创建 JSON 文件到:\n{fullPath}", "确定");
    }

    private void UpdateBoundJson()
    {
        if (m_jsonFileProperty?.objectReferenceValue is TextAsset jsonFile)
        {
            string jsonPath = AssetDatabase.GetAssetPath(jsonFile);
            object serializableData = CreateSerializableData(TargetData);
            string json = JsonConvert.SerializeObject(serializableData, GetJsonSettings());
            File.WriteAllText(jsonPath, json);
            AssetDatabase.Refresh();
            Debug.Log($"[{GetType().Name}] 已更新 JSON: {jsonPath}");
            EditorUtility.DisplayDialog("成功", $"已更新 JSON 文件", "确定");
        }
    }

    /// <summary>
    /// 绘制导入导出按钮
    /// </summary>
    protected void DrawImportExportButtons()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("数据导入/导出", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("导出到 JSON", GUILayout.Height(30)))
        {
            ExportToJson();
        }

        if (GUILayout.Button("从 JSON 导入", GUILayout.Height(30)))
        {
            ImportFromJson();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "导出: 将当前数据保存为 JSON 文件\n" +
            "导入: 从 JSON 文件加载数据并覆盖当前设置",
            MessageType.Info
        );
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // JSON 绑定区域
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("JSON 配置", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_jsonFileProperty, new GUIContent("绑定的 JSON 文件"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            SetupFileWatcher();
        }

        // 自动创建 JSON 按钮
        if (m_jsonFileProperty.objectReferenceValue == null)
        {
            if (GUILayout.Button("自动创建 JSON 到 Addressables", GUILayout.Height(25)))
            {
                CreateJsonToAddressables();
            }
            EditorGUILayout.HelpBox("未绑定 JSON 文件。点击按钮自动创建并绑定。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新加载 JSON", GUILayout.Height(25)))
            {
                ReloadFromBoundJson();
            }
            if (GUILayout.Button("更新 JSON", GUILayout.Height(25)))
            {
                UpdateBoundJson();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);

        // 绘制默认 Inspector
        DrawDefaultInspector();

        // 导入导出按钮
        DrawImportExportButtons();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 获取资产引用字符串
    /// </summary>
    protected string GetAssetReference(UnityEngine.Object asset)
    {
        if (asset == null) return null;

        foreach (CategoriesEnum category in Enum.GetValues(typeof(CategoriesEnum)))
        {
            string key = AssetManager.Instance.GetAssetKey(asset, category);
            if (!string.IsNullOrEmpty(key))
            {
                return $"{category}/{key}";
            }
        }

        string assetPath = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(assetPath))
        {
            return $"path:{assetPath}";
        }

        return null;
    }

    /// <summary>
    /// 加载资产引用
    /// </summary>
    protected TAsset LoadAssetReference<TAsset>(string reference) where TAsset : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(reference)) return null;

        if (reference.StartsWith("path:"))
        {
            string assetPath = reference.Substring(5);
            return AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
        }

        string[] parts = reference.Split('/');
        if (parts.Length != 2) return null;

        return AssetManager.Instance.GetAsset<TAsset>(parts[0], parts[1]);
    }
}

