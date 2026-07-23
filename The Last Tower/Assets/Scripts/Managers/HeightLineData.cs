using UnityEngine;

[System.Serializable]
public class HeightLineData
{
    [Header("Line Settings")]
    // 高度线名称
    // 高さライン名
    public string lineName;

    // 高度线物体
    // 高さラインのオブジェクト
    public GameObject lineObject;

    // 是否只触发一次
    // 1回だけ発動するか
    public bool triggerOnlyOnce = true;

    [Header("Runtime State")]
    // 是否已经触发
    // すでに発動したか
    [HideInInspector]
    public bool hasTriggered = false;
}