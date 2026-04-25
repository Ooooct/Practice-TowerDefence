using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeSpeedControl : MonoBehaviour
{
    [SerializeField] Slider m_slider;
    [SerializeField] Button m_button;
    float m_defaultTimeScale = 1f;
    void Start()
    {
        m_slider ??= GetComponent<Slider>();
        m_button ??= GetComponent<Button>();
        m_slider.value = m_defaultTimeScale * 2;
        Time.timeScale = m_defaultTimeScale;

        m_slider.onValueChanged.AddListener(OnSliderValueChanged);
        m_button.onClick.AddListener(OnButtonPressed);
    }

    public void OnSliderValueChanged(float value)
    {
        Time.timeScale = value / 2;
    }

    public void OnButtonPressed()
    {
        Time.timeScale = m_defaultTimeScale / 2;
        m_slider.value = m_defaultTimeScale * 2;
    }
}
