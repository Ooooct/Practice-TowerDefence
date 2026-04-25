using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Data/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("JSON 配置")]
    [Tooltip("绑定的 JSON 配置文件（可选）")]
    [SerializeField] private TextAsset m_jsonFile;

    public TextAsset JsonFile => m_jsonFile;

    public float maxHealth;
    public int goldReward;
    public float armor;
    public float speed;
    public List<Buff> buffs;
    public List<Buff> immuneBuffs;
    public List<damageReduce> damageReduce;
    public GameObject viewPrefab;
    public string Describe;
    public float size = 1;
}

[Serializable]
public class damageReduce
{
    public DamageType type;
    public float reduceValue;
}