using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// WayPointsData 的自定义 Inspector 编辑器
/// 允许在 Scene 视图中交互式标注路径点
/// </summary>
[CustomEditor(typeof(WayPointsData))]
public class WayPointsEditor : Editor
{
    private WayPointsData m_data;
    private bool m_isEditing = false;
    private List<Vector3> m_tempPoints = new List<Vector3>();

    #region Unity 生命周期

    protected virtual void OnEnable()
    {
        m_data = (WayPointsData)target;
        SceneView.duringSceneGui += HandleSceneGUI;
    }

    protected virtual void OnDisable()
    {
        SceneView.duringSceneGui -= HandleSceneGUI;

        // 如果正在编辑时禁用，应用当前的
        if (m_isEditing)
        {
            ApplyWayPoints();
        }
    }

    #endregion

    #region Inspector GUI

    public override void OnInspectorGUI()
    {
        if (m_data == null)
        {
            EditorGUILayout.HelpBox("数据对象为空", MessageType.Error);
            return;
        }

        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("路径点编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "点击「开始编辑路径点」进入编辑模式\n" +
            "在 Scene 视图中点击地面添加路径点\n" +
            "按 ESC 或右键终止编辑并自动应用",
            MessageType.Info);

        EditorGUILayout.Space(5);

        // 编辑按钮
        if (!m_isEditing)
        {
            if (GUILayout.Button("开始编辑路径点", GUILayout.Height(30)))
            {
                StartEditing();
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"正在编辑中... 已添加 {m_tempPoints.Count} 个路径点\n" +
                "点击 Scene 视图添加路径点\n" +
                "按 ESC 或右键完成编辑",
                MessageType.Warning);

            if (GUILayout.Button("完成编辑并应用", GUILayout.Height(30)))
            {
                ApplyWayPoints();
            }

            if (GUILayout.Button("取消编辑", GUILayout.Height(25)))
            {
                CancelEditing();
            }
        }

        EditorGUILayout.Space(10);

        // 显示当前路径点
        EditorGUILayout.LabelField($"当前路径点数量: {m_data.wayPoints.Count}", EditorStyles.boldLabel);

        if (m_data.wayPoints.Count > 0)
        {
            EditorGUILayout.BeginVertical("box");
            for (int i = 0; i < m_data.wayPoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"点 {i + 1}:", GUILayout.Width(50));
                EditorGUILayout.LabelField(m_data.wayPoints[i].ToString(), EditorStyles.miniLabel);

                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    Undo.RecordObject(m_data, "Remove WayPoint");
                    m_data.wayPoints.RemoveAt(i);
                    EditorUtility.SetDirty(m_data);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("清除所有路径点"))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清除所有路径点吗？", "确定", "取消"))
                {
                    Undo.RecordObject(m_data, "Clear WayPoints");
                    m_data.wayPoints.Clear();
                    EditorUtility.SetDirty(m_data);
                }
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"终点坐标: {WayPointsData.END_POINT}", EditorStyles.miniLabel);

        serializedObject.ApplyModifiedProperties();
    }

    #endregion

    #region Scene GUI

    private void HandleSceneGUI(SceneView sceneView)
    {
        if (!m_isEditing || m_data == null)
        {
            return;
        }

        // 显示编辑提示
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.BeginVertical("box");
        GUILayout.Label("路径点编辑模式", EditorStyles.boldLabel);
        GUILayout.Label($"已添加: {m_tempPoints.Count} 个路径点");
        GUILayout.Label("左键点击添加路径点");
        GUILayout.Label("ESC 或右键完成编辑");
        GUILayout.EndVertical();
        GUILayout.EndArea();
        Handles.EndGUI();

        // 监听 ESC 键
        Event evt = Event.current;
        if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
        {
            ApplyWayPoints();
            evt.Use();
            return;
        }

        // 监听右键
        if (evt.type == EventType.MouseDown && evt.button == 1)
        {
            ApplyWayPoints();
            evt.Use();
            return;
        }

        // 绘制已有的临时路径点
        DrawTempWayPoints();

        // 处理鼠标点击添加路径点
        if (evt.type == EventType.MouseDown && evt.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 point = hit.point;
                m_tempPoints.Add(point);
                evt.Use();
                sceneView.Repaint();
            }
        }

        // 阻止选择其他对象
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    private void DrawTempWayPoints()
    {
        if (m_tempPoints.Count == 0)
        {
            return;
        }

        // 绘制路径点
        Handles.color = Color.green;
        for (int i = 0; i < m_tempPoints.Count; i++)
        {
            Vector3 point = m_tempPoints[i];

            // 绘制球体
            Handles.SphereHandleCap(0, point, Quaternion.identity, 0.5f, EventType.Repaint);

            // 绘制标签
            Handles.Label(point + Vector3.up * 0.5f, $"点 {i + 1}", new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            });

            // 绘制连线
            if (i > 0)
            {
                Handles.DrawLine(m_tempPoints[i - 1], point);
            }
        }

        // 绘制到终点的连线
        Handles.color = Color.red;
        if (m_tempPoints.Count > 0)
        {
            Handles.DrawLine(m_tempPoints[m_tempPoints.Count - 1], WayPointsData.END_POINT);
            Handles.SphereHandleCap(0, WayPointsData.END_POINT, Quaternion.identity, 0.5f, EventType.Repaint);
            Handles.Label(WayPointsData.END_POINT + Vector3.up * 0.5f, "终点", new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.red },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            });
        }
    }

    #endregion

    #region private方法
    private void StartEditing()
    {
        m_isEditing = true;
        m_tempPoints.Clear();

        // 如果已有路径点，复制到临时列表（不包括终点）
        if (m_data.wayPoints.Count > 0)
        {
            for (int i = 0; i < m_data.wayPoints.Count; i++)
            {
                // 跳过终点
                if (m_data.wayPoints[i] != WayPointsData.END_POINT)
                {
                    m_tempPoints.Add(m_data.wayPoints[i]);
                }
            }
        }
        SceneView.lastActiveSceneView?.Repaint();
    }

    private void ApplyWayPoints()
    {
        if (!m_isEditing)
        {
            return;
        }

        Undo.RecordObject(m_data, "Apply WayPoints");

        // 清空原有路径点
        m_data.wayPoints.Clear();

        // 添加临时路径点
        m_data.wayPoints.AddRange(m_tempPoints);

        // 在末尾追加终点
        m_data.wayPoints.Add(WayPointsData.END_POINT);

        EditorUtility.SetDirty(m_data);

        m_isEditing = false;
        m_tempPoints.Clear();

        SceneView.lastActiveSceneView?.Repaint();
    }

    private void CancelEditing()
    {
        m_isEditing = false;
        m_tempPoints.Clear();

        SceneView.lastActiveSceneView?.Repaint();
    }
    #endregion
}
