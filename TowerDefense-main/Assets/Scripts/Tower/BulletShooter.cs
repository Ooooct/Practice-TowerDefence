using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 子弹发射器，负责塔的攻击和子弹发射逻辑
/// </summary>
public class BulletShooter : TowerComponentBase
{
    #region 变量
    private Vector3 m_firePoint;
    private float m_shootTimer = 0.0f;
    private EnemyFinder m_find;
    private AttackData m_attackDataToSend;
    private ModifiableFloat m_modifiableShootCoolDown = new ModifiableFloat(1.0f);
    private DamageType damageType;
    public DamageType DamageType { get => damageType; set => damageType = value; }

    public AttackData AttackDataToSend { get => m_attackDataToSend; set => m_attackDataToSend = value; }

    private float m_basicAttackDamage;
    public float BasicAttackDamage
    {
        get { return m_basicAttackDamage; }
        set { m_basicAttackDamage = value; }
    }

    private float m_basicCritRate;
    public float CriticalRate
    {
        get { return m_basicCritRate; }
        set { m_basicCritRate = value; }
    }

    /// <summary>
    /// 射击冷却时间（基础值）
    /// </summary>
    public float ShootCoolDown
    {
        get => m_modifiableShootCoolDown.BaseValue;
        set
        {
            m_modifiableShootCoolDown.BaseValue = value;
        }
    }

    /// <summary>
    /// 可修改的射击冷却时间
    /// </summary>
    public ModifiableFloat ModifiableShootCoolDown
    {
        get { return m_modifiableShootCoolDown; }
        set
        {
            // 取消旧的订阅
            if (m_modifiableShootCoolDown.OnValueChanged != null)
            {
                m_modifiableShootCoolDown.OnValueChanged -= OnShootCoolDownChanged;
            }

            m_modifiableShootCoolDown = value;

            // 订阅新的值变化事件
            m_modifiableShootCoolDown.OnValueChanged += OnShootCoolDownChanged;

            // 立即应用
            OnShootCoolDownChanged(m_modifiableShootCoolDown.Value);
        }
    }
    #endregion

    public void SetAttackRange(float range)
    {
        m_find.SetAttackRange(range);
    }

    #region 生命周期
    override public void Awake()
    {
        base.Awake();
        m_firePoint = m_tower.transform.position + new Vector3(0, 0.5f, 0);
        m_find = new EnemyFinder();
        m_find.SetTowerMain(m_tower);

        // 订阅射击冷却时间变化事件（ModifiableFloat 是结构体，已在字段声明时初始化）
        m_modifiableShootCoolDown.OnValueChanged += OnShootCoolDownChanged;
    }

    override public void Update()
    {
        base.Update();
        //简易射击计时器，到0
        if (m_shootTimer > 0.0f)
        {
            m_shootTimer -= Time.deltaTime;
        }
        else
        {
            m_shootTimer = 0;
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (m_shootTimer <= 0.0f)
        {
            FireBullet();
        }
    }

    public override void OnRecycle()
    {
        base.OnRecycle();

        // 取消订阅
        m_modifiableShootCoolDown.OnValueChanged -= OnShootCoolDownChanged;

        m_modifiableShootCoolDown = default;
        m_shootTimer = 0.0f;
    }
    #endregion

    #region Private 方法
    /// <summary>
    /// 射击冷却时间变化时的回调
    /// </summary>
    private void OnShootCoolDownChanged(float newCoolDown)
    {
        if (m_shootTimer > newCoolDown)
        {
            m_shootTimer = newCoolDown;
        }
    }

    /// <summary>
    /// 发射子弹
    /// </summary>
    void FireBullet()
    {
        EnemyMain target = m_find.FindEnemy();
        if (target == null)
        {
            return;
        }

        // 确定发射位置
        Vector3 spawnPosition = m_firePoint != null ? m_firePoint : m_tower.transform.position;

        // 计算朝向目标的旋转
        Vector3 direction = (target.transform.position - spawnPosition).normalized;
        Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction);

        // 从对象池中生成子弹
        BulletMain bullet = PoolManager.Instance.Spawn<BulletMain>(spawnPosition, rotation);

        if (bullet != null)
        {
            // 重置射击计时器，使用 ModifiableFloat 的当前值
            m_shootTimer = m_modifiableShootCoolDown.Value;

            // 通过事件系统发送子弹数据
            m_attackDataToSend = new AttackData(m_basicAttackDamage, m_basicCritRate, m_tower, DamageType);
            SendBulletDataEvent bulletData = new SendBulletDataEvent(bullet, m_attackDataToSend, target);
            EventBus.Instance.Publish(bulletData);
        }
        else
        {
            Debug.LogWarning("[BulletShooter] - 无法从对象池获取子弹");
        }

    }

    #endregion
}

public class SendBulletDataEvent : IEvent
{
    public BulletMain bullet;
    public AttackData attackData;
    public List<string> HitStrategies;
    public EnemyMain target;

    public SendBulletDataEvent(BulletMain bullet, AttackData attackData, EnemyMain target)
    {
        this.bullet = bullet;
        this.attackData = attackData;
        this.HitStrategies = new List<string> { "Default" };
        this.target = target;
    }
}