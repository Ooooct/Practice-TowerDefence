using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 资产类别枚举，用于强类型访问。
/// 注意：此处的枚举成员名称应与 ResourcesDatabase 中定义的类别键（key）完全匹配。
/// </summary>
public enum CategoriesEnum
{
    Towers, WayPoints, Enemy, Buff, HitStrategy, ViewPrefab
}
