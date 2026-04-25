using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveSpeedModifyEffect", menuName = "Buffs/Effects/MoveSpeedModifyEffect")]
public class MoveSpeedModifyEffect : BuffEffectBase
{
    [SerializeField] private Modifier m_modifier = new Modifier(0, 0, 1);
    //TODO 存在远距离链式调用
    public override void OnBuffApply(BuffInstance owner)
    {
        // 获取敌人移动组件并添加修改器
        EnemyMovement enemyMovement = GetEnemyMovement(owner);
        if (enemyMovement != null)
        {
            enemyMovement.ModifiableMoveSpeed.AddModifier(m_modifier);
        }
    }

    public override void OnBuffRemove(BuffInstance owner)
    {
        // 获取敌人移动组件并移除修改器
        EnemyMovement enemyMovement = GetEnemyMovement(owner);
        if (enemyMovement.ModifiableMoveSpeed != null)
        {
            bool removed = enemyMovement.ModifiableMoveSpeed.RemoveModifier(m_modifier);

            if (!removed)
            {
                Debug.LogWarning($"[MoveSpeedModifyBuff] 移除修改器失败！" +
                    $"修改器值: addValue={m_modifier.addValue}, " +
                    $"percentageAddValue={m_modifier.percentageAddValue}, " +
                    $"multiplyValue={m_modifier.multiplyValue}. " +
                    $"该修改器可能未被添加或已被其他事件移除，例如死亡。");
            }
        }
    }    /// <summary>
         /// 获取敌人移动组件
         /// </summary>
    private EnemyMovement GetEnemyMovement(BuffInstance owner)
    {
        if (owner?.RootOwner == null)
        {
            Debug.LogWarning("[MoveSpeedModifyBuff] - BuffInstance owner 或 RootOwner 为空");
            return null;
        }

        return owner.RootOwner.GetObjectComponent<EnemyMovement>();
    }
}
