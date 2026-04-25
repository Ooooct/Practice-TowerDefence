using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TowerData 可序列化的数据结构
/// </summary>
[Serializable]
public class SerializableTowerData
{
    public List<SerializableTowerLevelData> levelData;
}

[Serializable]
public class SerializableTowerLevelData
{
    public string name;
    public int cost;
    public float basicAttack;
    public float attackRange;
    public float basicAttackCoolDown;
    public float criticalRate;
    public string damageType; // 伤害类型的字符串表示
    public string towerViewPrefabReference; // GameObject 的资产引用
    public List<string> buffReferences; // Buff 的资产引用列表
}

/// <summary>
/// TowerData 自定义编辑器
/// </summary>
[CustomEditor(typeof(TowerData))]
public class TowerDataEditor : DataEditorBase<TowerData>
{
    protected override object CreateSerializableData(TowerData data)
    {
        var serializableData = new SerializableTowerData
        {
            levelData = new List<SerializableTowerLevelData>()
        };

        foreach (var level in data.LevelData)
        {
            var serializableLevel = new SerializableTowerLevelData
            {
                name = level.name,
                cost = level.cost,
                basicAttack = level.basicAttack,
                attackRange = level.attackRange,
                basicAttackCoolDown = level.basicAttackCoolDown,
                criticalRate = level.criticalRate,
                damageType = level.damageType.ToString(),
                towerViewPrefabReference = GetAssetReference(level.towerViewPrefab),
                buffReferences = new List<string>()
            };

            // 转换 Buff 列表
            if (level.buffs != null)
            {
                foreach (var buff in level.buffs)
                {
                    string buffReference = GetAssetReference(buff);
                    serializableLevel.buffReferences.Add(buffReference);
                }
            }

            serializableData.levelData.Add(serializableLevel);
        }

        return serializableData;
    }

    protected override void ApplySerializableData(TowerData target, object deserializedData)
    {
        var data = deserializedData as SerializableTowerData;
        if (data == null)
        {
            Debug.LogError("[TowerDataEditor] 反序列化数据类型不匹配");
            return;
        }

        // 使用反射设置私有字段
        var levelDataField = typeof(TowerData).GetField("m_levelData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (levelDataField == null)
        {
            Debug.LogError("[TowerDataEditor] 无法找到 m_levelData 字段");
            return;
        }

        var levelDataList = new List<TowerLevelData>();

        foreach (var serializableLevel in data.levelData)
        {
            GameObject prefab = null;
            if (!string.IsNullOrEmpty(serializableLevel.towerViewPrefabReference))
            {
                prefab = LoadAssetReference<GameObject>(serializableLevel.towerViewPrefabReference);
            }

            var buffs = new List<Buff>();
            if (serializableLevel.buffReferences != null)
            {
                foreach (var buffRef in serializableLevel.buffReferences)
                {
                    if (!string.IsNullOrEmpty(buffRef))
                    {
                        var buff = LoadAssetReference<Buff>(buffRef);
                        if (buff != null)
                        {
                            buffs.Add(buff);
                        }
                    }
                }
            }

            // 解析伤害类型
            DamageType damageType = DamageType.Normal;
            if (!string.IsNullOrEmpty(serializableLevel.damageType))
            {
                if (Enum.TryParse(serializableLevel.damageType, out DamageType parsedType))
                {
                    damageType = parsedType;
                }
            }

            var level = new TowerLevelData(
                serializableLevel.name,
                serializableLevel.cost,
                serializableLevel.basicAttack,
                serializableLevel.attackRange,
                serializableLevel.basicAttackCoolDown,
                serializableLevel.criticalRate,
                prefab,
                buffs,
                damageType
            );

            levelDataList.Add(level);
        }

        levelDataField.SetValue(target, levelDataList);
    }

    protected override Type GetSerializableDataType()
    {
        return typeof(SerializableTowerData);
    }
}

