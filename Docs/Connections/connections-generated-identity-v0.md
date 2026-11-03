# Connections Generated Identity Behaviors V0

**Scope:** Connections ordinary ambient hostile logical connections only. At each successful ordinary hostile plan, one categorical selection chooses normal identity, Continuous ID Hopping, or Reactive Identity Misdirection. This is an optional characteristic of the already selected LOADER or DELIVERY/INFECTOR recipe. It adds no opportunity, objective, connection, or clock. Benign traffic, Reconnaissance scans and follow-ups, Spawner children, Payloads, Worms, explicit developer spawns, and deterministic recipe builders remain outside this selection.

Both Connections scenes serialize provisional 10% hopping and 10% reactive chances within the ordinary hostile connection category, leaving 80% normal. Each chance can be zero. If the two authored chances exceed 100% together, the traffic director preserves their ratio and normalizes them to a combined 100%; run metadata reports the effective values. Generated hopping uses the existing two-simulation-second interval; reactive identity has no timer. The resulting Connection Plan explicitly stores its mode, interval, and alias seed, so a future directed or authored connection can select a behavior independently of these ambient chances.

`ConnectionTrafficDirector.GenerateHostilePlan` first completes the ordinary objective, PURPOSE, target, route, operations, modifiers, infection, and timing decisions. It then uses a dedicated seed-derived identity-selection stream. The seed and roll are independent of ordinary traffic, coverage, hostile scheduling, and activity-category random draws. A plan carries `identitySelectionEligible`, its roll, and selected mode in the existing generation trace, including when normal identity wins. The concrete mode, seed, and interval remain in the queued definition. Recorder run metadata stores both effective probabilities. None of these internal decisions appears in ordinary inspection or automatic terminal output. Canonical IDs still skip live aliases to prevent collisions, as established by ID Hopping V0; that collision safeguard can change a later generated ID choice during live play.

The four-node scene retains its ordinary-only hostile category and 54-second mean opportunity interval. The Advanced scene retains its 4:2:2:1 ordinary/Reconnaissance/Payload/Worm weights and 30-second mean. Its identity behaviors therefore occupy a smaller share of all hostile opportunities than of successful ordinary connections.

## Deterministic evidence

Unity 6000.3.11f1 batch Editor witness sampled 120 ordinary hostile plans for each of six seeds in each scene. Both scenes observed **580 normal, 74 hopping, and 66 reactive** out of 720 plans. For each seed, zero-probability and 10%/10% runs produced identical canonical IDs, PURPOSE choices, routes, operation values, infection kinds, and generation decisions. Replay reproduced every identity mode, alias seed, and selection roll. Zero probabilities produced normal identities throughout. The witness also checked excluded plan paths, queued recorder evidence, collision rejection, and Probe revelation for naturally generated hoppers and reactive connections.

Six additional seeded **900-second generated sessions per scene** produced:

| Scene | All hostile opportunities | Successful ordinary hostile connections | Normal | Hopping | Reactive | Hopping / all opportunities | Reactive / all opportunities |
|---|---:|---:|---:|---:|---:|---:|---:|
| Connections | 97 | 97 | 74 | 12 | 11 | 12.4% | 11.3% |
| ConnectionsAdvanced | 195 | 93 | 71 | 11 | 11 | 5.6% | 5.6% |

Within successful ordinary connections, the observed shares were 76.3% normal, 12.4% hopping, 11.3% reactive in Connections and 76.3% normal, 11.8% hopping, 11.8% reactive in Advanced. These are seeded observations, not a balance target or proof of future encounter frequency.

As a share of **all** hostile opportunities, normal identity appeared in 76.3% of the four-node opportunities and 36.4% of Advanced opportunities. Successful ordinary connections represented 100% and 47.7% of those opportunities, respectively. The hopping and reactive shares of all opportunities appear in the table above.

## Manual review

Play both scenes at 16:9 with ambient generation on. Watch ordinary traffic long enough to see occasional hoppers and reactive connections among normal IDs. Try a displayed false ID with `kill` or `inspect`, then Probe a crowded occupied edge. Check pills, LOAD/WORK diagnostic rows, terminal focus, simultaneous hostile pressures, and whether the two behaviors remain readable without overwhelming normal traffic. Automated Editor witnesses do not establish interactive pacing or difficulty.
