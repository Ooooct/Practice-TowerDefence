using UnityEngine;

/// <summary>
/// 命中敌人事件
/// </summary>
public class HitEnemyEvent : IEvent
{
    public AttackData attackData;
    public GameObject enemyObject;
    public BulletMain callerBullet;
    public bool isClearBullet;

    public HitEnemyEvent(AttackData attackData, GameObject enemyObject, BulletMain callerBullet, bool isClearBullet = true)
    {
        this.attackData = attackData;
        this.enemyObject = enemyObject;
        this.callerBullet = callerBullet;
        this.isClearBullet = isClearBullet;
    }
}
