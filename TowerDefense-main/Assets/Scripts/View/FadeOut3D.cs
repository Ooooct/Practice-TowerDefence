using System.Collections;
using UnityEngine;

/// <summary>
/// 3D 物体渐变消失组件
/// 根据配置的时间使物体透明度逐渐降低，最终销毁
/// </summary>
public class FadeOut3D : MonoBehaviour
{
    [Header("渐变配置")]
    [Tooltip("渐变颜色（RGB）")]
    [SerializeField] private Color m_color = Color.white;

    [Tooltip("初始透明度 (0-1)")]
    [SerializeField, Range(0f, 1f)] private float m_initialAlpha = 1f;

    [Tooltip("消失时间（秒）")]
    [SerializeField] private float m_fadeDuration = 1f;

    [Tooltip("是否在 Start 时自动开始渐变")]
    [SerializeField] private bool m_autoStart = true;

    // 运行时变量
    private Renderer m_renderer;
    private Material m_runtimeMaterial; // 运行时材质实例
    private float m_elapsedTime = 0f;
    private bool m_isFading = false;

    void Start()
    {
        // 获取 Renderer 组件
        m_renderer = GetComponent<Renderer>();

        if (m_renderer == null)
        {
            Debug.LogError($"[FadeOut3D] GameObject '{gameObject.name}' 缺少 Renderer 组件！", this);
            Destroy(this);
            return;
        }

        // 创建材质实例（避免修改共享材质）
        m_runtimeMaterial = m_renderer.material;

        // 设置初始颜色和透明度
        Color initialColor = m_color;
        initialColor.a = m_initialAlpha;
        m_runtimeMaterial.color = initialColor;

        // 确保材质支持透明度（设置渲染模式）
        SetupMaterialForTransparency();

        if (m_autoStart)
        {
            StartFade();
        }
    }

    void Update()
    {
        if (!m_isFading)
        {
            return;
        }

        // 累积时间
        m_elapsedTime += Time.deltaTime;

        // 计算当前透明度（从初始透明度线性降低到 0）
        float t = m_elapsedTime / m_fadeDuration;
        float currentAlpha = Mathf.Lerp(m_initialAlpha, 0f, t);

        // 更新材质颜色
        Color newColor = m_color;
        newColor.a = currentAlpha;
        m_runtimeMaterial.color = newColor;

        // 检查是否完成渐变
        if (m_elapsedTime >= m_fadeDuration)
        {
            // 销毁游戏对象
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 开始渐变消失
    /// </summary>
    public void StartFade()
    {
        m_isFading = true;
        m_elapsedTime = 0f;
    }

    /// <summary>
    /// 暂停渐变
    /// </summary>
    public void PauseFade()
    {
        m_isFading = false;
    }

    /// <summary>
    /// 恢复渐变
    /// </summary>
    public void ResumeFade()
    {
        m_isFading = true;
    }

    /// <summary>
    /// 重置渐变（重新开始）
    /// </summary>
    public void ResetFade()
    {
        m_elapsedTime = 0f;

        if (m_runtimeMaterial != null)
        {
            Color initialColor = m_color;
            initialColor.a = m_initialAlpha;
            m_runtimeMaterial.color = initialColor;
        }

        m_isFading = true;
    }

    /// <summary>
    /// 设置材质以支持透明度渲染
    /// </summary>
    private void SetupMaterialForTransparency()
    {
        if (m_runtimeMaterial == null)
        {
            return;
        }

        // 检查 Shader 是否支持透明度
        // 如果是标准 Shader，设置为 Transparent 模式
        if (m_runtimeMaterial.HasProperty("_Mode"))
        {
            // Standard Shader 的渲染模式：
            // 0 = Opaque, 1 = Cutout, 2 = Fade, 3 = Transparent
            m_runtimeMaterial.SetFloat("_Mode", 3);
            m_runtimeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m_runtimeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m_runtimeMaterial.SetInt("_ZWrite", 0);
            m_runtimeMaterial.DisableKeyword("_ALPHATEST_ON");
            m_runtimeMaterial.EnableKeyword("_ALPHABLEND_ON");
            m_runtimeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m_runtimeMaterial.renderQueue = 3000;
        }
        else
        {
            // 对于其他 Shader，尝试启用透明度相关的设置
            m_runtimeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m_runtimeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m_runtimeMaterial.SetInt("_ZWrite", 0);
            m_runtimeMaterial.renderQueue = 3000;
        }
    }

    /// <summary>
    /// 设置渐变颜色
    /// </summary>
    public void SetColor(Color color)
    {
        m_color = color;

        if (m_runtimeMaterial != null && !m_isFading)
        {
            Color newColor = color;
            newColor.a = m_initialAlpha;
            m_runtimeMaterial.color = newColor;
        }
    }

    /// <summary>
    /// 设置初始透明度
    /// </summary>
    public void SetInitialAlpha(float alpha)
    {
        m_initialAlpha = Mathf.Clamp01(alpha);
    }

    /// <summary>
    /// 设置消失时间
    /// </summary>
    public void SetFadeDuration(float duration)
    {
        m_fadeDuration = Mathf.Max(0.01f, duration);
    }

    void OnDestroy()
    {
        // 清理运行时材质实例
        if (m_runtimeMaterial != null)
        {
            Destroy(m_runtimeMaterial);
        }
    }
}
