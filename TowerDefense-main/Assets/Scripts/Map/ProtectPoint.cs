using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ProtectPoint : MonoBehaviour
{
    public static ProtectPoint Instance { get; private set; }
    public int health = 20;
    public Action<int> OnHealthChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 检查碰撞的对象是否是敌人
        EnemyMain enemy = other.GetComponent<EnemyMain>();
        if (enemy != null)
        {
            Debug.Log("[ProtectPoint] - 敌人到达保护点: " + enemy.name);

            // 回收敌人对象
            PoolManager.Instance.Recycle(enemy);
            health--;
            OnHealthChanged?.Invoke(health);
            if (health <= 0)
                WinAndLoseUI.Instance.Lose();
        }
    }
}