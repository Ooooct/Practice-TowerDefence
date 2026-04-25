using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyFinder
{
    EnemyMain FindEnemy();
}
/// <summary>
/// 敌人查找器，负责查找塔攻击范围内的敌人目标
/// </summary>
public class EnemyFinder : TowerComponentBase, IEnemyFinder
{
    private float m_attackRange;
    public float AttackRange => m_attackRange;
    #region Public方法
    /// <summary>
    /// 查找攻击范围内距离目标点最近的敌人
    /// </summary>
    /// <returns>距离目标点最近的敌人，如果没有找到则返回null</returns>
    public EnemyMain FindEnemy()
    {
        List<EnemyMain> targets = UnitManager.Instance.GetEnemiesInRange2D(m_tower.transform.position, m_attackRange);
        if (targets.Count == 0)
        {
            return null;
        }

        // 选取其寻路组件内距离目标点最近的敌人
        EnemyMain closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in targets)
        {
            if (enemy == null || enemy.Movement == null)
            {
                continue;
            }

            // 获取敌人到最终目标点的距离
            float distanceToTarget = enemy.Movement.DistanceToFinalTarget;

            // 找到距离最小的敌人
            if (distanceToTarget < minDistance)
            {
                minDistance = distanceToTarget;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    public void SetAttackRange(float range)
    {
        m_attackRange = range;
    }
    #endregion
}
