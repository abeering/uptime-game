using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Presentation state only. Six authored accents rotate independently of connection IDs.
    [Serializable]
    public sealed class ConnectionIdentityPalette
    {
        [SerializeField] private Color cyan = new Color(0.36f, 0.87f, 0.91f);
        [SerializeField] private Color teal = new Color(0.35f, 0.91f, 0.76f);
        [SerializeField] private Color blueGreen = new Color(0.43f, 0.82f, 0.92f);
        [SerializeField] private Color seafoam = new Color(0.43f, 0.89f, 0.79f);
        [SerializeField] private Color turquoise = new Color(0.31f, 0.85f, 0.84f);
        [SerializeField] private Color coolMint = new Color(0.49f, 0.87f, 0.75f);

        [NonSerialized] private Dictionary<LogicalConnectionState, int> slots;
        [NonSerialized] private int nextSlot;

        public const int SlotCount = 6;

        public Color Assign(LogicalConnectionState connection)
        {
            if (slots == null)
                slots = new Dictionary<LogicalConnectionState, int>();
            if (slots.TryGetValue(connection, out int existing))
                return ColorAt(existing);

            var occupied = new bool[SlotCount];
            foreach (int slot in slots.Values)
                occupied[slot] = true;
            int chosen = nextSlot;
            for (int i = 0; i < SlotCount; i++)
            {
                int candidate = (nextSlot + i) % SlotCount;
                if (occupied[candidate]) continue;
                chosen = candidate;
                break;
            }
            slots.Add(connection, chosen);
            nextSlot = (chosen + 1) % SlotCount;
            return ColorAt(chosen);
        }

        public bool TryGet(LogicalConnectionState connection, out Color color)
        {
            if (slots != null && slots.TryGetValue(connection, out int slot))
            {
                color = ColorAt(slot);
                return true;
            }
            color = default;
            return false;
        }

        public void Release(LogicalConnectionState connection) => slots?.Remove(connection);

        public void Reset()
        {
            slots?.Clear();
            nextSlot = 0;
        }

        public Color ColorAt(int slot)
        {
            switch (slot)
            {
                case 0: return cyan;
                case 1: return teal;
                case 2: return blueGreen;
                case 3: return seafoam;
                case 4: return turquoise;
                case 5: return coolMint;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }
    }
}
