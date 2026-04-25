using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerComponentBase : ComponentBase
{
    protected TowerMain m_tower;
    public void SetTowerMain(TowerMain tower)
    {
        m_tower = tower;
    }

    override public void Start()
    {
        base.Start();
        if (m_tower == null)
        {
            Debug.LogError("[TowerComponentBase] - TowerMain 未设置！");
        }

    }

}
