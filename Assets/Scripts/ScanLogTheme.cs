using System;
using UnityEngine;

[Serializable]
public class ScanLogTheme
{
    [Header("Scan Stages")]
    public Color stageUnknown = new(0.53f, 0.53f, 0.53f, 1f);
    public Color stageProbable = new(1.00f, 0.82f, 0.40f, 1f);
    public Color stageLikely = new(0.49f, 1.00f, 0.42f, 1f);
    public Color stageConfirmed = new(0.40f, 0.80f, 1.00f, 1f);

    [Header("Visible Classes")]
    public Color classUnknown = new(0.53f, 0.53f, 0.53f, 1f);
    public Color classBenign = new(0.72f, 1.00f, 0.72f, 1f);
    public Color classThreat = new(1.00f, 0.42f, 0.42f, 1f);
    public Color classPriority = new(0.40f, 0.80f, 1.00f, 1f);

    [Header("Scan Slots")]
    public Color slot1 = new(0.25f, 1.00f, 0.25f, 1f);
    public Color slot2 = new(0.30f, 0.85f, 1.00f, 1f);
    public Color slot3 = new(1.00f, 0.70f, 0.30f, 1f);
    public Color slot4 = new(1.00f, 0.45f, 0.80f, 1f);

    [Header("Misc")]
    public Color muted = new(0.67f, 0.67f, 0.67f, 0.53f);
}