using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDataApply : EnemyComponentBase
{
    private EnemyData m_enemyData;
    public void ApplyData(EnemyData data)
    {

        m_enemyData = data;

        foreach (var immuneBuff in data.immuneBuffs)
        {
            m_enemy.BuffManager.AddImmuneBuff(immuneBuff);
        }
        foreach (var buff in data.buffs)
        {
            m_enemy.BuffManager.AddBuff(buff);
        }


        m_enemy.GetObjectComponent<HealthHandler>().MaxHealth = data.maxHealth;
        m_enemy.GetObjectComponent<HealthHandler>().CurrentHealth = data.maxHealth;
        m_enemy.GetObjectComponent<HealthHandler>().Shield = data.armor;
        m_enemy.GetObjectComponent<EnemyMovement>().MoveSpeed = data.speed;
        m_enemy.GetObjectComponent<AttackReceiver>().damageReduceTypes = data.damageReduce;
        m_enemy.GoldReward = data.goldReward;
        m_enemy.enemyData = data;

        //移除子物件下的所有模型

        if (data.viewPrefab != null)
        {
            foreach (Transform child in m_enemy.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            //实例化新的模型
            {
                GameObject.Instantiate(data.viewPrefab, m_enemy.transform);
            }
        }
    }
}
