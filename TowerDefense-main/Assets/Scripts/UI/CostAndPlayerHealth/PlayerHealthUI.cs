using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    TextMeshProUGUI m_showText;
    private void Awake()
    {
        m_showText ??= GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        ProtectPoint.Instance.OnHealthChanged += UpdateHealth;
        UpdateHealth(ProtectPoint.Instance.health);
    }

    private void OnDestroy()
    {
        ProtectPoint.Instance.OnHealthChanged -= UpdateHealth;
    }

    private void UpdateHealth(int health)
    {
        if (m_showText != null)
        {
            string text = health.ToString();
            m_showText.text = "生命值：" + text;
        }
    }
}
