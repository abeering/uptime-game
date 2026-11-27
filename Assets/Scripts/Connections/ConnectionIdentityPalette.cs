using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Presentation state only. Six authored accents rotate independently of connection IDs.
    [Serializable]
    public sealed class ConnectionIdentityPalette
    {
        [NonSerialized] private Display.ConnectionDisplayStyle style;
        // Released paths free their live slot but keep terminal/history identity until reset.
        [NonSerialized] private Dictionary<LogicalConnectionState, int> historicalSlots;
        public void UseStyle(Display.ConnectionDisplayStyle sharedStyle) => style = sharedStyle;

        [NonSerialized] private Dictionary<LogicalConnectionState, int> slots;
        [NonSerialized] private int nextSlot;

        public const int SlotCount = 6;

        public Color Assign(LogicalConnectionState connection) => ColorFor(connection, AssignSlot(connection));
        private Color ColorFor(LogicalConnectionState connection, int slot) => style != null ?
            style.IdentityColor(connection.IsImportant ? Display.DisplayTrafficClass.Important : Display.DisplayTrafficClass.Ordinary, slot) : Color.gray;

        // Shared display families reuse the same lifetime slot ownership, independent of aliases.
        public int AssignSlot(LogicalConnectionState connection)
        {
            if (slots == null)
                slots = new Dictionary<LogicalConnectionState, int>();
            if (historicalSlots == null) historicalSlots = new Dictionary<LogicalConnectionState, int>();
            if (historicalSlots.TryGetValue(connection, out int existing))
                return existing;

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
            historicalSlots.Add(connection, chosen);
            nextSlot = (chosen + 1) % SlotCount;
            return chosen;
        }

        public bool TryGet(LogicalConnectionState connection, out Color color)
        {
            if (historicalSlots != null && historicalSlots.TryGetValue(connection, out int slot))
            {
                color = ColorFor(connection, slot);
                return true;
            }
            color = default;
            return false;
        }

        public void Release(LogicalConnectionState connection) => slots?.Remove(connection);

        public void Reset()
        {
            slots?.Clear();
            historicalSlots?.Clear();
            nextSlot = 0;
        }

        public Color ColorAt(int slot) => style != null ? style.IdentityColor(Display.DisplayTrafficClass.Ordinary, slot) : Color.gray;
    }
}
