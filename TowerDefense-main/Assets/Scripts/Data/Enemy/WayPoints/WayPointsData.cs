using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WayPointsData", menuName = "Data/Enemy/WayPointsData", order = 1)]
public class WayPointsData : ScriptableObject
{
    [Header("JSON 配置")]
    [Tooltip("绑定的 JSON 配置文件（可选）")]
    [SerializeField] private TextAsset m_jsonFile;

    public TextAsset JsonFile => m_jsonFile;

    public static readonly Vector3 END_POINT = new Vector3(-19, 0.25f, 2.5f);

    [SerializeField]
    public List<Vector3> wayPoints = new List<Vector3>();

    [System.NonSerialized]
    public bool isEditingWayPoints = false;

    [System.NonSerialized]
    public List<Vector3> tempWayPoints = new List<Vector3>();
}
