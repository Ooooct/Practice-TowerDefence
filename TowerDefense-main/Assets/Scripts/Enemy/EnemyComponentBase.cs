using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyComponentBase : ComponentBase
{
    protected EnemyMain m_enemy;
    public void SetEnemyMain(EnemyMain enemy)
    {
        m_enemy = enemy;
    }

    override public void Start()
    {
        base.Start();
        if (m_enemy == null)
        {
            Debug.LogError("[EnemyComponentBase] - EnemyMain 未设置！");
        }
    }
}
