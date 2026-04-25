using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingMovement : BulletComponentBase
{
    private EnemyMain m_target;
    private Vector3 m_direction;
    private float m_distance;
    const float STOP_TRACKING_DISTANCE = 0.4f;
    private float m_speed = 25f;
    public void SetTarget(EnemyMain enemy)
    {
        m_target = enemy;
    }
    public void SetSpeed(float speed)
    {
        m_speed = speed;
    }

    public override void Update()
    {
        base.Update();


        //不断往目标的V3靠近
        if (m_target != null)
        {
            Vector3 direction = (m_target.transform.position - m_bullet.transform.position).normalized;

            m_bullet.transform.position += direction * m_speed * Time.deltaTime;
            m_direction = direction;
        }
        else
        {
            m_target = null;
            m_bullet.transform.position += m_direction * m_speed * Time.deltaTime;
            return;
        }

        m_distance = Vector3.Distance(m_bullet.transform.position, m_target.transform.position);

        if (m_distance <= STOP_TRACKING_DISTANCE)
        {
            Debug.Log("[TrackingMovement] - 已到达目标附近，停止追踪");
            m_target = null;
        }
    }
}
