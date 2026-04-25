using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class BulletMain : MonoBehaviour, IPoolable, IBuffReceiver
{
    private Dictionary<Type, BulletComponentBase> m_componentCache = new Dictionary<Type, BulletComponentBase>();

    private AttackCheck m_attackCheck;
    private TrackingMovement m_trackingMovement;
    private AttackData m_attackData;
    private BuffManager m_buffManager;
    private BulletDestroy m_bulletDestroy;
    private bool m_shouldRecycleAtEndOfFrame = false;

    public BuffManager BuffManager { get { return m_buffManager; } }
    public AttackData AttackData
    {
        get { return m_attackData; }
        set { m_attackData = value; }
    }

    /// <summary>
    /// 获取指定类型的塔组件
    /// </summary>
    /// <typeparam name="T">组件类型，必须继承自 ComponentBase</typeparam>
    /// <returns>组件实例，如果不存在则返回 null</returns>
    public T GetObjectComponent<T>() where T : ComponentBase
    {
        Type componentType = typeof(T);

        if (m_componentCache.TryGetValue(componentType, out BulletComponentBase component))
        {
            return component as T;
        }

        Debug.LogWarning($"[BulletMain] - 未找到组件类型: {componentType.Name}");
        return null;
    }

    #region Unity 生命周期
    private void Awake()
    {
        // 初始化子弹组件
        m_attackCheck = InitComponent<AttackCheck>();
        m_trackingMovement = InitComponent<TrackingMovement>();
        m_bulletDestroy = InitComponent<BulletDestroy>();

        m_buffManager = new BuffManager();
        m_buffManager.Owner = this;

        m_attackCheck.Awake();
    }
    private void Start()
    {
        m_attackCheck.Start();
        m_trackingMovement.Start();
        m_bulletDestroy.Start();
    }

    public void OnSpawn()
    {
        m_shouldRecycleAtEndOfFrame = false;

        // 重置攻击检测组件的命中记录
        m_attackCheck?.ResetHitTargets();

        // 订阅子弹数据设置事件（Late 优先级，确保在其他初始化之后执行）
        EventBus.Instance?.Subscribe<SendBulletDataEvent>(HandleBulletDataEvent, EventPriority.Late);

        // 订阅命中事件（Late 优先级，确保在伤害处理之后才回收子弹）
        EventBus.Instance?.Subscribe<HitEnemyEvent>(HandleHitEvent, EventPriority.Late);

        // 添加到 UnitManager（每次生成都需要注册）
        UnitManager.Instance?.AddUnit(this);
    }
    private void Update()
    {
        m_attackCheck.Update();
        m_trackingMovement.Update();
        m_bulletDestroy.Update();
    }
    private void LateUpdate()
    {
        m_attackCheck.LateUpdate();
        m_trackingMovement.LateUpdate();
    }
    private void FixedUpdate()
    {
        m_attackCheck.FixedUpdate();
        m_trackingMovement.FixedUpdate();
    }
    public void OnRecycle()
    {
        // 取消事件订阅
        EventBus.Instance?.Unsubscribe<SendBulletDataEvent>(HandleBulletDataEvent);
        EventBus.Instance?.Unsubscribe<HitEnemyEvent>(HandleHitEvent);

        m_shouldRecycleAtEndOfFrame = false;

        // 回收组件
        m_attackCheck?.OnRecycle();
        m_trackingMovement?.OnRecycle();
        m_bulletDestroy?.OnRecycle();
        m_buffManager?.OnRecycle();

        // 从 UnitManager 移除
        UnitManager.Instance?.RemoveUnit(this);

        // 清空攻击数据
        m_attackData = default;
    }

    /// <summary>
    /// 3D碰撞检测，转发给AttackCheck组件处理
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (m_shouldRecycleAtEndOfFrame)
            return;

        if (m_attackCheck != null)
        {
            m_attackCheck.HandleTriggerEnter(other);
        }
    }

    /// <summary>
    /// 处理子弹数据设置事件
    /// </summary>
    private void HandleBulletDataEvent(SendBulletDataEvent evt)
    {
        // 检查事件是否是发给这个子弹的
        if (evt.bullet != this)
            return;

        // 设置攻击数据
        m_attackData = evt.attackData;

        // 设置目标
        if (evt.target != null && m_trackingMovement != null)
        {
            m_trackingMovement.SetTarget(evt.target);
        }

        // 设置命中策略
        if (evt.HitStrategies != null && evt.HitStrategies.Count > 0 && m_attackCheck != null)
        {
            // 清空现有策略
            m_attackCheck.SetHitStrategy(evt.HitStrategies[0]);

            // 如果有多个策略，依次添加
            for (int i = 1; i < evt.HitStrategies.Count; i++)
            {
                m_attackCheck.AddHitStrategy(evt.HitStrategies[i]);
            }

            Debug.Log($"[BulletMain] - 设置命中策略，共 {evt.HitStrategies.Count} 个");
        }
    }

    /// <summary>
    /// 处理命中敌人事件
    /// </summary>
    private void HandleHitEvent(HitEnemyEvent evt)
    {
        // 检查事件是否是由这个子弹触发的
        if (evt.callerBullet != this)
            return;

        // 如果事件标记需要清除子弹，且还未标记过回收
        if (evt.isClearBullet && !m_shouldRecycleAtEndOfFrame)
        {
            m_shouldRecycleAtEndOfFrame = true;
            StartCoroutine(RecycleAtEndOfFixedUpdate());
        }
    }

    /// <summary>
    /// 在帧末尾回收子弹
    /// </summary>
    private IEnumerator RecycleAtEndOfFixedUpdate()
    {
        yield return new WaitForFixedUpdate();

        // 确保在回收前仍然标记为需要回收（防止意外情况）
        if (m_shouldRecycleAtEndOfFrame)
        {
            PoolManager.Instance?.Recycle(this);
        }
    }
    #endregion

    #region 属性
    public GameObject GameObject => gameObject;
    #endregion

    #region private方法
    /// <summary>
    /// 初始化敌人组件的通用方法
    /// </summary>
    /// <typeparam name="T">继承自 BulletComponentBase 的组件类型</typeparam>
    /// <returns>初始化完成的组件实例</returns>
    private T InitComponent<T>() where T : BulletComponentBase, new()
    {
        T component = new T();
        component.SetBulletMain(this);

        // 将组件添加到缓存中
        Type componentType = typeof(T);
        m_componentCache[componentType] = component;

        return component;
    }
    #endregion
}

public struct AttackData
{
    public float damage;
    public DamageType damageType;
    public float critRate;
    public TowerMain sourceTower;
    public List<string> applyBuffs;
    public AttackData(float damage, float critRate, TowerMain sourceTower, DamageType damageType = DamageType.Normal)
    {
        this.damage = damage;
        this.critRate = critRate;
        this.sourceTower = sourceTower;
        this.damageType = damageType;
        applyBuffs = new List<string>();
    }
}

public enum DamageType
{
    Normal,
    Fire,
    Ice,
    Electric
}