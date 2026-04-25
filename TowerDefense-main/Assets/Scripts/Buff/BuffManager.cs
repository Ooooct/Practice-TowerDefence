using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff管理器类，负责管理游戏中所有的Buff实例
/// </summary>
public class BuffManager : ComponentBase
{
    #region 字段与属性
    // 存储当前活跃的Buff实例列表
    private List<BuffInstance> m_activeBuffs = new List<BuffInstance>();
    // 免疫 Buff 列表，列表中的 Buff 不会被添加到管理器
    private List<Buff> m_immuneBuffs = new List<Buff>();
    // Buff所有者，Buff作用的对象
    private IBuffReceiver m_owner;
    public IBuffReceiver Owner { get { return m_owner; } set { m_owner = value; } }
    public IReadOnlyList<Buff> ImmuneBuffs => m_immuneBuffs;
    #endregion

    #region 免疫 Buff 管理
    /// <summary>
    /// 添加免疫的 Buff 到列表
    /// </summary>
    public void AddImmuneBuff(Buff buff)
    {
        if (!m_immuneBuffs.Contains(buff))
        {
            m_immuneBuffs.Add(buff);
            // 移除所有该类型的活跃 Buff
            RemoveAllBuffsOfType(buff);
        }
    }
    /// <summary>
    /// 从免疫列表中移除 Buff
    /// </summary>
    public void RemoveImmuneBuff(Buff buff)
    {
        m_immuneBuffs.Remove(buff);
    }
    /// <summary>
    /// 检查是否免疫某个 Buff
    /// </summary>
    public bool IsImmuneTo(Buff buff)
    {
        return buff != null && m_immuneBuffs.Contains(buff);
    }
    /// <summary>
    /// 移除所有指定类型的活跃 Buff
    /// </summary>
    private void RemoveAllBuffsOfType(Buff buff)
    {
        for (int i = m_activeBuffs.Count - 1; i >= 0; i--)
        {
            BuffInstance instance = m_activeBuffs[i];
            if (instance.BuffData == buff)
            {
                instance.Remove();
                m_activeBuffs.RemoveAt(i);
            }
        }
    }
    #endregion

    #region Buff 增删
    /// <summary>
    /// 添加一个Buff到管理器
    /// </summary>
    public void AddBuff(Buff buff) => AddBuff(buff, null);
    /// <summary>
    /// 添加一个Buff到管理器（带来源）
    /// </summary>
    public void AddBuff(Buff buff, object source)
    {
        // 0. 检查免疫列表
        if (IsImmuneTo(buff)) return;

        // 1. 检查相同策略（SameStrategies）
        BuffInstance existingSameInstance = FindBuffInstance(buff);
        if (existingSameInstance != null)
        {
            bool handled = HandleSameStrategy(buff, existingSameInstance);
            if (handled)
            {
                // 相同策略已处理，检查冲突后返回
                CheckAndHandleConflicts();
                return;
            }
        }

        // 2. 创建并添加新的 Buff 实例
        BuffInstance instance = new BuffInstance(buff, this, source);
        instance.Apply();
        m_activeBuffs.Add(instance);

        // 3. 检查并处理冲突（ConflictStrategies）
        CheckAndHandleConflicts();
    }

    public void AddBuff(string buff, object source = null)
    {
        Buff buffAsset = AssetManager.Instance.GetAsset<Buff>(CategoriesEnum.Buff, buff);
        AddBuff(buffAsset, source);
    }

    public void AddBuffFromIndex(string index, object source = null)
    {
        Buff buff = AssetManager.Instance.GetAsset<Buff>(CategoriesEnum.Buff, index);
        if (buff != null)
        {
            AddBuff(buff, source);
        }
    }
    /// <summary>
    /// 移除指定的Buff
    /// </summary>
    public void RemoveBuff(Buff buff)
    {
        for (int i = m_activeBuffs.Count - 1; i >= 0; i--)
        {
            BuffInstance instance = m_activeBuffs[i];
            if (instance.BuffData == buff)
            {
                instance.Remove();
                m_activeBuffs.RemoveAt(i);
            }
        }
        Debug.Log($"[BuffManager] - 移除 Buff: {buff.name} 从 {m_owner.GetObjectComponent<ComponentBase>()}");
    }
    public void RemoveBuff(BuffInstance buffInstance)
    {
        if (m_activeBuffs.Contains(buffInstance))
        {
            buffInstance.Remove();
            m_activeBuffs.Remove(buffInstance);

            // 移除后检查冲突（可能有被 Mute 的 Buff 需要恢复）
            CheckAndHandleConflicts();
        }
        Debug.Log($"[BuffManager] - 移除 Buff: {buffInstance.BuffData.name} 从 {m_owner.GetObjectComponent<ComponentBase>()}");
    }
    #endregion

