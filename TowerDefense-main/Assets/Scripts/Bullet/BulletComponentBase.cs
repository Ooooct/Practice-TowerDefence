using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletComponentBase : ComponentBase
{
    protected BulletMain m_bullet;
    public void SetBulletMain(BulletMain bullet)
    {
        m_bullet = bullet;
    }

    override public void Start()
    {
        base.Start();
        if (m_bullet == null)
        {
            Debug.LogError("[BulletComponentBase] - BulletMain 未设置！");
        }
    }
}
