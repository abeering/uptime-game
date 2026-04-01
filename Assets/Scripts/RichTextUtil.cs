using UnityEngine;

public static class RichTextUtil
{
    public static string Colorize(string text, Color color, bool bold = false)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(color);
        string content = bold ? $"<b>{text}</b>" : text;
        return $"<color=#{hex}>{content}</color>";
    }
}