using System;
using System.Collections.Generic;

namespace Uptime.Connections
{
    // Experimental tree only. This is not the shared Uptime topology grammar.
    public sealed class StressTopologyGraph
    {
        public sealed class Node
        {
            public string Id;
            public string Role;
            public bool External;
            public int Parent;
            public int Depth;
        }

        public readonly struct Edge
        {
            public readonly int A;
            public readonly int B;
            public Edge(int a, int b) { A = a; B = b; }
        }

        public readonly List<Node> Nodes = new List<Node>();
        public readonly List<Edge> Edges = new List<Edge>();

        public static StressTopologyGraph Generate(int count, int seed)
        {
            if (count != 4 && count != 8 && count != 12 && count != 16 &&
                count != 20 && count != 24 && count != 28)
                throw new ArgumentOutOfRangeException(nameof(count), "Use a requested 4-node tier.");
            var result = new StressTopologyGraph();
            var random = new Random(seed);
            int externalCount = count >= 24 ? 4 : count >= 16 ? 3 : count >= 8 ? 2 : 1;
            int internalCount = count - externalCount;
            var childCounts = new int[internalCount];
            for (int i = 0; i < internalCount; i++)
            {
                int parent = i == 0 ? -1 : (i - 1) / 2;
                if (i > 3 && random.Next(4) == 0)
                {
                    int alternate = Math.Max(0, parent - 1);
                    if (childCounts[alternate] < 3) parent = alternate;
                }
                int depth = parent < 0 ? 0 : result.Nodes[parent].Depth + 1;
                result.Nodes.Add(new Node
                {
                    Id = "N" + (i + 1).ToString("00"),
                    Role = i == 0 ? "TRANSIT" : i % 5 == 4 ? "CORE" : "SERVICE",
                    Parent = parent, Depth = depth
                });
                if (parent >= 0)
                {
                    childCounts[parent]++;
                    result.Edges.Add(new Edge(parent, i));
                }
            }
            for (int i = 0; i < externalCount; i++)
            {
                int parent = Math.Min(internalCount - 1, i == 0 ? 0 :
                    i == 1 ? Math.Min(2, internalCount - 1) :
                    i == 2 ? Math.Min(4, internalCount - 1) : internalCount - 1);
                int index = result.Nodes.Count;
                result.Nodes.Add(new Node
                {
                    Id = "X" + (i + 1), Role = "ENTRY", External = true,
                    Parent = parent, Depth = result.Nodes[parent].Depth + 1
                });
                result.Edges.Add(new Edge(parent, index));
            }
            return result;
        }
    }
}
