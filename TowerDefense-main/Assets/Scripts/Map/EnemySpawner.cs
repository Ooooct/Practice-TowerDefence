using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人生成器，负责从对象池中生成敌人并应用数据
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private Transform m_spawnPoint; // 生成点位置

    /// <summary>
    /// 生成敌人
    /// </summary>
    /// <param name="enemyData">敌人数据</param>
    /// <returns>生成的敌人实例，如果失败则返回 null</returns>
    public EnemyMain SpawnEnemy(EnemyData enemyData)
    {
        if (enemyData == null)
        {
            Debug.LogWarning("[EnemySpawner] - 敌人数据为空，无法生成敌人");
            return null;
        }

        // 从对象池获取敌人
        Vector3 spawnPosition = m_spawnPoint != null ? m_spawnPoint.position : transform.position;
        EnemyMain enemy = PoolManager.Instance.Spawn<EnemyMain>(spawnPosition, Quaternion.identity);

        if (enemy == null)
        {
            Debug.LogError("[EnemySpawner] - 从对象池获取敌人失败");
            return null;
        }

        // 获取 EnemySaveLoad 组件并应用数据
        EnemyDataApply enemySL = enemy.GetObjectComponent<EnemyDataApply>();
        if (enemySL != null)
        {
            enemySL.ApplyData(enemyData);
        }
        else
        {
            Debug.LogError("[EnemySpawner] - 无法获取 EnemySaveLoad 组件");
        }

        return enemy;
    }
}
