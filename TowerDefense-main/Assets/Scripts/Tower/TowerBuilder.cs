using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 塔建造管理器，处理塔的建造、升级等逻辑
/// </summary>
public class TowerBuilder : TowerComponentBase
{

    //TODO 使用数据库SO来存储默认塔（工地）的数据，以便移除defaultTowerBuildData的存储
    [SerializeField] private TowerData m_defaultTowerBuildData;
    private TowerData m_towerBuildData;
    private int m_currentLevel = 0;
    private int m_upgradeCost = 0;

    #region 公共属性
    /// <summary>获取当前塔的等级</summary>
    public int CurrentLevel => m_currentLevel;

    /// <summary>获取当前塔的最大等级</summary>
    public int MaxLevel => m_towerBuildData?.LevelData.Count - 1 ?? 0;

    /// <summary>获取当前塔的建造数据</summary>
    public TowerData TowerBuildData => m_towerBuildData;
    #endregion

    #region Public方法
    /// <summary>
    /// 建造新塔
    /// </summary>
    /// <param name="evt">建造事件，包含塔等级数据</param>
    public void BuildNewTower(TryBuildTowerEvent evt)
    {
        if (evt.targetTowerSpot != m_tower.gameObject)
            return;

        //如果是本塔的更高一级，其实是升级，调用升级方法
        if (evt.towerBuildData == m_towerBuildData && evt.towerLevel == m_currentLevel + 1)
        {
            UpgradeTower();
            return;
        }

        Debug.Log($"[TowerBuilder] - 尝试建造塔: {evt.towerName}");

        // 应用塔数据（不更新当前等级，因为这是新建）
        ApplyTowerData(evt.towerBuildData, evt.towerLevel, updateCurrentLevel: false);
    }

    public void UpgradeTower()
    {
        int nextLevel = m_currentLevel + 1;
        if (nextLevel >= m_towerBuildData.LevelData.Count)
        {
            Debug.LogWarning("[TowerBuilder] - 已经达到最高等级，无法升级");
            return;
        }

        // 应用塔数据并更新当前等级
        ApplyTowerData(m_towerBuildData, nextLevel, updateCurrentLevel: true);
        Debug.Log($"[TowerBuilder] - 塔升级到等级 {nextLevel}");
    }

    public void DemolishTower()
    {
        Debug.Log("[TowerBuilder] - 塔已被拆除");
        m_towerBuildData = null;
        m_currentLevel = 0;
    }
    #endregion

    #region 生命周期
    override public void Start()
    {
        base.Start();
        EventBus.Instance.Subscribe<TryBuildTowerEvent>(BuildNewTower);

        //初始化为默认塔数据（工地）
        m_defaultTowerBuildData = AssetManager.Instance.GetAsset<TowerData>(CategoriesEnum.Towers, "Default");
        ApplyTowerData(m_defaultTowerBuildData, 0, updateCurrentLevel: false);
    }

    override public void OnRecycle()
    {
        EventBus.Instance.Unsubscribe<TryBuildTowerEvent>(BuildNewTower);
    }
    #endregion

    #region Private方法
    /// <summary>
    /// 应用塔数据（合并的统一方法）
    /// </summary>
    /// <param name="towerBuildData">塔建造数据</param>
    /// <param name="level">目标等级</param>
    /// <param name="updateCurrentLevel">是否更新当前等级字段</param>
    private void ApplyTowerData(TowerData towerBuildData, int level, bool updateCurrentLevel)
    {
        if (towerBuildData == null || level >= towerBuildData.LevelData.Count)
        {
            Debug.LogError($"[TowerBuilder] - 无效的塔数据或等级: level={level}");
            return;
        }

        TowerLevelData levelData = towerBuildData.LevelData[level];

        // 检查是否有足够的金钱
        if (!CostManager.Instance.TryCost(levelData.cost))
        {
            Debug.LogWarning($"[TowerBuilder] - 金钱不足，无法建造/升级塔，需要: {levelData.cost}");
            return;
        }

        Debug.Log($"[TowerBuilder] - 应用塔数据: {levelData.name}, 等级: {level}, 花费: {levelData.cost}");

        // 更新塔数据引用
        m_towerBuildData = towerBuildData;

        // 根据需要更新当前等级
        if (updateCurrentLevel)
        {
            m_currentLevel = level;
        }

        // 应用 Buffs
        foreach (Buff buff in levelData.buffs)
        {
            m_tower.BuffManager.AddBuff(buff);
        }

        // 设置塔名称
        m_tower.gameObject.name = levelData.name;

        // 应用塔属性到 BulletShooter
        BulletShooter bulletShooter = m_tower.GetObjectComponent<BulletShooter>();
        if (bulletShooter != null)
        {
            bulletShooter.ShootCoolDown = levelData.basicAttackCoolDown;
            bulletShooter.SetAttackRange(levelData.attackRange);
            bulletShooter.BasicAttackDamage = levelData.basicAttack;
            bulletShooter.CriticalRate = levelData.criticalRate;
            bulletShooter.DamageType = levelData.damageType;
        }

        // 移除旧的塔模型（所有子物体）
        foreach (Transform child in m_tower.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        // 实例化新的塔模型
        if (levelData.towerViewPrefab != null)
        {
            GameObject towerModel = GameObject.Instantiate(levelData.towerViewPrefab, m_tower.transform);
            towerModel.transform.localPosition = Vector3.zero;
            towerModel.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning($"[TowerBuilder] - 塔等级数据中没有视图预制体");
        }

        // 发布建造完成事件
        EventBus.Instance.Publish(new CompletedBuildEvent());
    }
    #endregion
}
