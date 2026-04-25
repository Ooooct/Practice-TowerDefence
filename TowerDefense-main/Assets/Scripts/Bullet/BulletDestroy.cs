using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletDestroy : BulletComponentBase
{
    private float m_lifetime = 10f;
    private float m_timer = 0f;

    public override void Awake()
    {
        base.Awake();
        m_timer = 0f;
    }
    public override void Update()
    {
        base.Update();
        m_timer += Time.deltaTime;
        if (m_timer >= m_lifetime)
        {
            if (m_bullet != null)
            {
                PoolManager.Instance.Recycle(m_bullet);
            }
        }
    }
}