    #region 相同策略处理
    /// <summary>
    /// 查找与指定 Buff 相同的实例
    /// 判断标准：
    /// 1. BuffData 完全相同（ScriptableObject 引用相同）
    /// 2. 或者 BuffEffect 的类型列表完全一致
    /// </summary>
    private BuffInstance FindBuffInstance(Buff buff)
    {
        foreach (var instance in m_activeBuffs)
        {
            // 方式1: BuffData 引用相同
            if (instance.BuffData == buff)
                return instance;

            // 方式2: BuffEffect 类型列表一致
            if (AreEffectTypesSame(instance.BuffData, buff))
                return instance;
        }
        return null;
    }

    /// <summary>
    /// 判断两个 Buff 的 BuffEffect 类型列表是否完全一致
    /// </summary>
    private bool AreEffectTypesSame(Buff buff1, Buff buff2)
    {
        // 数量不同，肯定不一致
        if (buff1.Effects.Count != buff2.Effects.Count)
            return false;

        // 创建类型集合进行比较
        var types1 = new HashSet<System.Type>();
        var types2 = new HashSet<System.Type>();

        foreach (var effect in buff1.Effects)
        {
            if (effect != null)
                types1.Add(effect.GetType());
        }

        foreach (var effect in buff2.Effects)
        {
            if (effect != null)
                types2.Add(effect.GetType());
        }

        // 使用 SetEquals 判断两个集合是否完全相同
        return types1.SetEquals(types2);
    }
    /// <summary>
    /// 处理相同 Buff 的策略
    /// </summary>
    private bool HandleSameStrategy(Buff buff, BuffInstance existingInstance)
    {
        switch (buff.SameStrategy)
        {
            case SameStrategies.Return:
                return true;
            case SameStrategies.Refresh:
                existingInstance.RefreshDuration();
                return true;
            case SameStrategies.Add:
                existingInstance.AddStacks(buff.stacks);
                existingInstance.RefreshDuration();
                return true;
            case SameStrategies.Ignore:
                return false;
            case SameStrategies.Large:
                // 比较优先级，优先级高的保留
                if (buff.priority > existingInstance.BuffData.priority)
                {
                    // 新 Buff 优先级更高，移除旧的
                    RemoveBuff(existingInstance);
                    return false; // 返回 false 表示允许添加新的
                }
                // 旧 Buff 优先级 >= 新 Buff，保留旧的
                return true;
            default:
                return false;
        }
    }
    #endregion

