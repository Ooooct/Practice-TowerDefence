using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimpleMenu : MonoBehaviour
{
    [SerializeField] private Button m_resumeButton;
    [SerializeField] private Button m_restartButton;
    [SerializeField] private Button m_exitButton;
    [SerializeField] private Button m_replayButton;
    [SerializeField] private TMP_InputField m_replayInputField;
    private void Awake()
    {
        if (m_resumeButton != null)
            m_resumeButton.onClick.AddListener(Resume);
        if (m_restartButton != null)
            m_restartButton.onClick.AddListener(Restart);
        if (m_exitButton != null)
            m_exitButton.onClick.AddListener(Exit);
        if (m_replayButton != null)
            m_replayButton.onClick.AddListener(Replay);
    }

    private void Resume()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    private void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Exit()
    {
        Time.timeScale = 1;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
    }

    private void Replay()
    {
        // 标记下一次场景开始时自动回放最新文件
        if (string.IsNullOrEmpty(m_replayInputField?.text))
        {
            ReplayManager.MarkReplayLatestOnNextScene();
        }
        else
        {
            ReplayManager.MarkReplayFileOnNextScene(m_replayInputField.text);
        }

        // 先保存当前录制（如果在录制中），再重启场景
        ReplayManager.Instance.StopRecording();
        Restart();
        // 注意：不要在这里直接调用 StartReplay()，新的场景中 ReplayManager.Start 会处理
    }
}
