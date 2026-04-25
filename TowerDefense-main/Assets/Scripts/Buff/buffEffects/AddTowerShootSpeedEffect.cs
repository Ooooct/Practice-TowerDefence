using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AddTowerShootSpeedEffect", menuName = "Buffs/Effects/AddTowerShootSpeedEffect")]
public class AddTowerShootSpeedEffect : BuffEffectBase
{
    [SerializeField] public Modifier modifier = new Modifier(0, 0, 1);

    public override void OnBuffApply(BuffInstance owner)
    {
        // 获取塔的子弹发射器并添加修改器
        BulletShooter bulletShooter = GetBulletShooter(owner);
        if (bulletShooter != null)
        {
            bulletShooter.ModifiableShootCoolDown.AddModifier(modifier);
        }
    }

    public override void OnBuffRemove(BuffInstance owner)
    {
        // 获取塔的子弹发射器并移除修改器
        BulletShooter bulletShooter = GetBulletShooter(owner);
        if (bulletShooter != null)
        {
            bool removed = bulletShooter.ModifiableShootCoolDown.RemoveModifier(modifier);

            if (!removed)
            {
                Debug.LogWarning($"[AddTowerShootSpeedBuff] 移除修改器失败！" +
                    $"修改器值: addValue={modifier.addValue}, " +
                    $"percentageAddValue={modifier.percentageAddValue}, " +
                    $"multiplyValue={modifier.multiplyValue}. " +
                    $"该修改器可能未被添加或已被其他事件移除。");
            }
        }
    }

    /// <summary>
    /// 获取塔的子弹发射器组件
    /// </summary>
    private BulletShooter GetBulletShooter(BuffInstance owner)
    {
        if (owner?.RootOwner == null)
        {
            Debug.LogWarning("[AddTowerShootSpeedBuff] - BuffInstance owner 或 RootOwner 为空");
            return null;
        }

        return owner.RootOwner.GetObjectComponent<BulletShooter>();
    }
}
