using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Buff 可序列化的数据结构
/// </summary>
[Serializable]
public class SerializableBuff
{
    public float duration;
    public int stacks;
    public int maxStacks;
    public float tickInterval;
    public List<string> effectReferences; // BuffEffectBase 的资产引用列表
    public List<SerializableBuffConflictConfig> conflictConfigs;
    public SameStrategies sameStrategy;
}

[Serializable]
public class SerializableBuffConflictConfig
{
    public string conflictedBuffReference; // Buff 的资产引用
    public ConflictStrategies strategies;
    public int priority;
}

/// <summary>
/// Buff 自定义编辑器
/// </summary>
[CustomEditor(typeof(Buff))]
public class BuffEditor : DataEditorBase<Buff>
{
    protected override object CreateSerializableData(Buff data)
    {
        var serializableData = new SerializableBuff
        {
            duration = data.duration,
            stacks = data.stacks,
            maxStacks = data.maxStacks,
            tickInterval = data.tickInterval,
            sameStrategy = data.SameStrategy,
            effectReferences = new List<string>(),
            conflictConfigs = new List<SerializableBuffConflictConfig>()
        };

        // 转换 BuffEffect 列表
        if (data.Effects != null)
        {
            foreach (var effect in data.Effects)
            {
                string effectReference = GetAssetReference(effect);
                serializableData.effectReferences.Add(effectReference);
            }
        }

        // 转换冲突配置列表
        if (data.ConflictConfigs != null)
        {
            foreach (var config in data.ConflictConfigs)
            {
                var serializableConfig = new SerializableBuffConflictConfig
                {
                    conflictedBuffReference = GetAssetReference(config.conflictedBuff),
                    strategies = config.strategies,
                    priority = config.priority
                };
                serializableData.conflictConfigs.Add(serializableConfig);
            }
        }

        return serializableData;
    }

    protected override void ApplySerializableData(Buff target, object deserializedData)
    {
        var data = deserializedData as SerializableBuff;
        if (data == null)
        {
            Debug.LogError("[BuffEditor] 反序列化数据类型不匹配");
            return;
        }

        // 设置公共字段
        target.duration = data.duration;
        target.stacks = data.stacks;
        target.maxStacks = data.maxStacks;
        target.tickInterval = data.tickInterval;

        // 使用反射设置私有字段
        var effectsField = typeof(Buff).GetField("m_effects",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var conflictConfigsField = typeof(Buff).GetField("m_confidedBuffs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sameStrategyField = typeof(Buff).GetField("m_sameStrategy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // 加载 BuffEffect 列表
        var effects = new List<BuffEffectBase>();
        if (data.effectReferences != null)
        {
            foreach (var effectRef in data.effectReferences)
            {
                if (!string.IsNullOrEmpty(effectRef))
                {
                    var effect = LoadAssetReference<BuffEffectBase>(effectRef);
                    if (effect != null)
                    {
                        effects.Add(effect);
                    }
                }
            }
        }
        effectsField?.SetValue(target, effects);

        // 加载冲突配置列表
        var conflictConfigs = new List<BuffConflictConfig>();
        if (data.conflictConfigs != null)
        {
            foreach (var serializableConfig in data.conflictConfigs)
            {
                Buff conflictedBuff = null;
                if (!string.IsNullOrEmpty(serializableConfig.conflictedBuffReference))
                {
                    conflictedBuff = LoadAssetReference<Buff>(serializableConfig.conflictedBuffReference);
                }

                var config = new BuffConflictConfig
                {
                    conflictedBuff = conflictedBuff,
                    strategies = serializableConfig.strategies,
                    priority = serializableConfig.priority
                };
                conflictConfigs.Add(config);
            }
        }
        conflictConfigsField?.SetValue(target, conflictConfigs);

        // 设置相同策略
        sameStrategyField?.SetValue(target, data.sameStrategy);
    }

    protected override Type GetSerializableDataType()
    {
        return typeof(SerializableBuff);
    }
}

