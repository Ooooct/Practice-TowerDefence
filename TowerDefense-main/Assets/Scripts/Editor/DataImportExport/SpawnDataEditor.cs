using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SerializableSpawnData
{
    public List<SerializableWaveData> waveData;
    public int waitTimeBeforeStart;
}

[Serializable]
public class SerializableWaveData
{
    public List<SerializableEnemySpawnData> spawnData;
    public int spawnDelay;
}

[Serializable]
public class SerializableEnemySpawnData
{
    public string enemyDataReference;
    public string wayPointsDataReference;
    public int spawnerIndex;
    public int spawnCount;
    public float spawnInterval;
    public float spawnDelay;
}

[CustomEditor(typeof(SpawnData))]
public class SpawnDataEditor : DataEditorBase<SpawnData>
{
    protected override object CreateSerializableData(SpawnData data)
    {
        var serializableData = new SerializableSpawnData
        {
            waveData = new List<SerializableWaveData>(),
            waitTimeBeforeStart = data.WaitTimeBeforeStart
        };

        foreach (var wave in data.WaveData)
        {
            var serializableWave = new SerializableWaveData
            {
                spawnData = new List<SerializableEnemySpawnData>(),
                spawnDelay = wave.spawnDelay
            };

            foreach (var enemySpawn in wave.spawnData)
            {
                string enemyReference = GetAssetReference(enemySpawn.enemyData);
                var serializableEnemySpawn = new SerializableEnemySpawnData
                {
                    enemyDataReference = enemyReference,
                    wayPointsDataReference = GetAssetReference(enemySpawn.wayPointsData),
                    spawnerIndex = enemySpawn.SpawnerIndex,
                    spawnCount = enemySpawn.spawnCount,
                    spawnInterval = enemySpawn.spawnInterval,
                    spawnDelay = enemySpawn.spawnDelay
                };

                serializableWave.spawnData.Add(serializableEnemySpawn);
            }

            serializableData.waveData.Add(serializableWave);
        }

        return serializableData;
    }

    protected override void ApplySerializableData(SpawnData target, object deserializedData)
    {
        var data = deserializedData as SerializableSpawnData;
        if (data == null)
        {
            Debug.LogError("[SpawnDataEditor] 反序列化数据类型不匹配");
            return;
        }

        var waitTimeField = typeof(SpawnData).GetField("m_waitTimeBeforeStart",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        waitTimeField?.SetValue(target, data.waitTimeBeforeStart);

        var waveDataField = typeof(SpawnData).GetField("m_waveData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (waveDataField == null)
        {
            Debug.LogError("[SpawnDataEditor] 无法找到 m_waveData 字段");
            return;
        }

        var waveDataList = new List<WaveData>();

        foreach (var serializableWave in data.waveData)
        {
            var wave = new WaveData
            {
                spawnData = new List<EnemySpawnData>(),
                spawnDelay = serializableWave.spawnDelay
            };

            foreach (var serializableEnemySpawn in serializableWave.spawnData)
            {
                EnemyData enemyData = null;
                if (!string.IsNullOrEmpty(serializableEnemySpawn.enemyDataReference))
                {
                    enemyData = LoadAssetReference<EnemyData>(serializableEnemySpawn.enemyDataReference);
                }

                WayPointsData wayPoints = null;
                if (!string.IsNullOrEmpty(serializableEnemySpawn.wayPointsDataReference))
                {
                    wayPoints = LoadAssetReference<WayPointsData>(serializableEnemySpawn.wayPointsDataReference);
                }

                var enemySpawn = new EnemySpawnData
                {
                    enemyData = enemyData,
                    wayPointsData = wayPoints,
                    SpawnerIndex = serializableEnemySpawn.spawnerIndex,
                    spawnCount = serializableEnemySpawn.spawnCount,
                    spawnInterval = serializableEnemySpawn.spawnInterval,
                    spawnDelay = serializableEnemySpawn.spawnDelay
                };

                wave.spawnData.Add(enemySpawn);
            }

            waveDataList.Add(wave);
        }

        waveDataField.SetValue(target, waveDataList);
    }

    protected override Type GetSerializableDataType()
    {
        return typeof(SerializableSpawnData);
    }
}
