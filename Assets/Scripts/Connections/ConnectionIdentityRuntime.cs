using System;

namespace Uptime.Connections
{
    public enum ConnectionIdentityMode { Normal, ContinuousHopping, ReactiveMisdirection }

    // Only the advertised label changes. ConnectionId remains the simulation key.
    public sealed class ConnectionIdentityRuntime
    {
        private const string Letters = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const double BoundaryTolerance = 0.00001;
        private readonly string trueId;
        private readonly float hopSeconds;
        private readonly ConnectionIdentityMode mode;
        private readonly string[] aliases;
        private int cursor;
        private double nextHopAt;
        private bool started;
        private bool revealed;

        public ConnectionIdentityRuntime(string connectionId, float intervalSeconds, int seed,
            ConnectionIdentityMode identityMode = ConnectionIdentityMode.Normal)
        {
            trueId = connectionId;
            hopSeconds = intervalSeconds;
            mode = identityMode == ConnectionIdentityMode.Normal && intervalSeconds > 0f ?
                ConnectionIdentityMode.ContinuousHopping : identityMode;
            AliasIdentity = connectionId;
            if (mode == ConnectionIdentityMode.Normal) return;
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

        public ConnectionIdentityMode Mode => mode;
        public bool IsHopping => mode == ConnectionIdentityMode.ContinuousHopping;
        public bool IsDisguised => aliases != null;
        public bool Started => started;
        public bool IsRevealed => revealed;
        public string AliasIdentity { get; private set; }
        public string KnownIdentity => revealed ? trueId : AliasIdentity;
        public int HopCount { get; private set; }
        public double NextHopAtSeconds => nextHopAt;

        public bool Start(double now, Func<string, bool> available)
        {
            if (!IsDisguised || started) return false;
            started = true;
            if (IsHopping) nextHopAt = now + hopSeconds;
            return ChangeAlias(available);
        }

        public void Advance(double now, Func<string, bool> available,
            Action<double, string> changed)
        {
            if (!started || !IsHopping) return;
            while (now + BoundaryTolerance >= nextHopAt)
            {
                double at = nextHopAt;
                nextHopAt += hopSeconds;
                if (ChangeAlias(available)) changed?.Invoke(at, AliasIdentity);
            }
        }

        public bool ReactToFailedAddress(string attemptedId, Func<string, bool> available)
        {
            return mode == ConnectionIdentityMode.ReactiveMisdirection && started &&
                !revealed && string.Equals(attemptedId, AliasIdentity,
                    StringComparison.OrdinalIgnoreCase) && ChangeAlias(available);
        }

        public void SetRevealed(bool value) => revealed = value && IsDisguised;

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
