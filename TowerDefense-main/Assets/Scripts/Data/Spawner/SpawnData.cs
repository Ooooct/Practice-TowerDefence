using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人生成数据
/// </summary>
[System.Serializable]
public struct EnemySpawnData
{
    public EnemyData enemyData;
    // 每次生成时绑定的路径数据：路径不再作为 EnemyData 的一部分，而由生成数据决定
    public WayPointsData wayPointsData;
    public int SpawnerIndex;
    public int spawnCount;
    public float spawnInterval; // 生成间隔，单位秒
    public float spawnDelay; // 在波次开始后延迟生成的时间（秒）
}

/// <summary>
/// 一波敌人的生成数据
/// </summary>
[System.Serializable]
public struct WaveData
{
    public List<EnemySpawnData> spawnData;
    public int spawnDelay; // 相较于上一波的延迟时间，单位秒
}

/// <summary>
/// 生成配置数据
/// </summary>
[CreateAssetMenu(fileName = "SpawnData", menuName = "Data/SpawnData", order = 1)]
public class SpawnData : ScriptableObject
{
    [Header("JSON 配置")]
    [Tooltip("绑定的 JSON 配置文件（可选）")]
    [SerializeField]
    private TextAsset m_jsonFile;

    [SerializeField]
    private List<WaveData> m_waveData = new List<WaveData>();

    [SerializeField]
    private int m_waitTimeBeforeStart = 0;

    #region 属性
    /// <summary>
    /// 绑定的 JSON 文件
    /// </summary>
    public TextAsset JsonFile => m_jsonFile;
    /// <summary>
    /// 波次数据列表
    /// </summary>
    public List<WaveData> WaveData => m_waveData;

    /// <summary>
    /// 开始前等待时间（秒）
    /// </summary>
    public int WaitTimeBeforeStart => m_waitTimeBeforeStart;
    #endregion
}
