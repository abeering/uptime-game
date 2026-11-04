# Payload Final-Approach Timing V1 — deterministic Unity evidence

Unity 6000.3.11f1 ran the expanded `PayloadWitness` in batch Editor mode against an isolated copy of this branch. The witness starts default Payloads with no travel-duration override, advances the authoritative simulation through each orange hop, arms an interceptor when the final red hop begins establishing, and steps to arrival. The values below are simulation seconds from the witness log, rounded to three decimals. Expected arrival is `redAt + 1.2 + finalTravel`; every observed arrival was before the interceptor's `redAt + 8` expiry.

| Scene | Profile | Route | Red at | Final traversal | Expected arrival | Observed arrival | Window expires | Result |
|---|---|---|---:|---:|---:|---:|---:|---|
| Connections | Steady | One hop | 0.000 | 5.318 | 6.518 | 6.518 | 8.000 | Caught |
| Connections | Steady | Multi-hop | 17.792 | 5.164 | 24.156 | 24.155 | 25.792 | Caught |
| Connections | Variable | One hop | 0.000 | 5.318 | 6.518 | 6.518 | 8.000 | Caught |
| Connections | Variable | Multi-hop | 13.930 | 5.164 | 20.294 | 20.294 | 21.930 | Caught |
| ConnectionsAdvanced | Steady | One hop | 0.000 | 5.318 | 6.518 | 6.518 | 8.000 | Caught |
| ConnectionsAdvanced | Steady | Multi-hop | 34.810 | 5.782 | 41.792 | 41.792 | 42.810 | Caught |
| ConnectionsAdvanced | Variable | One hop | 0.000 | 5.318 | 6.518 | 6.518 | 8.000 | Caught |
| ConnectionsAdvanced | Variable | Multi-hop | 25.076 | 5.782 | 32.058 | 32.057 | 33.076 | Caught |

The small one-millisecond differences shown after rounding come from float stepping. The assertions compare unrounded simulation values within 0.001 seconds. In both scenes, the witness also let a default one-hop Payload impact at 6.518 seconds, then armed node-wide interception; the infection persisted and targeted interception rejected the resolved ID. It checked unchanged intermediate ranges (Steady 15–20, Variable 7–25), an explicit one-hop 20-second override, seeded replay, recorder per-hop durations, node-wide repeated catches, targeted filtering, exact expiry ordering, 15-second cooldown, and 4-LOAD demand. `GeometryWitness`, `InfectionWitness`, `AmbientHostileActivityWitness`, and `ResourceWitness` passed in the same isolated Unity project.

These are deterministic simulation and Editor checks, not an interactive timing playtest. For manual review, reopen `Connections.unity` and `ConnectionsAdvanced.unity` from disk, enter Play Mode at 16:9, and use the development `payload db steady` and `payload db variable` commands. On a red hop, submit `intercept @ db` immediately and confirm the catch; repeat with `intercept xN @ db`, then deliberately wait until after impact to confirm the missed strike. Check the terminal prompt focus, response time, 8-second status timer, unchanged cooldown and LOAD demand, and color/label readability during ordinary traffic.
