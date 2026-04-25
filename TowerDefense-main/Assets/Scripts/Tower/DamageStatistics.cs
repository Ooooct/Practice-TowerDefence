using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageStatistics : TowerComponentBase
{
    public float TotalDamage { get; private set; } = 0f;
    public float TotalFire { get; private set; } = 0f;
    public float TotalTime { get; private set; } = 0;
    public float DamagePerSecond
    {
        get
        {
            if (TotalTime == 0) return 0f;
            return TotalDamage / TotalTime;
        }
    }

    public float PredictDPS
    {
        get
        {
            float attackCoolDownTime = m_tower.GetObjectComponent<BulletShooter>().ShootCoolDown;
            if (attackCoolDownTime == 0) return 0f;
            float damagePreFire = TotalDamage / TotalFire;
            return damagePreFire / attackCoolDownTime;
        }
    }
    public override void Start()
    {
        base.Start();
        EventBus.Instance.Subscribe<DamageReceivedEvent>(ReceiveDamage);
        EventBus.Instance.Subscribe<SendBulletDataEvent>(BulletFired);
    }

    public void UpdateStatistics(float deltaTime)
    {
        TotalTime += deltaTime;
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        EventBus.Instance.Unsubscribe<DamageReceivedEvent>(ReceiveDamage);
        EventBus.Instance.Subscribe<SendBulletDataEvent>(BulletFired);
    }

    private void ReceiveDamage(DamageReceivedEvent evt)
    {
        if (evt.sourceTower != m_tower) return;
        TotalDamage += evt.damage;
    }

    private void BulletFired(SendBulletDataEvent evt)
    {
        if (evt.attackData.sourceTower != m_tower) return;
        TotalFire++;
    }
}
