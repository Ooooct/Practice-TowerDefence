using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MouseOverTowerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_showText;
    [SerializeField] private Vector2 m_uiOffset;
    [SerializeField] private LineRenderer m_rangeRenderer;
    private TowerMain m_targetTower;
    // 圆形范围渲染器
    private int m_circleSegments = 60;
    void Start()
    {
        EventBus.Instance.Subscribe<MouseMoveEvent>(OnMouseMoveEvent);
    }

    void Update()
    {
        UpdateUI();
    }

    private void OnMouseMoveEvent(MouseMoveEvent evt)
    {
        //射线检测
        Ray ray = Camera.main.ScreenPointToRay(evt.mousePosition);
        //如果命中到Tag为Tower的物体
        if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.collider.CompareTag("Tower") && hit.collider.gameObject.name != "Default")
        {
            //显示UI
            gameObject.SetActive(true);
            //设置UI位置
            Canvas canvas = GetComponentInParent<Canvas>();

            Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            Vector3 worldPos = hit.collider.transform.position + Vector3.up * 2;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            RectTransform rectTransform = transform as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPos, cam, out localPoint);
            rectTransform.anchoredPosition = localPoint + m_uiOffset;
            m_targetTower = hit.collider.GetComponent<TowerMain>();
            UpdateUI();
        }
        else
        {
            //隐藏UI
            gameObject.SetActive(false);
            m_targetTower = null;

            if (m_rangeRenderer != null)
                m_rangeRenderer.enabled = false;
        }
    }

    private void UpdateUI()
    {
        if (m_targetTower != null)
        {
            if (m_showText != null)
            {
                var builder = m_targetTower.GetObjectComponent<TowerBuilder>();
                var stats = m_targetTower.GetObjectComponent<DamageStatistics>();
                string towerName = builder.TowerBuildData.name;
                int level = builder.CurrentLevel;
                string dps = stats != null ? stats.DamagePerSecond.ToString("F2") : "-";
                string predictDps = stats != null ? stats.PredictDPS.ToString("F2") : "-";
                m_showText.text = $"{towerName}\n等级: {++level}\nDPS: {dps}\n预计最大DPS: {predictDps}";
            }
        }
        // 以 m_targetTower 为中心绘制圆形线段表示攻击范围
        if (m_rangeRenderer == null || m_rangeRenderer.enabled == false)
            DrawRangeCircle();
    }


    private void DrawRangeCircle()
    {
        if (m_targetTower == null) return;
        float range = m_targetTower.GetObjectComponent<TowerBuilder>().TowerBuildData.LevelData[m_targetTower.GetObjectComponent<TowerBuilder>().CurrentLevel].attackRange;
        Vector3 center = m_targetTower.transform.position;

        m_rangeRenderer.positionCount = m_circleSegments + 1;
        // 计算圆形顶点
        for (int i = 0; i <= m_circleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / m_circleSegments;
            float x = Mathf.Cos(angle) * range;
            float z = Mathf.Sin(angle) * range;
            Vector3 pos = center + new Vector3(x, 0.25f, z); // 稍微抬高避免地面遮挡
            m_rangeRenderer.SetPosition(i, pos);
        }
        m_rangeRenderer.enabled = true;
    }

    private void OnDisable()
    {
        if (m_rangeRenderer != null)
        {
            m_rangeRenderer.enabled = false;
        }
    }
}

