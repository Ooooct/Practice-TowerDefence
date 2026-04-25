using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class WinAndLoseUI : MonoBehaviour
{
    public static WinAndLoseUI Instance { get; private set; }

    [Header("UI组件")]
    [SerializeField] private Button m_restartButton;
    [SerializeField] private Button m_exitButton;
    [SerializeField] private Button m_replayButton;
    [SerializeField] private TMP_Text m_resultText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (m_restartButton != null)
            m_restartButton.onClick.AddListener(Restart);
        if (m_replayButton != null)
            m_replayButton.onClick.AddListener(Replay);
        if (m_exitButton != null)
            m_exitButton.onClick.AddListener(Exit);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 外部调用：胜利
    /// </summary>
    public void Win()
    {
        ShowResult(true);
    }

    /// <summary>
    /// 外部调用：失败
    /// </summary>
    public void Lose()
    {
        ShowResult(false);
    }

    private void ShowResult(bool isWin)
    {
        Time.timeScale = 0;
        gameObject.SetActive(true);
        if (m_resultText != null)
        {
            m_resultText.text = isWin ? "你赢了！" : "你输了！";
        }
    }

    private void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Replay()
    {
        ReplayManager.MarkReplayLatestOnNextScene();
        ReplayManager.Instance.StopRecording();
        Restart();
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
}
