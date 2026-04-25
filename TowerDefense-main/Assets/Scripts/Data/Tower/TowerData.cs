using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 塔建造数据配置，包含各等级塔的属性
/// </summary>
[CreateAssetMenu(fileName = "TowerBuildData", menuName = "Data/TowerBuildData", order = 1)]
public class TowerData : ScriptableObject
{
    #region 变量
    [Header("JSON 配置")]
    [Tooltip("绑定的 JSON 配置文件（可选）")]
    [SerializeField] private TextAsset m_jsonFile;

    [SerializeField] private List<TowerLevelData> m_levelData;
    #endregion

    #region 属性
    public TextAsset JsonFile => m_jsonFile;
    public List<TowerLevelData> LevelData => m_levelData;
    #endregion
}

/// <summary>
/// 塔等级数据结构
/// </summary>
[System.Serializable]
public struct TowerLevelData
{
    public string name;                     // 等级名称
    public int cost;                        // 建造花费
    public float basicAttack;               // 基础攻击力
    public float attackRange;               // 攻击范围（单位：米）
    public float basicAttackCoolDown;       // 攻击冷却时间（单位：秒）
    public float criticalRate;              // 暴击率
    public DamageType damageType;
    public GameObject towerViewPrefab;      // 塔的外观预制体
    public List<Buff> buffs;

    public TowerLevelData(string name, int cost, float basicAttack, float attackRange, float basicAttackCoolDown, float criticalRate, GameObject towerViewPrefab, List<Buff> buffs, DamageType damageType)
    {
        this.name = name;
        this.cost = cost;
        this.basicAttack = basicAttack;
        this.attackRange = attackRange;
        this.basicAttackCoolDown = basicAttackCoolDown;
        this.criticalRate = criticalRate;
        this.towerViewPrefab = towerViewPrefab;
        this.buffs = buffs;
        this.damageType = damageType;
    }
}
