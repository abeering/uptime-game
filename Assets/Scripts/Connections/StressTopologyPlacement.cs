using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Intentionally simple depth columns and perimeter entries. Diagnostics expose its failures.
    public static class StressTopologyPlacement
    {
        public static Vector2[] Place(StressTopologyGraph graph, Vector2 areaSize, int seed)
        {
            var positions = new Vector2[graph.Nodes.Count];
            var levels = new SortedDictionary<int, List<int>>();
            int maxDepth = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i].External) continue;
                int depth = graph.Nodes[i].Depth;
                if (!levels.TryGetValue(depth, out List<int> level))
                    levels.Add(depth, level = new List<int>());
                level.Add(i);
                maxDepth = Math.Max(maxDepth, depth);
            }
            var random = new System.Random(unchecked(seed ^ 0x3F519));
            foreach (KeyValuePair<int, List<int>> level in levels)
            {
                float x = Mathf.Lerp(-areaSize.x * 0.5f + 155f,
                    areaSize.x * 0.5f - 155f,
                    maxDepth == 0 ? 0.5f : (float)level.Key / maxDepth);
                for (int row = 0; row < level.Value.Count; row++)
                {
                    float y = (0.5f - (row + 0.5f) / level.Value.Count) *
                        (areaSize.y - 110f);
                    float jitter = level.Value.Count == 1 ? 0f :
                        ((float)random.NextDouble() - 0.5f) * 20f;
                    positions[level.Value[row]] = new Vector2(x, y + jitter);
                }
            }
            int externalIndex = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (!graph.Nodes[i].External) continue;
                float halfW = areaSize.x * 0.5f - 148f;
                float halfH = areaSize.y * 0.5f - 52f;
                positions[i] = externalIndex == 0 ? new Vector2(-halfW, halfH * 0.75f) :
                    externalIndex == 1 ? new Vector2(-halfW * 0.45f, halfH) :
                    externalIndex == 2 ? new Vector2(halfW * 0.45f, -halfH) :
                    new Vector2(halfW, halfH * 0.75f);
                externalIndex++;
            }
            return positions;
        }
    }
}
