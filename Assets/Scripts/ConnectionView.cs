using UnityEngine;

[ExecuteAlways]
public class ConnectionView : MonoBehaviour
{
    public string connectionId;

    public NodeView nodeA;
    public NodeView nodeB;

    [Min(1)]
    public int lengthSteps = 5;

    [Min(1)]
    public int latency = 1;

    public LineRenderer lineRenderer;

    private void Reset()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void OnValidate()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        RefreshLine();
    }

    public void RefreshLine()
    {
        if (lineRenderer == null || nodeA == null || nodeB == null)
            return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, nodeA.transform.position);
        lineRenderer.SetPosition(1, nodeB.transform.position);
    }

    public Vector3 GetWorldPositionAtStep(int step, bool aToB)
    {
        if (nodeA == null || nodeB == null)
            return transform.position;

        int clampedStep = Mathf.Clamp(step, 0, lengthSteps);

        Vector3 start = aToB ? nodeA.transform.position : nodeB.transform.position;
        Vector3 end = aToB ? nodeB.transform.position : nodeA.transform.position;

        float t = lengthSteps == 0 ? 0f : (float)clampedStep / lengthSteps;
        return Vector3.Lerp(start, end, t);
    }

    public NodeView GetStartNode(bool aToB)
    {
        return aToB ? nodeA : nodeB;
    }

    public NodeView GetEndNode(bool aToB)
    {
        return aToB ? nodeB : nodeA;
    }
}