# Calibration evidence

Eight seeds per canonical scene: `1701`, `1702`, `3141`, `9001`, `42`, `99`, `31146359`, `1987295192`. All runs retain authored SPREAD, traffic/catalog/topology settings and mixed ambient families. The human recording seeds are rerun from scene defaults; these are not reproductions of those human command histories.

48 natural runs continue until CORE breach or 360 seconds with an unreachable diagnostic quota of 1,000,000, so quota success cannot truncate the throughput baseline. Six repeats (seed 1701 × both scenes × all three policies) compare exact gameplay event/snapshot SHA-256 signatures. Initial metadata/UTC timestamps are excluded from that comparison. Full exact per-operation credit facts, signatures, candidate-deadline WORK and final eligible/lost/unfinished totals are in [calibration.json](calibration.json); [the CSV](calibration-summary.csv) is easier to compare.

The visible-defense policy considers only visible final-hop Payloads and node requested LOAD/capacity, and issues at most one command every six simulation seconds: node-wide interception first, otherwise overclock the most loaded node if overloaded and ready. It does not read hostility, infections, Integrity secrets, or directive intent, and does not scan/CLEAN or type to win Duels. Kill-all attempts every visible active/connecting ID every second, starting at six seconds. All policies respect Duel startup, attacker race and result input ownership. The deterministic Edit Mode driver advances the real seeded hacker race after its authored splash and resolves ordinary losses; it does not invent concessions or assume wins. No policy forces traffic, infection, node damage or WORK.

## Provisional quotas

| Scene | Quota | Unattended successes | Visible-defense successes | Kill-all successes |
|---|---:|---:|---:|---:|
| Connections | 900 | 4/8 | 6/8 | 0/8 |
| ConnectionsAdvanced | 1,200 | 3/8 | 5/8 | 0/8 |

These counts use the first genuine quota-crossing credit before any same-boundary CORE breach; merely surviving to 360 seconds is not success. Eighteen actual quota-enabled runs (seeds 1701,9001,31146359 × both scenes × three policies) match the predicted endings, exact crossing times and credit prefixes. See [target-checks.txt](target-checks.txt) and [actual-target-runs.json](actual-target-runs.json).

Small 900 preserves a distinction between unattended and limited intervention while avoiding the 1,000 WORK candidate’s 2/8 versus 4/8 completion rates. Advanced 1,200 similarly distinguishes 3/8 versus 5/8; 1,400 reduces visible-defense completion to 4/8. These are playtest starting points, not an optimized or fair quota claim. Traffic and threat frequencies were not adjusted.

## Candidate deadlines at the selected quotas

| Scene / policy | Success by 180s | Success by 270s | Success by 360s |
|---|---:|---:|---:|
| Connections / unattended | 0/8 | 2/8 | 4/8 |
| Connections / visible-defense | 0/8 | 2/8 | 6/8 |
| Connections / kill-all | 0/8 | 0/8 | 0/8 |
| ConnectionsAdvanced / unattended | 0/8 | 1/8 | 3/8 |
| ConnectionsAdvanced / visible-defense | 0/8 | 1/8 | 5/8 |
| ConnectionsAdvanced / kill-all | 0/8 | 0/8 | 0/8 |

## Limits and tradeoffs observed

- Kill-all sacrifices quota completion in all 16 measured scene/seed pairs. Fifteen earn zero WORK; Small seed 3141 earns 51.50 before later kills. It survives CORE in all 16, demonstrating why survival alone rewards indiscriminate killing in this sample.
- Small seed 99 rises from 654.70 WORK with CORE breach at 285.32s to 1,094.22 WORK without breach under visible defense. Seed 31146359 rises from 697.46 to 1,034.08. The exact totals and crossing times remain in the JSON/CSV.
- Advanced seed 9001 rises from 882.61 WORK with CORE breach at 267.30s to 1,636.96 without breach under visible defense; the actual 1,200 quota ends successfully at 296.48s with 1,276.62 credited WORK. Completion credits arrive in operation-sized steps, so quota overshoot is expected.
- Intervention is not uniformly beneficial: Advanced seed 31146359 produces 534.80 unattended versus 445.82 under the limited defense policy; both miss quota. Advanced seed 1701 breaches sooner under this policy (266.23s versus 274.91s) and misses 1,200.
- Small seed 9001 naturally produces only 220.64 WORK under both non-kill policies over 360 seconds. Several other seeds miss quota. These are failed observed policies, not proof that every possible player strategy is mathematically unable to win. No traffic was added to repair poor cases.
- Neither selected quota is completed within 180 seconds by these policies. The sample shows no immediate/trivial quota wins, but eight seeds cannot establish general difficulty, fairness or fun. Missing scan/CLEAN use and no player Duel wins limit the policy’s strength. Human play remains necessary.

The diagnostics count materialized benign opportunities, including whole uncredited operation requirements. They do not promise a known future supply. Raw baseline outcome `InsufficientWork` refers to its intentionally unreachable diagnostic target; use `quotaOutcome` in the CSV or actual quota runs for selected-quota success/failure.
