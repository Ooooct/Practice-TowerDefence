using UnityEngine;
using System.IO;
using System;

/// <summary>
/// 回放信使
/// 专门用于在场景切换时传递回放标记，自身在场景加载后立即销毁
/// </summary>
public class ReplayMessenger : MonoBehaviour
{
    private static ReplayMessenger s_instance;

    // 跨场景传递的数据
    private bool m_shouldReplay;
    private string m_replayFilePath; // null 表示使用最新文件

    #region 公共接口
    /// <summary>
    /// 标记下一场景开始时回放最新文件
    /// </summary>
    public static void MarkReplayLatestOnNextScene()
    {
        CreateMessenger(true, null);
    }

    /// <summary>
    /// 标记下一场景开始时回放指定文件
    /// </summary>
    public static void MarkReplayFileOnNextScene(string filePath)
    {
        CreateMessenger(true, filePath);
    }

    /// <summary>
    /// 检查并消费回放标记（ReplayManager 在 Start 时调用）
    /// </summary>
    /// <param name="shouldReplay">输出：是否应该回放</param>
    /// <param name="filePath">输出：回放文件路径（null 表示最新）</param>
    /// <returns>是否有有效的标记</returns>
    public static bool ConsumeReplayMark(out bool shouldReplay, out string filePath)
    {
        shouldReplay = false;
        filePath = null;

        if (s_instance == null)
        {
            return false;
        }

        shouldReplay = s_instance.m_shouldReplay;
        filePath = s_instance.m_replayFilePath;

        // 消费后立即销毁信使
        Destroy(s_instance.gameObject);
        s_instance = null;

        return shouldReplay;
    }
    #endregion

    #region 私有方法
    private static void CreateMessenger(bool shouldReplay, string filePath)
    {
        // 如果已有信使，先销毁
        if (s_instance != null)
        {
            Destroy(s_instance.gameObject);
        }

        // 创建新信使对象
        GameObject messengerObj = new GameObject("ReplayMessenger");
        s_instance = messengerObj.AddComponent<ReplayMessenger>();
        s_instance.m_shouldReplay = shouldReplay;
        if (string.IsNullOrEmpty(filePath))
        {
            s_instance.m_replayFilePath = null;
        }
        else
        {
            // 拼接完整路径
            string replayFolderPath = Path.Combine(Application.dataPath, "Addressables", "Replay");
            s_instance.m_replayFilePath = Path.Combine(replayFolderPath, filePath + ".json");
        }

        // 标记为跨场景保持（但会在被消费后立即销毁）
        DontDestroyOnLoad(messengerObj);
    }
    #endregion
}
