using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 单位管理器，负责管理游戏中的所有单位（敌人、塔、子弹等）
public class UnitManager : MonoBehaviour
{
    #region 单例模式
    private static UnitManager m_instance;

    public static UnitManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("UnitManager");
                m_instance = obj.AddComponent<UnitManager>();
            }
            return m_instance;
        }
    }
    #endregion

    #region 集合管理
    private Dictionary<Type, IUnitCollection> m_unitCollections = new Dictionary<Type, IUnitCollection>();

    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            InitializeCollections();
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCollections()
    {
        m_unitCollections[typeof(EnemyMain)] = new UnitCollection<EnemyMain>();
        m_unitCollections[typeof(TowerMain)] = new UnitCollection<TowerMain>();
        m_unitCollections[typeof(BulletMain)] = new UnitCollection<BulletMain>();
    }
    #endregion

    #region 事件定义
    public static event Action<EnemyMain> OnEnemyAdded;
    public static event Action<EnemyMain> OnEnemyRemoved;

    public static event Action<TowerMain> OnTowerAdded;
    public static event Action<TowerMain> OnTowerRemoved;

    public static event Action<BulletMain> OnBulletAdded;
    public static event Action<BulletMain> OnBulletRemoved;
    #endregion

    #region 属性访问
    public IReadOnlyList<EnemyMain> Enemies => GetUnits<EnemyMain>();
    public IReadOnlyList<TowerMain> Towers => GetUnits<TowerMain>();
    public IReadOnlyList<BulletMain> Bullets => GetUnits<BulletMain>();

    public int EnemyCount => GetUnitCount<EnemyMain>();
    public int TowerCount => GetUnitCount<TowerMain>();
    public int BulletCount => GetUnitCount<BulletMain>();
    #endregion

    #region public通用单位操作
    public void AddUnit<T>(T unit) where T : class
    {
        var collection = GetCollection<T>();
        if (collection.Add(unit))
        {
            InvokeAddEvent(unit);
        }
        else
        {
            Debug.LogWarning($"[UnitManager] - {typeof(T).Name} 已存在，跳过添加");
        }
    }

    // 泛型方法：移除单位
    public void RemoveUnit<T>(T unit) where T : class
    {
        if (unit == null)
        {
            Debug.LogWarning($"[UnitManager] - 尝试移除空的 {typeof(T).Name} 对象");
            return;
        }

        var collection = GetCollection<T>();
        if (collection.Remove(unit))
        {
            InvokeRemoveEvent(unit);
        }
    }

    // 清空所有单位
    public void ClearUnits<T>() where T : class
    {
        var collection = GetCollection<T>();
        collection.Clear();
        Debug.Log($"[UnitManager] - 清空所有 {typeof(T).Name}");
    }

    // 获取单位列表
    public IReadOnlyList<T> GetUnits<T>() where T : class
    {
        return GetCollection<T>().GetUnits();
    }

    // 获取单位数量
    public int GetUnitCount<T>() where T : class
    {
        return GetCollection<T>().Count;
    }

    // 获取整个集合
    private UnitCollection<T> GetCollection<T>() where T : class
    {
        Type type = typeof(T);
        if (!m_unitCollections.ContainsKey(type))
        {
            m_unitCollections[type] = new UnitCollection<T>();
        }
        return m_unitCollections[type] as UnitCollection<T>;
    }
    #endregion

    #region 事件触发兼容
    private void InvokeAddEvent<T>(T unit)
    {
        if (unit is EnemyMain enemy)
            OnEnemyAdded?.Invoke(enemy);
        else if (unit is TowerMain tower)
            OnTowerAdded?.Invoke(tower);
        else if (unit is BulletMain bullet)
            OnBulletAdded?.Invoke(bullet);
    }

    // 触发移除事件
    private void InvokeRemoveEvent<T>(T unit)
    {
        if (unit is EnemyMain enemy)
            OnEnemyRemoved?.Invoke(enemy);
        else if (unit is TowerMain tower)
            OnTowerRemoved?.Invoke(tower);
        else if (unit is BulletMain bullet)
            OnBulletRemoved?.Invoke(bullet);
    }
    #endregion

    #region 兼容方法
    // 兼容的方法
    public void AddEnemy(EnemyMain enemy) => AddUnit(enemy);
    public void RemoveEnemy(EnemyMain enemy) => RemoveUnit(enemy);
    public void ClearAllEnemies() => ClearUnits<EnemyMain>();

    public void AddTower(TowerMain tower) => AddUnit(tower);
    public void RemoveTower(TowerMain tower) => RemoveUnit(tower);
    public void ClearAllTowers() => ClearUnits<TowerMain>();

    public void AddBullet(BulletMain bullet) => AddUnit(bullet);
    public void RemoveBullet(BulletMain bullet) => RemoveUnit(bullet);
    public void ClearAllBullets() => ClearUnits<BulletMain>();
    #endregion

    #region 范围查询
    // 获取指定水平范围（圆形）内的敌人
    public List<EnemyMain> GetEnemiesInRange2D(Vector3 position, float range)
    {
        List<EnemyMain> enemiesInRange = new List<EnemyMain>();
        float rangeSqr = range * range;

        foreach (var enemy in Enemies)
        {
            if (enemy == null) continue;

            Vector3 enemyPosition = enemy.transform.position;
            float distanceSqr = (enemyPosition.x - position.x) * (enemyPosition.x - position.x) +
                                (enemyPosition.z - position.z) * (enemyPosition.z - position.z);

            if (distanceSqr <= rangeSqr)
            {
                enemiesInRange.Add(enemy);
            }
        }

        return enemiesInRange;
    }

    // 泛型版本：获取指定范围内的单位
    public List<T> GetUnitsInRange2D<T>(Vector3 position, float range) where T : MonoBehaviour
    {
        List<T> unitsInRange = new List<T>();
        float rangeSqr = range * range;

        foreach (var unit in GetUnits<T>())
        {
            if (unit == null) continue;

            Vector3 unitPosition = unit.transform.position;
            float distanceSqr = (unitPosition.x - position.x) * (unitPosition.x - position.x) +
                                (unitPosition.z - position.z) * (unitPosition.z - position.z);

            if (distanceSqr <= rangeSqr)
            {
                unitsInRange.Add(unit);
            }
        }

        return unitsInRange;
    }
    #endregion
}

#region 集合实现
internal interface IUnitCollection
{
    int Count { get; }
    void Clear();
}

internal class UnitCollection<T> : IUnitCollection where T : class
{
    private List<T> m_units = new List<T>();

    public int Count => m_units.Count;

    public bool Add(T unit)
    {
        if (m_units.Contains(unit))
            return false;

        m_units.Add(unit);
        return true;
    }

    public bool Remove(T unit)
    {
        return m_units.Remove(unit);
    }

    public void Clear()
    {
        m_units.Clear();
    }

    public IReadOnlyList<T> GetUnits()
    {
        return m_units;
    }
}
#endregion