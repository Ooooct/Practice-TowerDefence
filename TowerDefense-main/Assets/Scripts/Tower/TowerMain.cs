using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 塔主控制器，管理塔的基本参数以及组件的引用
/// </summary>
public class TowerMain : MonoBehaviour, IBuffReceiver
{
    #region 变量
    private TowerBuilder m_towerBuilder;
    private BulletShooter m_bulletShooter;
    private DamageStatistics m_damageStatistics;
    private BuffManager m_buffManager;
    public BuffManager BuffManager { get { return m_buffManager; } }
    private Dictionary<Type, TowerComponentBase> m_componentCache = new Dictionary<Type, TowerComponentBase>();
    #endregion

    #region Public 方法
    public void SetDefaultTowerBuilder()
    {
        OnRecycle();
    }

    /// <summary>
    /// 获取指定类型的塔组件
    /// </summary>
    /// <typeparam name="T">组件类型，必须继承自 TowerComponentBase</typeparam>
    /// <returns>组件实例，如果不存在则返回 null</returns>
    public T GetObjectComponent<T>() where T : ComponentBase
    {
        Type componentType = typeof(T);

        if (m_componentCache.TryGetValue(componentType, out TowerComponentBase component))
        {
            return component as T;
        }

        Debug.LogWarning($"[TowerMain] - 未找到组件类型: {componentType.Name}");
        return null;
    }
    #endregion

    #region Unity 生命周期
    private void Awake()
    {
        // 初始化塔组件
        m_towerBuilder = InitComponent<TowerBuilder>();
        m_bulletShooter = InitComponent<BulletShooter>();
        m_damageStatistics = InitComponent<DamageStatistics>();
        m_buffManager = new BuffManager();
        m_buffManager.Owner = this;

        //TODO 使用事件解耦？ 11/12
        m_towerBuilder.Awake();
        m_bulletShooter.Awake();
        m_damageStatistics.Awake();
    }
    private void Start()
    {
        m_towerBuilder.Start();
        m_bulletShooter.Start();
        m_damageStatistics.Start();

        UnitManager.Instance.AddUnit(this);
    }
    private void Update()
    {
        m_towerBuilder.Update();
        m_bulletShooter.Update();
        m_damageStatistics.Update();
        m_damageStatistics.UpdateStatistics(Time.deltaTime);
    }
    private void LateUpdate()
    {
        m_towerBuilder.LateUpdate();
        m_bulletShooter.LateUpdate();
        m_damageStatistics.LateUpdate();
    }
    private void FixedUpdate()
    {
        m_towerBuilder.FixedUpdate();
        m_bulletShooter.FixedUpdate();
        m_damageStatistics.FixedUpdate();
    }
    private void OnRecycle()
    {
        m_towerBuilder.OnRecycle();
        m_bulletShooter.OnRecycle();
        m_damageStatistics.OnRecycle();
        m_buffManager.OnRecycle();

        UnitManager.Instance.RemoveUnit(this);
    }

    #endregion

    #region Private 方法
    /// <summary>
    /// 初始化塔组件的通用方法
    /// </summary>
    /// <typeparam name="T">继承自 TowerComponentBase 的组件类型</typeparam>
    /// <returns>初始化完成的组件实例</returns>
    private T InitComponent<T>() where T : TowerComponentBase, new()
    {
        T component = new T();
        component.SetTowerMain(this);

        // 将组件添加到缓存中
        Type componentType = typeof(T);
        m_componentCache[componentType] = component;

        return component;
    }
    #endregion
}
