using System;

namespace Uptime.Connections
{
    // Only the advertised label changes. ConnectionId remains the simulation key.
    public sealed class ConnectionIdentityRuntime
    {
        private const string Letters = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const double BoundaryTolerance = 0.00001;
        private readonly string trueId;
        private readonly float hopSeconds;
        private readonly string[] aliases;
        private int cursor;
        private double nextHopAt;
        private bool started;
        private bool revealed;

        public ConnectionIdentityRuntime(string connectionId, float intervalSeconds, int seed)
        {
            trueId = connectionId;
            hopSeconds = intervalSeconds;
            AliasIdentity = connectionId;
            if (intervalSeconds <= 0f) return;
            aliases = new string[Letters.Length * Digits.Length];
            for (int i = 0; i < aliases.Length; i++)
                aliases[i] = new string(new[] { Letters[i / Digits.Length],
                    Digits[i % Digits.Length] });
            var random = new Random(seed);
            for (int i = aliases.Length - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                (aliases[i], aliases[swap]) = (aliases[swap], aliases[i]);
            }
        }

        public bool IsHopping => aliases != null;
        public bool Started => started;
        public bool IsRevealed => revealed;
        public string AliasIdentity { get; private set; }
        public string KnownIdentity => revealed ? trueId : AliasIdentity;
        public int HopCount { get; private set; }
        public double NextHopAtSeconds => nextHopAt;

        public bool Start(double now, Func<string, bool> available)
        {
            if (!IsHopping || started) return false;
            started = true;
            nextHopAt = now + hopSeconds;
            return ChangeAlias(available);
        }

        public void Advance(double now, Func<string, bool> available,
            Action<double, string> changed)
        {
            if (!started) return;
            while (now + BoundaryTolerance >= nextHopAt)
            {
                double at = nextHopAt;
                nextHopAt += hopSeconds;
                if (ChangeAlias(available)) changed?.Invoke(at, AliasIdentity);
            }
        }

        public void SetRevealed(bool value) => revealed = value && IsHopping;

        private bool ChangeAlias(Func<string, bool> available)
        {
            // A full pass guarantees the exhaustion fallback without relying on random retries.
            for (int i = 0; i < aliases.Length; i++)
            {
                string candidate = aliases[cursor];
                cursor = (cursor + 1) % aliases.Length;
                if (string.Equals(candidate, AliasIdentity, StringComparison.OrdinalIgnoreCase) ||
                    !available(candidate)) continue;
                AliasIdentity = candidate;
                HopCount++;
                return true;
            }
            // If no fresh alias exists, retain a legal one. Otherwise expose the true ID
            // and warn at the host rather than displaying a colliding false identity.
            if (available(AliasIdentity)) return false;
            AliasIdentity = trueId;
            return false;
        }
    }
}
