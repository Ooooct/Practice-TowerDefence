using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP UI 项，显示单个敌人的生命值和护甲
/// </summary>
public class HPUIItem : MonoBehaviour, IPoolable
{
    #region 序列化字段
    [Header("UI 组件")]
    [SerializeField] private Slider m_hpBar;
    [SerializeField] private Slider m_armorBar;
    #endregion

    #region 变量
    private RectTransform m_rectTransform;
    private HealthHandler m_target;
    private Transform m_targetTransform;
    #endregion

    #region 属性
    public GameObject GameObject => gameObject;
    #endregion

    #region Unity 生命周期
    private void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();

        m_rectTransform.pivot = new Vector2(0.5f, 0.6f);
    }
    #endregion

    #region IPoolable 接口
    public void OnSpawn()
    {
        // 对象从池中取出时调用
    }

    public void OnRecycle()
    {
        // 取消订阅事件
        if (m_target != null)
        {
            m_target.OnHealthChanged -= OnHealthChanged;
            m_target.OnShieldChanged -= OnShieldChanged;
        }

        m_target = null;
        m_targetTransform = null;

        // 重置护甲条显示
        if (m_armorBar != null)
        {
            m_armorBar.gameObject.SetActive(true);
        }
    }
    #endregion

    #region Public 方法
    /// <summary>
    /// 绑定目标敌人
    /// </summary>
    public void BindTarget(EnemyMain enemy)
    {
        if (enemy == null)
        {
            Debug.LogWarning("[HPUIItem] - 尝试绑定空的敌人对象");
            return;
        }

        m_targetTransform = enemy.transform;
        m_target = enemy.GetObjectComponent<HealthHandler>();

        if (m_target == null)
        {
            Debug.LogError("[HPUIItem] - 敌人没有 HealthHandler 组件");
            return;
        }

        // 订阅生命值和护甲变化事件
        m_target.OnHealthChanged += OnHealthChanged;
        m_target.OnShieldChanged += OnShieldChanged;

        OnHealthChanged(m_target.CurrentHealth, m_target.MaxHealth);
        OnShieldChanged(m_target.Shield, m_target.MaxHealth);
    }

    /// <summary>
    /// 更新 UI 位置（由 HPUIManager 每帧调用）
    /// </summary>
    public void UpdatePosition(Camera camera, Canvas canvas)
    {
        if (m_targetTransform == null || m_rectTransform == null || canvas == null)
        {
            return;
        }

        // 将世界坐标转换为屏幕坐标
        Vector3 screenPos = camera.WorldToScreenPoint(m_targetTransform.position);

        // 检查目标是否在摄像机前方
        if (screenPos.z > 0)
        {
            // 获取 Canvas 的 RectTransform
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // 将屏幕坐标转换为 Canvas 内的局部坐标
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera,
                out localPoint))
            {
                // 设置 RectTransform 的局部坐标
                m_rectTransform.localPosition = localPoint;
            }
        }
        else
        {
            // 如果目标在摄像机后方，隐藏 UI
            m_rectTransform.localPosition = new Vector3(0, 0, -10000);
        }
    }
    #endregion

    #region Private 方法
    /// <summary>
    /// 生命值变化回调
    /// </summary>
    private void OnHealthChanged(float current, float max)
    {
        if (m_hpBar != null)
        {
            m_hpBar.value = max > 0 ? current / max : 0;
        }
    }

    /// <summary>
    /// 护甲变化回调
    /// </summary>
    private void OnShieldChanged(float current, float max)
    {
        if (m_armorBar != null)
        {
            // 更新护甲条的值
            m_armorBar.value = max > 0 ? current / max : 0;

            // 如果护甲值为 0，隐藏护甲条
            m_armorBar.gameObject.SetActive(current > 0.001);
        }
    }
    #endregion
}
