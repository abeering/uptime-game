using System.Collections.Generic;
using UnityEngine;

public class PacketOverlapCoordinator : MonoBehaviour
{
    [Header("Overlap Detection")]
    [Min(0.01f)]
    public float overlapDistance = 0.02f;

    [Header("Cycle Timing")]
    [Min(0.05f)]
    public float cycleInterval = 0.75f;

    [Header("Sorting")]
    public int baseSortOrder = 10;
    public int topBoost = 10;

    private readonly List<PacketView> packets = new();
    private readonly List<List<PacketView>> groups = new();

    void Update()
    {
        CollectPackets();
        BuildGroups();
        ApplySorting();
    }

    private void CollectPackets()
    {
        packets.Clear();

        PacketView[] found = FindObjectsOfType<PacketView>();
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && found[i].gameObject.activeInHierarchy)
                packets.Add(found[i]);
        }
    }

    private void BuildGroups()
    {
        groups.Clear();

        HashSet<PacketView> visited = new();

        for (int i = 0; i < packets.Count; i++)
        {
            PacketView seed = packets[i];
            if (seed == null || visited.Contains(seed))
                continue;

            List<PacketView> group = new();
            Queue<PacketView> queue = new();

            queue.Enqueue(seed);
            visited.Add(seed);

            while (queue.Count > 0)
            {
                PacketView current = queue.Dequeue();
                group.Add(current);

                Vector3 currentPos = current.transform.position;

                for (int j = 0; j < packets.Count; j++)
                {
                    PacketView other = packets[j];
                    if (other == null || visited.Contains(other))
                        continue;

                    float dist = Vector3.Distance(currentPos, other.transform.position);

                    float currentRadius = current.GetVisualOverlapRadius();
                    float otherRadius = other.GetVisualOverlapRadius();
                    float threshold = currentRadius + otherRadius;

                    if (dist <= threshold)
                    {
                        visited.Add(other);
                        queue.Enqueue(other);
                    }
                }
            }

            groups.Add(group);
        }
    }

    private void ApplySorting()
    {
        float now = Time.time;

        for (int i = 0; i < groups.Count; i++)
        {
            List<PacketView> group = groups[i];

            if (group.Count == 0)
                continue;

            group.Sort((a, b) => string.CompareOrdinal(a.PacketId, b.PacketId));

            if (group.Count == 1)
            {
                group[0].SetVisualSortOrder(baseSortOrder);
                continue;
            }

            int dominantIndex = Mathf.FloorToInt(now / cycleInterval) % group.Count;

            for (int j = 0; j < group.Count; j++)
            {
                int order = (j == dominantIndex)
                    ? baseSortOrder + topBoost
                    : baseSortOrder;

                group[j].SetVisualSortOrder(order);
            }
        }
    }
}