using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EnemyData 可序列化的数据结构
/// </summary>
[Serializable]
public class SerializableEnemyData
{
    public float maxHealth;
    public int goldReward;
    public float armor;
    public float speed;
    public List<string> buffReferences; // Buff 的资产引用列表
    public List<string> immuneBuffReferences; // 免疫 Buff 的资产引用列表
    public List<SerializableDamageReduce> damageReduce; // 伤害减免列表
    public string viewPrefabReference; // GameObject 的资产引用
    public string Describe; // 描述
    public float size;
}

[Serializable]
public class SerializableDamageReduce
{
    public string type; // DamageType 的字符串表示
    public float reduceValue;
}

/// <summary>
/// EnemyData 自定义编辑器
/// </summary>
[CustomEditor(typeof(EnemyData))]
public class EnemyDataEditor : DataEditorBase<EnemyData>
{
    protected override object CreateSerializableData(EnemyData data)
    {
        var serializableData = new SerializableEnemyData
        {
            maxHealth = data.maxHealth,
            goldReward = data.goldReward,
            armor = data.armor,
            speed = data.speed,
            viewPrefabReference = GetAssetReference(data.viewPrefab),
            Describe = data.Describe,
            size = data.size,
            buffReferences = new List<string>(),
            immuneBuffReferences = new List<string>(),
            damageReduce = new List<SerializableDamageReduce>()
        };

        // 转换 Buff 列表
        if (data.buffs != null)
        {
            foreach (var buff in data.buffs)
            {
                string buffReference = GetAssetReference(buff);
                serializableData.buffReferences.Add(buffReference);
            }
        }

        // 转换免疫 Buff 列表
        if (data.immuneBuffs != null)
        {
            foreach (var buff in data.immuneBuffs)
            {
                string buffReference = GetAssetReference(buff);
                serializableData.immuneBuffReferences.Add(buffReference);
            }
        }

        // 转换 damageReduce 列表
        if (data.damageReduce != null)
        {
            foreach (var reduce in data.damageReduce)
            {
                serializableData.damageReduce.Add(new SerializableDamageReduce
                {
                    type = reduce.type.ToString(),
                    reduceValue = reduce.reduceValue
                });
            }
        }

        return serializableData;
    }

    protected override void ApplySerializableData(EnemyData target, object deserializedData)
    {
        var data = deserializedData as SerializableEnemyData;
        if (data == null)
        {
            Debug.LogError("[EnemyDataEditor] 反序列化数据类型不匹配");
            return;
        }

        // 直接设置公共字段
        target.maxHealth = data.maxHealth;
        target.goldReward = data.goldReward;
        target.armor = data.armor;
        target.speed = data.speed;
        target.Describe = data.Describe;
        target.size = data.size;

        // 加载 WayPointsData


        // 加载 ViewPrefab
        if (!string.IsNullOrEmpty(data.viewPrefabReference))
        {
            target.viewPrefab = LoadAssetReference<GameObject>(data.viewPrefabReference);
        }
        else
        {
            target.viewPrefab = null;
        }

        // 加载 Buff 列表
        target.buffs = new List<Buff>();
        if (data.buffReferences != null)
        {
            foreach (var buffRef in data.buffReferences)
            {
                if (!string.IsNullOrEmpty(buffRef))
                {
                    var buff = LoadAssetReference<Buff>(buffRef);
                    if (buff != null)
                    {
                        target.buffs.Add(buff);
                    }
                }
            }
        }

        // 加载免疫 Buff 列表
        target.immuneBuffs = new List<Buff>();
        if (data.immuneBuffReferences != null)
        {
            foreach (var buffRef in data.immuneBuffReferences)
            {
                if (!string.IsNullOrEmpty(buffRef))
                {
                    var buff = LoadAssetReference<Buff>(buffRef);
                    if (buff != null)
                    {
                        target.immuneBuffs.Add(buff);
                    }
                }
            }
        }

        // 加载 damageReduce 列表
        target.damageReduce = new List<damageReduce>();
        if (data.damageReduce != null)
        {
            foreach (var serializableReduce in data.damageReduce)
            {
                DamageType damageType = DamageType.Normal;
                if (!string.IsNullOrEmpty(serializableReduce.type))
                {
                    if (Enum.TryParse(serializableReduce.type, out DamageType parsedType))
                    {
                        damageType = parsedType;
                    }
                }

                target.damageReduce.Add(new damageReduce
                {
                    type = damageType,
                    reduceValue = serializableReduce.reduceValue
                });
            }
        }
    }

    protected override Type GetSerializableDataType()
    {
        return typeof(SerializableEnemyData);
    }
}

