using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemyMain : MonoBehaviour, IPoolable, IBuffReceiver
{
    private Dictionary<Type, EnemyComponentBase> m_componentCache = new Dictionary<Type, EnemyComponentBase>();
    private HealthHandler m_healthHandler;
    private EnemyMovement m_movement;
    private AttackReceiver m_attackReceiver;
    private EnemyDataApply m_enemySL;
    private BuffManager m_buffManager;
    public BuffManager BuffManager { get { return m_buffManager; } }
    private int m_goldReward;
    public int GoldReward { get => m_goldReward; set => m_goldReward = value; }
    public EnemyData enemyData;
    /// <summary>
    /// 获取指定类型的塔组件
    /// </summary>
    /// <typeparam name="T">组件类型，必须继承自 ComponentBase</typeparam>
    /// <returns>组件实例，如果不存在则返回 null</returns>
    public T GetObjectComponent<T>() where T : ComponentBase
    {
        Type componentType = typeof(T);

        if (m_componentCache.TryGetValue(componentType, out EnemyComponentBase component))
        {
            return component as T;
        }

        Debug.LogWarning($"[EnemyMain] - 未找到组件类型: {componentType.Name}");
        return null;
    }

    #region Unity 生命周期
    private void Awake()
    {
        // 初始化敌人组件
        m_healthHandler = InitComponent<HealthHandler>();
        m_movement = InitComponent<EnemyMovement>();
        m_attackReceiver = InitComponent<AttackReceiver>();
        m_enemySL = InitComponent<EnemyDataApply>();

        m_buffManager = new BuffManager();
        m_buffManager.Owner = this;

        m_healthHandler.Awake();
        m_movement.Awake();
        m_attackReceiver.Awake();
        m_enemySL.Awake();
    }
    private void Start()
    {
    }
    public void OnSpawn()
    {
        //加入到单位管理
        UnitManager.Instance.AddEnemy(this);

        m_healthHandler.Start();
        m_movement.Start();
        m_attackReceiver.Start();
        m_enemySL.Start();
    }
    private void Update()
    {
        m_healthHandler.Update();
        m_movement.Update();
        m_attackReceiver.Update();
        m_enemySL.Update();
        m_buffManager.Update();
    }
    private void LateUpdate()
    {
        m_healthHandler.LateUpdate();
        m_movement.LateUpdate();
        m_attackReceiver.LateUpdate();
        m_enemySL.LateUpdate();
        m_buffManager.LateUpdate();
    }
    private void FixedUpdate()
    {
        m_healthHandler.FixedUpdate();
        m_movement.FixedUpdate();
        m_attackReceiver.FixedUpdate();
        m_enemySL.FixedUpdate();
        m_buffManager.FixedUpdate();
    }
    public void OnRecycle()
    {
        m_healthHandler.OnRecycle();
        m_movement.OnRecycle();
        m_attackReceiver.OnRecycle();
        m_enemySL.OnRecycle();
        m_buffManager.OnRecycle();

        // 从单位管理器中移除
        UnitManager.Instance.RemoveEnemy(this);
        EventBus.Instance.Publish(new EnemyDieEvent(this));
    }

    #endregion

    #region 属性
    public EnemyMovement Movement => m_movement;
    public HealthHandler HealthHandler => m_healthHandler;
    public GameObject GameObject => gameObject;
    #endregion


    #region private方法
    /// <summary>
    /// 初始化敌人组件的通用方法
    /// </summary>
    /// <typeparam name="T">继承自 EnemyComponentBase 的组件类型</typeparam>
    /// <returns>初始化完成的组件实例</returns>
    private T InitComponent<T>() where T : EnemyComponentBase, new()
    {
        T component = new T();
        component.SetEnemyMain(this);

        // 将组件添加到缓存中
        Type componentType = typeof(T);
        m_componentCache[componentType] = component;

        return component;
    }
    #endregion
}

public struct EnemyDieEvent : IEvent
{
    public EnemyMain enemy;

    public EnemyDieEvent(EnemyMain enemy)
    {
        this.enemy = enemy;
    }
}

