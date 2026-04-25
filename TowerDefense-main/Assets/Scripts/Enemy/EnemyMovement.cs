using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 敌人移动组件，负责使用NavMeshAgent沿路径点移动
public class EnemyMovement : EnemyComponentBase
{
    // 变量
    private List<Vector3> m_waypoints = new List<Vector3>();
    private WayPointsData m_wayPointsData;
    private NavMeshAgent m_agent;
    private int m_currentWaypointIndex = 0;
    private float m_distanceToFinalTarget = 0f;
    private bool m_isMoving = false;
    private ModifiableFloat m_ModifiableMoveSpeed = new ModifiableFloat(0);

    private const float WAYPOINT_REACH_DISTANCE = 2f;
    private const float DESTINATION_RANDOM_OFFSET_RANGE = 2f;
    private const float STUCK_SPEED_THRESHOLD = 0.1f;
    private const float STUCK_TIME_THRESHOLD = 3f;
    private const float NAVMESH_SAMPLE_RADIUS = 1.5f;

    private Vector3 m_currentDestination = Vector3.zero;
    private Vector3 m_lastPosition = Vector3.zero;
    private float m_stuckTimer = 0f;
    // 变量结束

    // 属性
    public List<Vector3> Waypoints
    {
        get => m_waypoints;
        set => m_waypoints = value;
    }

    public WayPointsData WayPointsData
    {
        get => m_wayPointsData;
        set
        {
            m_wayPointsData = value;
            m_waypoints = m_wayPointsData.wayPoints;
        }
    }

    // 距离最终目标点的距离
    public float DistanceToFinalTarget => m_distanceToFinalTarget;

    // 是否正在移动
    public bool IsMoving => m_isMoving;

    // NavMeshAgent组件
    public NavMeshAgent Agent => m_agent;

    // 移动速度
    public float MoveSpeed
    {
        get => m_ModifiableMoveSpeed.BaseValue;
        set
        {
            m_ModifiableMoveSpeed.BaseValue = value;
            if (m_agent != null)
            {
                m_agent.speed = m_ModifiableMoveSpeed.Value;
            }
        }
    }

    public ModifiableFloat ModifiableMoveSpeed
    {
        get { return m_ModifiableMoveSpeed; }
        set
        {
            // 取消旧的订阅
            if (m_ModifiableMoveSpeed.OnValueChanged != null)
            {
                m_ModifiableMoveSpeed.OnValueChanged -= OnMoveSpeedChanged;
            }

            m_ModifiableMoveSpeed = value;

            // 订阅新的值变化事件
            m_ModifiableMoveSpeed.OnValueChanged += OnMoveSpeedChanged;

            // 立即应用
            OnMoveSpeedChanged(m_ModifiableMoveSpeed.Value);
        }
    }
    // 属性结束

    // Unity生命周期
    public override void Awake()
    {
        base.Awake();

        InitializeNavMeshAgent();
        m_ModifiableMoveSpeed.OnValueChanged += OnMoveSpeedChanged;
        m_agent.speed = m_ModifiableMoveSpeed.Value;
        if ((m_waypoints != null && m_waypoints.Count > 0) || m_wayPointsData != null)
            StartMovement();
    }

    public override void Start()
    {
        base.Start();
    }
    public override void Update()
    {
        if (!m_isMoving || m_waypoints == null || m_waypoints.Count == 0)
        {
            return;
        }

        CheckWaypointReached();
        UpdateRotationTowardsVelocity();
        HandlePotentialStuck();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        UpdateDistanceToFinalTarget();
    }

    public override void OnRecycle()
    {
        base.OnRecycle();

        // 取消订阅
        m_ModifiableMoveSpeed.OnValueChanged -= OnMoveSpeedChanged;

        StopMovement();
        m_currentWaypointIndex = 0;
        m_currentDestination = Vector3.zero;
        m_lastPosition = Vector3.zero;
        m_stuckTimer = 0f;
    }
    // 生命周期结束

