using UnityEngine;

[System.Serializable]
public class VideoCallData
{
    public string CallerName;

    [TextArea(2, 8)]
    public string Body;

    public Sprite Portrait;
}