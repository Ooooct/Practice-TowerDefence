using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 随机数控制器
/// 使用受控的伪随机数生成器，支持种子设置以实现确定性随机
/// 用于游戏回放和测试场景中确保结果一致性
/// </summary>
public class RandomController : MonoBehaviour
{
    #region 单例实现
    private static RandomController m_instance;

    public static RandomController Instance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("RandomController");
                m_instance = obj.AddComponent<RandomController>();
            }
            return m_instance;
        }
    }
    #endregion

    #region 变量
    private System.Random m_random;
    private int m_currentSeed;
    #endregion

    #region 属性
    /// <summary>
    /// 获取当前种子
    /// </summary>
    public int CurrentSeed => m_currentSeed;
    #endregion

    #region Unity 生命周期
    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            InitializeRandom();
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化随机数生成器，使用系统时间作为默认种子
    /// </summary>
    private void InitializeRandom()
    {
        int defaultSeed = (int)DateTime.Now.Ticks;
        SetSeed(defaultSeed);
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 设置随机数生成器的种子
    /// 设置相同的种子将生成相同序列的随机数
    /// </summary>
    /// <param name="seed">种子值</param>
    public void SetSeed(int seed)
    {
        m_currentSeed = seed;
        m_random = new System.Random(seed);
    }

    /// <summary>
    /// 返回范围内的随机整数 [min, max)
    /// </summary>
    /// <param name="min">最小值（包含）</param>
    /// <param name="max">最大值（不包含）</param>
    /// <returns>随机整数</returns>
    public int Next(int min, int max)
    {
        if (m_random == null)
        {
            Debug.LogError("[RandomController] 随机数生成器未初始化");
            return min;
        }

        if (min >= max)
        {
            Debug.LogWarning($"[RandomController] Next 参数无效: min={min}, max={max}");
            return min;
        }

        int result = m_random.Next(min, max);
        return result;
    }

    /// <summary>
    /// 返回范围内的随机整数 [0, max)
    /// </summary>
    /// <param name="max">最大值（不包含）</param>
    /// <returns>随机整数</returns>
    public int Next(int max)
    {
        return Next(0, max);
    }

    /// <summary>
    /// 返回非负的随机整数
    /// </summary>
    /// <returns>随机整数</returns>
    public int Next()
    {
        if (m_random == null)
        {
            Debug.LogError("[RandomController] 随机数生成器未初始化");
            return 0;
        }
        return m_random.Next();
    }

    /// <summary>
    /// 返回范围内的随机浮点数 [min, max)
    /// </summary>
    /// <param name="min">最小值（包含）</param>
    /// <param name="max">最大值（不包含）</param>
    /// <returns>随机浮点数</returns>
    public float Range(float min, float max)
    {
        if (m_random == null)
        {
            Debug.LogError("[RandomController] 随机数生成器未初始化");
            return min;
        }

        if (min >= max)
        {
            Debug.LogWarning($"[RandomController] Range 参数无效: min={min}, max={max}");
            return min;
        }

        // 使用 NextDouble 生成 [0, 1) 的随机数
        double randomDouble = m_random.NextDouble();
        float result = min + (float)randomDouble * (max - min);
        return result;
    }

    /// <summary>
    /// 返回 [0, 1) 范围内的随机浮点数
    /// </summary>
    /// <returns>随机浮点数</returns>
    public float Value()
    {
        if (m_random == null)
        {
            Debug.LogError("[RandomController] 随机数生成器未初始化");
            return 0f;
        }
        return (float)m_random.NextDouble();
    }

    /// <summary>
    /// 返回指定范围内的随机整数 [min, max]（包含两端）
    /// </summary>
    /// <param name="min">最小值（包含）</param>
    /// <param name="max">最大值（包含）</param>
    /// <returns>随机整数</returns>
    public int RandomInt(int min, int max)
    {
        // Next(min, max + 1) 返回 [min, max + 1)，即 [min, max]
        return Next(min, max + 1);
    }

    /// <summary>
    /// 从数组中随机选择一个元素
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <param name="array">数组</param>
    /// <returns>随机选择的元素，如果数组为空则返回默认值</returns>
    public T RandomElement<T>(T[] array)
    {
        if (array == null || array.Length == 0)
        {
            Debug.LogWarning("[RandomController] 尝试从空数组中随机选择元素");
            return default(T);
        }

        int randomIndex = Next(array.Length);
        return array[randomIndex];
    }

    /// <summary>
    /// 从列表中随机选择一个元素
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    /// <param name="list">列表</param>
    /// <returns>随机选择的元素，如果列表为空则返回默认值</returns>
    public T RandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("[RandomController] 尝试从空列表中随机选择元素");
            return default(T);
        }

        int randomIndex = Next(list.Count);
        return list[randomIndex];
    }

    /// <summary>
    /// 打乱数组的顺序（Fisher-Yates 算法）
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <param name="array">要打乱的数组</param>
    public void Shuffle<T>(T[] array)
    {
        if (array == null || array.Length == 0)
        {
            return;
        }

        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Next(i + 1);
            // 交换
            T temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 打乱列表的顺序（Fisher-Yates 算法）
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    /// <param name="list">要打乱的列表</param>
    public void Shuffle<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            return;
        }

        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Next(i + 1);
            // 交换
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    #endregion
}