    // public方法
    public void SetWaypoints(List<Vector3> waypoints)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning("[EnemyMovement] - 路径点列表为空或无效");
            return;
        }

        m_waypoints = waypoints;
        m_currentWaypointIndex = 0;
        StartMovement();
    }

    public void SetWaypoints(WayPointsData data)
    {
        if (data == null || data.wayPoints == null || data.wayPoints.Count == 0)
        {
            Debug.LogWarning("[EnemyMovement] - 路径点数据为空或无效");
            return;
        }

        m_wayPointsData = data;
        SetWaypoints(data.wayPoints);
    }

    // 开始移动
    public void StartMovement()
    {
        // 先检查 m_wayPointsData 是否为 null
        if (m_wayPointsData != null)
        {
            m_waypoints = m_wayPointsData.wayPoints;
        }

        // 验证路径点列表
        if (m_waypoints == null || m_waypoints.Count == 0)
        {
            Debug.LogWarning("[EnemyMovement] - 无法开始移动，路径点列表为空");
            return;
        }

        m_isMoving = true;
        MoveToCurrentWaypoint();
    }

    // 停止移动
    public void StopMovement()
    {
        m_isMoving = false;
        if (m_agent != null && m_agent.enabled)
        {
            m_agent.isStopped = true;
        }
    }

    // 恢复移动
    public void ResumeMovement()
    {
        if (m_agent != null && m_agent.enabled)
        {
            m_isMoving = true;
            m_agent.isStopped = false;
        }
    }
    // public方法结束

    // private方法
    private void UpdateRotationTowardsVelocity()
    {
        if (m_agent == null || !m_agent.enabled)
        {
            return;
        }

        Vector3 velocity = m_agent.velocity;
        if (velocity.sqrMagnitude < 0.01f) return;
        float angle = Mathf.Atan2(-velocity.z, velocity.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, angle + 90f, 0);
        m_enemy.transform.rotation = Quaternion.Lerp(m_enemy.transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    // 移动速度变化时的回调
    private void OnMoveSpeedChanged(float newSpeed)
    {
        if (m_agent != null && m_agent.enabled)
        {
            m_agent.speed = newSpeed;
            Debug.Log($"[EnemyMovement] - 移动速度已更新: {newSpeed}");
        }
    }

    // 初始化NavMeshAgent组件
    private void InitializeNavMeshAgent()
    {
        if (m_enemy == null)
        {
            Debug.LogError("[EnemyMovement] - EnemyMain引用为空");
            return;
        }

        m_agent = m_enemy.GetComponent<NavMeshAgent>();
        if (m_agent == null)
        {
            Debug.LogError("[EnemyMovement] - 未找到NavMeshAgent组件");
            return;
        }

        m_agent.updateRotation = false;
        m_agent.updateUpAxis = false;
        m_agent.speed = m_ModifiableMoveSpeed.Value;
        m_agent.stoppingDistance = 0f;
        m_agent.radius = 0.5f;
        m_agent.avoidancePriority = RandomController.Instance.RandomInt(40, 60);
    }

    // 移动到当前路径点
    private void MoveToCurrentWaypoint()
    {
        if (m_waypoints == null || m_currentWaypointIndex >= m_waypoints.Count)
        {
            OnReachedFinalDestination();
            return;
        }

        Vector3 baseWaypoint = m_waypoints[m_currentWaypointIndex];
        bool allowRandomOffset = m_currentWaypointIndex < m_waypoints.Count - 1;

        m_currentDestination = GetDestinationForWaypoint(baseWaypoint, allowRandomOffset);
        m_lastPosition = m_enemy.transform.position;
        m_stuckTimer = 0f;

        if (m_agent != null && m_agent.enabled)
        {
            if (!m_agent.SetDestination(m_currentDestination))
            {
                Debug.LogWarning($"[EnemyMovement] - 无法为 {m_enemy?.name ?? "Enemy"} 设置路径点 {m_currentWaypointIndex} 的目标");
            }
        }
        else
        {
            Debug.LogWarning("[EnemyMovement] - NavMeshAgent 不可用，无法设置目标点");
        }
    }

    // 检查是否到达当前路径点
    private void CheckWaypointReached()
    {
        if (m_agent == null || !m_agent.enabled || m_currentWaypointIndex >= m_waypoints.Count)
        {
            return;
        }

        Vector3 targetWaypoint = m_waypoints[m_currentWaypointIndex];
        float distanceToCurrentDestination = Vector3.Distance(m_enemy.transform.position, m_currentDestination);
        float distanceToBaseWaypoint = Vector3.Distance(m_enemy.transform.position, targetWaypoint);

        // 当距离路径点小于等于检测半径时，视为到达
        if (distanceToCurrentDestination <= WAYPOINT_REACH_DISTANCE || distanceToBaseWaypoint <= WAYPOINT_REACH_DISTANCE)
        {
            AdvanceToNextWaypoint();
        }
        else if (!m_agent.pathPending && m_agent.remainingDistance <= WAYPOINT_REACH_DISTANCE)
        {
            AdvanceToNextWaypoint();
        }
    }

    // 更新到最终目标点的距离
    public void UpdateDistanceToFinalTarget()
    {
        // 简化的距离计算算法
        if (m_agent == null || !m_agent.enabled)
        {
            m_distanceToFinalTarget = float.MaxValue;
            return;
        }

        // 直接使用 NavMeshAgent 的 remainingDistance 属性
        // 它已经计算了沿着路径到目标的距离
        if (m_agent.hasPath && !m_agent.pathPending)
        {
            m_distanceToFinalTarget = m_agent.remainingDistance;
        }
        else
        {
            m_distanceToFinalTarget = float.MaxValue;
        }
    }

    // 到达最终目的地时调用
    private void OnReachedFinalDestination()
    {
        m_isMoving = false;
        m_distanceToFinalTarget = 0f;
    }

    // 根据基础路径点生成实际目标点
    private Vector3 GetDestinationForWaypoint(Vector3 baseWaypoint, bool allowRandomOffset)
    {
        if (allowRandomOffset)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                Vector3 offset = new Vector3(
                    RandomController.Instance.Range(-DESTINATION_RANDOM_OFFSET_RANGE, DESTINATION_RANDOM_OFFSET_RANGE),
                    0f,
                    RandomController.Instance.Range(-DESTINATION_RANDOM_OFFSET_RANGE, DESTINATION_RANDOM_OFFSET_RANGE)
                );

                Vector3 candidate = baseWaypoint + offset;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NAVMESH_SAMPLE_RADIUS, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }
        }

        if (NavMesh.SamplePosition(baseWaypoint, out NavMeshHit baseHit, NAVMESH_SAMPLE_RADIUS, NavMesh.AllAreas))
        {
            return baseHit.position;
        }

        return baseWaypoint;
    }

    private void AdvanceToNextWaypoint()
    {
        m_currentWaypointIndex++;
        MoveToCurrentWaypoint();
    }

    // 处理敌人在拥挤情况下可能卡住的问题
    private void HandlePotentialStuck()
    {
        if (!m_isMoving || m_agent == null || !m_agent.enabled || m_agent.pathPending)
        {
            m_stuckTimer = 0f;
            return;
        }

        // 如果即将到达路径点，不需要检测卡住
        if (m_agent.remainingDistance <= WAYPOINT_REACH_DISTANCE)
        {
            m_stuckTimer = 0f;
            return;
        }

        // 检测实际位置变化而不是速度
        float movedDistance = Vector3.Distance(m_enemy.transform.position, m_lastPosition);

        // 如果移动距离很小，说明可能卡住了
        if (movedDistance < STUCK_SPEED_THRESHOLD * Time.deltaTime)
        {
            m_stuckTimer += Time.deltaTime;

            // 等待足够长时间后才判定为卡住
            if (m_stuckTimer >= STUCK_TIME_THRESHOLD)
            {
                // 检查是否接近当前路径点
                if (m_currentWaypointIndex < m_waypoints.Count)
                {
                    float distanceToWaypoint = Vector3.Distance(m_enemy.transform.position, m_waypoints[m_currentWaypointIndex]);

                    // 如果已经很接近路径点，直接前进到下一个
                    if (distanceToWaypoint <= WAYPOINT_REACH_DISTANCE * 2f)
                    {
                        AdvanceToNextWaypoint();
                    }
                    else
                    {
                        // 否则尝试重新计算路径，但不添加随机偏移避免闪现
                        Vector3 waypoint = m_waypoints[m_currentWaypointIndex];
                        if (NavMesh.SamplePosition(waypoint, out NavMeshHit hit, NAVMESH_SAMPLE_RADIUS * 2f, NavMesh.AllAreas))
                        {
                            m_agent.SetDestination(hit.position);
                        }
                    }
                }

                m_stuckTimer = 0f;
            }
        }
        else
        {
            // 有正常移动，重置计时器
            m_stuckTimer = 0f;
        }

        // 更新上次位置
        m_lastPosition = m_enemy.transform.position;
    }
    // private方法结束
}

