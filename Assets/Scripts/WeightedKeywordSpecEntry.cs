using UnityEngine;

[System.Serializable]
public readonly struct WeightedKeywordSpecEntry
{
    public readonly string spec;
    public readonly float weight;

    public WeightedKeywordSpecEntry(string spec, float weight)
    {
        this.spec = spec;
        this.weight = Mathf.Max(0f, weight);
    }
}