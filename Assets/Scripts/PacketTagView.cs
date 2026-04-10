using TMPro;
using UnityEngine;

public class PacketTagView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private TextMeshPro label;

    public void SetTag(string text, Color backgroundColor, Color textColor)
    {
        if (label != null)
        {
            label.text = text;
            label.color = textColor;
        }

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = backgroundColor;
            backgroundRenderer.enabled = true;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetSortOrder(int backgroundOrder, int textOrder)
    {
        if (backgroundRenderer != null)
            backgroundRenderer.sortingOrder = backgroundOrder;

        if (label != null)
            label.sortingOrder = textOrder;
    }
}