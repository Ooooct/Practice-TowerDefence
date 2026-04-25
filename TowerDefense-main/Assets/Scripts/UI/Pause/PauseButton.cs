using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class PauseButton : MonoBehaviour
{
    [SerializeField] private Button pauseBtn;
    [SerializeField] private SimpleMenu simpleMenu;

    private void Awake()
    {
        if (pauseBtn != null)
            pauseBtn.onClick.AddListener(ToggleMenu);
        if (simpleMenu != null)
            simpleMenu.gameObject.SetActive(false);
    }

    private void ToggleMenu()
    {
        if (simpleMenu != null)
        {
            bool active = !simpleMenu.gameObject.activeSelf;
            simpleMenu.gameObject.SetActive(active);
            Time.timeScale = active ? 0 : 1;
        }
    }
}
