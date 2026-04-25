using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff ScriptableObject，包含多个 BuffEffect 用于定义复杂的 Buff 行为。
/// </summary>
[CreateAssetMenu(fileName = "Buff", menuName = "Buffs/Buff")]
public class Buff : ScriptableObject
{
    [Header("JSON 配置")]
    [Tooltip("绑定的 JSON 配置文件（可选）")]
    [SerializeField] private TextAsset m_jsonFile;

    public TextAsset JsonFile => m_jsonFile;

    public float duration = -1f; // 持续时间，-1 表示无限制
    public int stacks = -1;    // 堆叠数，-1 表示无限制
    public int maxStacks = -1; // 最大堆叠数，-1 表示无限制

    [Header("周期触发配置")]
    [Tooltip("周期触发间隔（秒），0 或负数表示不触发")]
    public float tickInterval = 0f;

    [Header("优先级配置")]
    [Tooltip("Buff 优先级，用于 Large 策略比较，数值越大优先级越高")]
    public int priority = 0;

    [SerializeField] private List<BuffEffectBase> m_effects = new List<BuffEffectBase>();
    [SerializeField] private List<BuffConflictConfig> m_confidedBuffs = new List<BuffConflictConfig>();
    [SerializeField] private SameStrategies m_sameStrategy = SameStrategies.Return;

    public IReadOnlyList<BuffEffectBase> Effects => m_effects;
    public IReadOnlyList<BuffConflictConfig> ConflictConfigs => m_confidedBuffs;
    public SameStrategies SameStrategy => m_sameStrategy;
}

[Serializable]
public struct BuffConflictConfig
{
    public Buff conflictedBuff;
    public ConflictStrategies strategies;
    public int priority;
    BuffConflictConfig(Buff conflictedBuff, ConflictStrategies strategies, int priority = 0)
    {
        this.conflictedBuff = conflictedBuff;
        this.strategies = strategies;
        this.priority = priority;
    }
}

public enum ConflictStrategies
{
    PriorityMute, // 使用优先级决定生效
    OtherMute,// 无效化冲突者
    SelfMute, // 无效化自己
    Replace, // 替换旧的Buff
}

public enum SameStrategies
{
    Ignore, //无视，追加新的
    Add, //增加层数时间等Buff的内容
    Refresh, //刷新持续时间或者buff
    Return, //拒绝添加新的buff
    Large //取大值
}