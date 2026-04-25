using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 胜利检查器
/// 当所有波次生成完毕且场上无敌人时判定胜利
/// </summary>
public class WinCheck : MonoBehaviour
{
    private bool m_hasWon = false;

    void Update()
    {
        // 如果已经胜利，不再检查
        if (m_hasWon)
            return;

        // 检查是否所有波次生成完毕
        if (SpawnManager.Instance == null || !SpawnManager.Instance.AllWavesCompleted)
            return;

        // 检查场上敌人数量
        if (UnitManager.Instance == null)
            return;

        int enemyCount = UnitManager.Instance.EnemyCount;

        // 如果场上没有敌人，判定胜利
        if (enemyCount == 0)
        {
            m_hasWon = true;
            Debug.Log("[WinCheck] - 胜利！所有敌人已清除");

            if (WinAndLoseUI.Instance != null)
            {
                WinAndLoseUI.Instance.Win();
            }
            else
            {
                Debug.LogError("[WinCheck] - WinAndLoseUI.Instance 为空，无法显示胜利界面");
            }
        }
    }
}
