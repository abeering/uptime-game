using UnityEngine;

public class PopupClampRegion : MonoBehaviour
{
    [Header("Allowed Regions")]
    public RectTransform[] allowedRegions;

    [Header("Padding (world units)")]
    public Vector2 padding = Vector2.zero;

    public bool TryGetBestClampedWorldPosition(
        RectTransform popup,
        Vector3 desiredWorldPosition,
        out Vector3 clampedWorldPosition)
    {
        clampedWorldPosition = desiredWorldPosition;

        if (popup == null || allowedRegions == null || allowedRegions.Length == 0)
            return false;

        bool foundCandidate = false;
        float bestScore = float.PositiveInfinity;
        Vector3 bestWorldPosition = desiredWorldPosition;

        for (int i = 0; i < allowedRegions.Length; i++)
        {
            RectTransform region = allowedRegions[i];
            if (region == null)
                continue;

            Vector3 candidate = ClampWorldPositionToRegion(popup, region, desiredWorldPosition);
            float score = (candidate - desiredWorldPosition).sqrMagnitude;

            if (!foundCandidate || score < bestScore)
            {
                foundCandidate = true;
                bestScore = score;
                bestWorldPosition = candidate;
            }
        }

        if (!foundCandidate)
            return false;

        clampedWorldPosition = bestWorldPosition;
        return true;
    }

    private Vector3 ClampWorldPositionToRegion(
        RectTransform popup,
        RectTransform region,
        Vector3 desiredWorldPosition)
    {
        Vector3[] regionCorners = new Vector3[4];
        region.GetWorldCorners(regionCorners);

        Vector3 minBounds = regionCorners[0];
        Vector3 maxBounds = regionCorners[2];

        Rect popupRect = popup.rect;
        Vector2 popupPivot = popup.pivot;
        Vector3 popupScale = popup.lossyScale;

        float popupWidthWorld = popupRect.width * popupScale.x;
        float popupHeightWorld = popupRect.height * popupScale.y;

        float leftExtent = popupWidthWorld * popupPivot.x;
        float rightExtent = popupWidthWorld * (1f - popupPivot.x);
        float bottomExtent = popupHeightWorld * popupPivot.y;
        float topExtent = popupHeightWorld * (1f - popupPivot.y);

        float minX = minBounds.x + leftExtent + padding.x;
        float maxX = maxBounds.x - rightExtent - padding.x;
        float minY = minBounds.y + bottomExtent + padding.y;
        float maxY = maxBounds.y - topExtent - padding.y;

        Vector3 result = desiredWorldPosition;

        result.x = (minX > maxX)
            ? (minBounds.x + maxBounds.x) * 0.5f
            : Mathf.Clamp(result.x, minX, maxX);

        result.y = (minY > maxY)
            ? (minBounds.y + maxBounds.y) * 0.5f
            : Mathf.Clamp(result.y, minY, maxY);

        return result;
    }
}