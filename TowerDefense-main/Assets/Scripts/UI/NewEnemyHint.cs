using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 新敌人提示UI组件
/// 监听新敌人发现事件并显示提示信息，5秒后自动消失
/// </summary>
public class NewEnemyHint : MonoBehaviour
{
    #region 变量
    private TextMeshProUGUI m_textComponent;
    private Coroutine m_hideCoroutine;
    private const float DISPLAY_DURATION = 5f;
    #endregion

    #region Unity 生命周期
    void Start()
    {
        // 获取 TextMeshProUGUI 组件
        m_textComponent = GetComponent<TextMeshProUGUI>();
        if (m_textComponent == null)
        {
            Debug.LogError("[NewEnemyHint] 无法找到 TextMeshProUGUI 组件");
            return;
        }

        // 初始隐藏
        gameObject.SetActive(false);

        // 订阅新敌人发现事件
        EventBus.Instance.Subscribe<NewEnemyDiscoveredEvent>(OnNewEnemyDiscovered);
    }

    void OnDestroy()
    {
        // 取消订阅
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<NewEnemyDiscoveredEvent>(OnNewEnemyDiscovered);
        }
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 处理新敌人发现事件
    /// </summary>
    private void OnNewEnemyDiscovered(NewEnemyDiscoveredEvent evt)
    {
        // 显示提示信息
        ShowHint(evt.EnemyName);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 显示提示信息
    /// </summary>
    private void ShowHint(string enemyName)
    {
        if (m_textComponent == null)
        {
            Debug.LogError("[NewEnemyHint] TextMeshProUGUI 组件为空");
            return;
        }

        // 设置提示文本
        m_textComponent.text = $"本波将遇见新的敌人：{enemyName}";
        Debug.Log($"[NewEnemyHint] 显示新敌人提示: {enemyName}");

        // 显示组件
        gameObject.SetActive(true);

        // 如果已有隐藏协程在运行，先停止它
        if (m_hideCoroutine != null)
        {
            StopCoroutine(m_hideCoroutine);
        }

        // 启动新的隐藏协程
        m_hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    /// <summary>
    /// 延迟后隐藏提示
    /// </summary>
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(DISPLAY_DURATION);

        // 隐藏组件
        gameObject.SetActive(false);
        Debug.Log("[NewEnemyHint] 提示已隐藏");
    }
    #endregion
}
