using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Modifier
{
    public float addValue;
    public float percentageAddValue;
    public float multiplyValue;
    public Modifier(float addValue = 0f, float percentageAddValue = 0f, float multiplyValue = 1f)
    {
        this.addValue = addValue;
        this.percentageAddValue = percentageAddValue;
        this.multiplyValue = multiplyValue;
    }
}
