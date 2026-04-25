using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CostUI : MonoBehaviour
{
    TextMeshProUGUI m_showText;
    private void Awake()
    {
        m_showText ??= GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        CostManager.Instance.OnPlayerGoldChanged += UpdateGoldDisplay;
        UpdateGoldDisplay(CostManager.Instance.PlayerGold, 0);
    }

    private void OnDestroy()
    {
        CostManager.Instance.OnPlayerGoldChanged -= UpdateGoldDisplay;
    }

    private void UpdateGoldDisplay(int newGold, int oldGold)
    {
        if (m_showText != null)
        {
            string text = newGold.ToString();
            m_showText.text = "金币：" + text;
        }
    }
}
