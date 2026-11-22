# Congestion & Ambient Traffic V1 — review guide

**Partial positive result; keep for playtesting and revise before adoption.** Nominal occupancy correctly decouples business demand from actual slowdown, kill and outages. It does not create useful congestion by itself at current tuning. Moderate legitimate LOAD plus headroom/concentration can produce real post-removal pressure in some seeds at modest connection counts. Longer sources worsen that pressure, and existing tools help. The current WORK quotas often finish much earlier, so this is not a production traffic/balance selection.

Connections implementation only. Branch `experiment/connections-congestion-ambient-v1`, based on unmerged #125 at `7c98f4fc20fe75e3af808e3a05677f76f696c19e`. Its draft PR targets #125's branch. Main, #124, #125 and #115 remain unchanged/unmerged by this work. No scenes, prefabs, Packet code, allocator, enemy, CORE, Overclock, WORK-credit, ending or quota rules were changed.

Read [results](RESULTS.md), [validation/reproduction](VALIDATION.md), [runtime contract](../../Connections/connections-congestion-ambient-v1.md), and the concise [implemented/future traffic-source map](../../Connections/connections-traffic-source-map-v1.md).

## Evidence

**338 retained actual Unity runs**: 272 diagnostic controlled runs, 30 actual-quota controlled runs, and 36 mixed-threat shifts. Four seeds validate the two most useful tuning points; two-seed screens retain the negative alternatives. The [per-run CSV](runs.csv), [generated tables](tables.md) and gzip suites retain admissions, active/connecting counts, per-node LOAD, actual shortfall integrals, operation delays, WORK traces, CORE/endings and sampled paired recovery differences. `summarize.py` reduces Unity data; it does not simulate gameplay.

| Data file | Runs | Purpose |
|---|---:|---|
| [nominal-first](nominal-first.json.gz) | 24 | Untuned nominal first; 5/10/20/30-second and overlapping sources |
| [comparisons](comparisons.json.gz) | 48 | Population baseline, independent trial rate and empirical rate match |
| [variations](variations.json.gz) | 32 | LOAD×3 and increased nominal headroom; includes unhealthy baselines |
| [headroom](headroom.json.gz) | 16 | LOAD×2 with larger expected population |
| [concentration](concentration.json.gz) | 32 | Moderate LOAD with actual DB-purpose concentration |
| [full](full.json.gz) | 80 | Four seeds, moderate candidate, survival durations, overlaps and recovery tools |
| [advanced-target](advanced-target.json.gz) | 40 | Four-seed higher-concurrency Advanced follow-up |
| [quotas](quotas.json.gz) / [advanced-quotas](advanced-quotas.json.gz) | 24 / 6 | Ordinary 900/1,200 quotas and actual early endings |
| [natural](natural.json.gz) / [advanced-natural](advanced-natural.json.gz) | 32 / 4 | Ordinary mixed threats and limited visible defense |

[Paired traces](paired-traces.png) and [duration/tool comparison](duration-and-tools.png) show actual execution. [30 V1 assertions](deterministic-checks.txt), [four exact screening replays](screening-replay-checks.txt), [32 #125 assertions](v0-deterministic-checks.txt), [52 WORK checks](parent-work-checks.txt), [18 exact original quota checks](parent-quota-checks.txt) and applicable existing regressions accompany them. The aggregate still fails the same two inherited UI assertions; see validation.

Sixteen actual Play Mode captures cover both scenes at 1920×1080 and 1280×720: [healthy Small](Captures/Connections/01-healthy-1280x720.png), [active source](Captures/Connections/02-loader-1280x720.png), [source gone, legitimate traffic still slowed](Captures/Connections/03-after-kill-1280x720.png), and [Advanced after kill](Captures/ConnectionsAdvanced/03-after-kill-1280x720.png). Captures use the moderate LOAD×2/default-goal configuration and fixed DB source, **not** the later Advanced goal8 configuration. Recovery capture is at 116 seconds; expired-Overclock capture is at 132. These images are not a same-time tool-effect comparison; numeric paired traces provide that comparison. Existing Integrity damage can remain after LOAD recovers.

## Short manual playtest

1. Use Unity **6000.3.11f1** on this branch. Open `Connections.unity`, then `ConnectionsAdvanced.unity`. Keep canonical scene files unsaved; use runtime Inspector edits in Play Mode and `restart`. Disable `Randomize Seed`, choose 1701, then 1702/3141/9001. Restart after each policy/tuning change.
2. Compare untouched Population Responsive with Nominal Occupancy at multiplier1 and empty experimental weights. Demand should continue during slowdown; killing a known legitimate job should not cause an immediate replacement. Normal manual `spawn` is outside the nominal ambient ledger.
3. Try the provisional Small point: Nominal Occupancy, LOAD multiplier **2**, goal **3**, and sparse experimental benign weights `product-lookup=3.75`, `inventory-sync=1.5`, `sales-export=.625`. Other entries fall back to their current weights. These modify business mix, not routes or hostile selection.
4. In Advanced, start with multiplier2, goal4, and only `product-lookup=3.75`; its inventory/export destinations are JOBS/RPT. For the more informative Advanced pressure point, try multiplier **1.5**, goal **8**, `product-lookup=3`. This is an experimental test point; do not save it as canonical tuning.
5. Play real six-minute quota-enabled shifts and record ending/time, decisions needed, Integrity and whether early success hides recovery. For an uncensored recovery view, temporarily use target1,000,000 with duration360; keep Integrity enabled. The Editor witness supplies exact 8-LOAD/5–30-second fixtures. Existing developer `spawn <purposeId> obj:loader` explores authored variants but does not guarantee that fixture.
6. Kill a known source, compare `overclock <node>`, and observe after its 15-second boost expires. Try sacrificing one **known legitimate** job only when the tradeoff is clear; compare released LOAD with lost/foregone confirmed WORK. Do not call a cleaner-looking network a productivity improvement.
7. At both 16:9 sizes, inspect busy bundles and structured `inspect -v` output, scroll history, issue successive commands, and check input focus, labels, WORK/tool strip and terminal/network proportions. Real keyboard/IME, sustained command rhythm and crowded peak states still need human review.

Nothing merges automatically. Leave the PR draft and unmerged until human testing and an explicit traffic-model decision.
