using UnityEngine;

[System.Serializable]
public class AudioCue
{
    public string cueName;
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 1.5f)] public float pitchMin = 1f;
    [Range(0.5f, 1.5f)] public float pitchMax = 1f;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    public float GetRandomPitch()
    {
        return Random.Range(pitchMin, pitchMax);
    }
}