    #region Buff 冲突处理
    /// <summary>
    /// 检查并处理所有 Buff 之间的冲突
    /// </summary>
    private void CheckAndHandleConflicts()
    {
        for (int i = m_activeBuffs.Count - 1; i >= 0; i--)
        {
            BuffInstance currentInstance = m_activeBuffs[i];
            Buff currentBuff = currentInstance.BuffData;
            foreach (var conflictConfig in currentBuff.ConflictConfigs)
            {
                BuffInstance conflictedInstance = FindBuffInstance(conflictConfig.conflictedBuff);
                if (conflictedInstance == null)
                {
                    // 冲突的 Buff 不存在，如果当前 Buff 被 Mute，尝试恢复
                    if (currentInstance.IsMuted && ShouldUnMute(currentInstance))
                        currentInstance.UnMute();
                    continue;
                }
                // 存在冲突，根据策略处理
                HandleConflict(currentInstance, conflictedInstance, conflictConfig);
            }
        }
    }
    /// <summary>
    /// 检查 Buff 是否应该取消 Mute（所有冲突都已解除）
    /// </summary>
    private bool ShouldUnMute(BuffInstance instance)
    {
        Buff buff = instance.BuffData;
        // 遍历所有冲突配置，检查是否还有冲突存在
        foreach (var conflictConfig in buff.ConflictConfigs)
        {
            BuffInstance conflictedInstance = FindBuffInstance(conflictConfig.conflictedBuff);
            if (conflictedInstance != null)
                return false; // 还有冲突存在，不能取消 Mute
        }
        return true; // 所有冲突都已解除
    }
    /// <summary>
    /// 处理两个 Buff 之间的冲突
    /// </summary>
    private void HandleConflict(BuffInstance currentInstance, BuffInstance conflictedInstance, BuffConflictConfig config)
    {
        switch (config.strategies)
        {
            case ConflictStrategies.Replace:
                // 替换：移除冲突的 Buff
                RemoveBuff(conflictedInstance);
                Debug.Log($"[BuffManager] - 冲突替换: 移除 Buff {conflictedInstance.BuffData.name} 因为与 {currentInstance.BuffData.name} 冲突");
                break;

            case ConflictStrategies.OtherMute:
                // 无效化冲突者
                if (!conflictedInstance.IsMuted)
                {
                    conflictedInstance.Mute();
                    Debug.Log($"[BuffManager] - 无效化冲突者: 无效化 Buff {conflictedInstance.BuffData.name} 因为与 {currentInstance.BuffData.name} 冲突");
                }

                break;
            case ConflictStrategies.SelfMute:
                // 无效化自己
                if (!currentInstance.IsMuted)
                {
                    currentInstance.Mute();
                    Debug.Log($"[BuffManager] - 无效化自己: 无效化 Buff {currentInstance.BuffData.name} 因为与 {conflictedInstance.BuffData.name} 冲突");
                }

                break;
            case ConflictStrategies.PriorityMute:
                // 根据优先级决定
                if (config.priority > GetConflictPriority(conflictedInstance.BuffData, currentInstance.BuffData))
                {
                    // 当前 Buff 优先级更高，无效化冲突者
                    if (!conflictedInstance.IsMuted)
                    {
                        conflictedInstance.Mute();
                        Debug.Log($"[BuffManager] - 优先级无效化冲突者: 无效化 Buff {conflictedInstance.BuffData.name} 因为与 {currentInstance.BuffData.name} 冲突");
                    }
                }
                else
                {
                    // 冲突者优先级更高或相等，无效化自己
                    if (!currentInstance.IsMuted)
                    {
                        currentInstance.Mute();
                        Debug.Log($"[BuffManager] - 无效化自己: 无效化 Buff {currentInstance.BuffData.name} 因为与 {conflictedInstance.BuffData.name} 冲突");
                    }
                }
                break;
        }
    }
    /// <summary>
    /// 获取 targetBuff 对 sourceBuff 的冲突优先级
    /// </summary>
    private int GetConflictPriority(Buff targetBuff, Buff sourceBuff)
    {
        foreach (var config in targetBuff.ConflictConfigs)
        {
            if (config.conflictedBuff == sourceBuff)
                return config.priority;
        }
        return 0; // 默认优先级
    }
    #endregion

    #region 生命周期
    /// <summary>
    /// 更新所有活跃的Buff，处理Buff的持续时间和过期移除
    /// </summary>
    public override void Update()
    {
        base.Update();
        List<BuffInstance> toRemove = null;
        if (m_activeBuffs.Count == 0) return;

        for (int i = m_activeBuffs.Count - 1; i >= 0; i--)
        {
            if (i >= m_activeBuffs.Count) continue;
            if (i < 0) break;
            BuffInstance instance = m_activeBuffs[i];
            instance.Update(Time.deltaTime);
            if (instance.IsExpired)
            {
                if (toRemove == null) toRemove = new List<BuffInstance>();
                toRemove.Add(instance);
            }
        }
        if (toRemove != null)
        {
            foreach (var instance in toRemove)
            {
                instance.Remove();
                m_activeBuffs.Remove(instance);
            }
        }
    }
    public override void OnRecycle()
    {
        base.OnRecycle();
        for (int i = m_activeBuffs.Count - 1; i >= 0; i--)
        {
            BuffInstance instance = m_activeBuffs[i];
            instance.Remove();
        }
        m_activeBuffs.Clear();
        m_immuneBuffs.Clear();
    }

    /// <summary>
    /// 获取当前活跃的 Buff 数量
    /// </summary>
    public int GetActiveBufCount()
    {
        return m_activeBuffs.Count;
    }
    #endregion
}

/// <summary>
/// Buff接收器接口，用于定义如何接收和处理Buff效果的对象
/// </summary>
public interface IBuffReceiver
{
    public T GetObjectComponent<T>() where T : ComponentBase;
    public BuffManager BuffManager { get; }
}
//TODO buff系统现在由多个BuffInstance内再由多个BuffEffect来实现，这可能导致重叠，冲突的细分buff情况难以处理;
