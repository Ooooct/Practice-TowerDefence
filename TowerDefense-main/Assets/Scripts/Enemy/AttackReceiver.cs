using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(HealthHandler))]
public class AttackReceiver : EnemyComponentBase
{
    const float CRIT_MULTIPLIER = 2f;
    public List<damageReduce> damageReduceTypes = new List<damageReduce>();
    public override void Start()
    {
        base.Start();
        EventBus.Instance.Subscribe<HitEnemyEvent>(Attacked);
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        EventBus.Instance.Unsubscribe<HitEnemyEvent>(Attacked);
    }

    private void Attacked(HitEnemyEvent evt)
    {
        if (evt.enemyObject != m_enemy.gameObject) return;

        //处理传递的buff
        foreach (string buffStr in evt.attackData.applyBuffs)
        {
            m_enemy.BuffManager.AddBuff(buffStr, evt.attackData.sourceTower);
        }

        float damage = evt.attackData.damage;

        //检查伤害减免类型
        foreach (var damageReduction in damageReduceTypes)
        {
            if (damageReduction.type == evt.attackData.damageType)
            {
                damage *= (1 - damageReduction.reduceValue);
            }
        }

        if (CalculateCrit(evt.attackData.critRate))
        {
            damage *= CRIT_MULTIPLIER;
        }

        m_enemy.HealthHandler.TakeDamage(damage);

        //触发收到伤害事件
        DamageReceivedEvent damageReceivedEvent = new DamageReceivedEvent
        {
            enemy = m_enemy,
            sourceTower = evt.attackData.sourceTower,
            damage = damage
        };
        EventBus.Instance.Publish(damageReceivedEvent);
    }

    private bool CalculateCrit(float rate)
    {
        float roll = RandomController.Instance.Range(0f, 1f);
        return roll <= rate;
    }
}

public struct DamageReceivedEvent : IEvent
{
    public EnemyMain enemy;
    public TowerMain sourceTower;
    public float damage;
}