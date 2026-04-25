using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池类，管理单一类型的对象复用。
/// </summary>
/// <typeparam name="T">需要池化的对象类型，必须是实现了IPoolable接口的组件。</typeparam>
public class ObjectPool<T> where T : Component, IPoolable
{
    #region 变量
    private readonly T m_prefab;
    private readonly Transform m_parent;
    private readonly bool m_autoExpand;
    private readonly Queue<T> m_availableObjects = new Queue<T>();

    // 回收对象的安全位置，避免与场景中其他物体碰撞
    private static readonly Vector3 RECYCLE_POSITION = new Vector3(0, 100, 0);
    #endregion

    #region 构造函数
    /// <summary>
    /// 创建一个新的对象池。
    /// </summary>
    /// <param name="prefab">对象预制件。</param>
    /// <param name="parent">对象池的父级Transform。</param>
    /// <param name="initialSize">初始池大小。</param>
    /// <param name="autoExpand">当池为空时是否自动扩展。</param>
    public ObjectPool(T prefab, Transform parent, int initialSize, bool autoExpand)
    {
        m_prefab = prefab;
        m_parent = parent;
        m_autoExpand = autoExpand;

        // 预创建对象以填充对象池
        for (int i = 0; i < initialSize; i++)
        {
            CreateAndPoolObject();
        }
    }
    #endregion

    #region public方法
    /// <summary>
    /// 从对象池中获取一个对象。
    /// </summary>
    /// <param name="position">生成位置。</param>
    /// <param name="rotation">生成旋转。</param>
    /// <returns>获取到的对象实例。</returns>
    public T Spawn(Vector3 position, Quaternion rotation)
    {
        T instance;
        if (m_availableObjects.Count > 0)
        {
            instance = m_availableObjects.Dequeue();
        }
        else if (m_autoExpand)
        {
            instance = CreateObject();
            Debug.LogWarning($"[ObjectPool] - 池中已无可用对象，自动扩展并创建新实例: {m_prefab.name}");
        }
        else
        {
            Debug.LogError($"[ObjectPool] - 池中已无可用对象，且不允许扩展: {m_prefab.name}");
            return null;
        }

        // 设置对象的位置和旋转，并激活
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.GameObject.SetActive(true);

        // 调用对象的出池回调
        instance.OnSpawn();

        return instance;
    }

    /// <summary>
    /// 将对象返回到对象池。
    /// </summary>
    /// <param name="instance">要回收的对象实例。</param>
    public void Recycle(T instance)
    {
        if (instance == null)
        {
            Debug.LogWarning("[ObjectPool] - 尝试回收一个空对象。");
            return;
        }

        // 调用对象的入池回调
        instance.OnRecycle();

        // 禁用对象
        instance.GameObject.SetActive(false);

        // 移动到安全位置
        instance.transform.position = RECYCLE_POSITION;
        instance.transform.rotation = Quaternion.identity;

        // 将对象放回队列中
        m_availableObjects.Enqueue(instance);
    }
    #endregion

    #region private方法
    /// <summary>
    /// 创建一个新对象实例。
    /// </summary>
    private T CreateObject()
    {
        return Object.Instantiate(m_prefab, m_parent);
    }

    /// <summary>
    /// 创建一个新对象并立即将其回收到池中。
    /// </summary>
    private void CreateAndPoolObject()
    {
        T instance = CreateObject();

        // 设置到安全位置
        instance.transform.position = RECYCLE_POSITION;
        instance.transform.rotation = Quaternion.identity;

        instance.GameObject.SetActive(false);
        m_availableObjects.Enqueue(instance);
    }
    #endregion
}
