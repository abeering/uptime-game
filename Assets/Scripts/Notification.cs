using UnityEngine;

[System.Serializable]
public class Notification
{
    public string SpeakerName;
    [TextArea(2, 4)]
    public string Body;
    public Sprite Avatar;
}