# Natural directive comparison

Derived by `python3 analyze.py`; seconds are simulation time. Empty breach time means no loss by the observation horizon.

| Directive | Seed | Policy | Breach s | Core/path | One-hit-away s | Eligible / preferred / changed | Fallback |
|---|---:|---|---:|---|---:|---|---:|
| SPREAD | 1701 | unattended | 403.517 | EXT1>WEB>API>DB | 233.638 | 42 / 0 / 0 | 52 |
| SPREAD | 1701 | visible-defense | 402.838 | EXT2>JOBS>API>DB | 232.958 | 42 / 0 / 0 | 52 |
| SPREAD | 1701 | kill-all | — | — | 0 | 57 / 0 / 0 | 72 |
| SPREAD | 1702 | unattended | 205.117 | EXT2>JOBS>API>DB | 7.011 | 21 / 0 / 0 | 28 |
| SPREAD | 1702 | visible-defense | — | — | 0 | 65 / 0 / 0 | 83 |
| SPREAD | 1702 | kill-all | — | — | 0 | 56 / 0 / 0 | 83 |
| SPREAD | 3141 | unattended | 331.157 | EXT2>JOBS>API>DB | 29.284 | 20 / 0 / 0 | 25 |
| SPREAD | 3141 | visible-defense | — | — | 81.699 | 44 / 0 / 0 | 56 |
| SPREAD | 3141 | kill-all | — | — | 0 | 35 / 0 / 0 | 56 |
| SPREAD | 9001 | unattended | 133.352 | EXT1>WEB>AUTH | 46.006 | 21 / 0 / 0 | 24 |
| SPREAD | 9001 | visible-defense | 347.928 | EXT1>WEB>API>DB | 147.782 | 40 / 0 / 0 | 49 |
| SPREAD | 9001 | kill-all | — | — | 0 | 64 / 0 / 0 | 90 |
| BREACH | 1701 | unattended | 244.535 | EXT1>WEB>AUTH | 132.822 | 27 / 19 / 14 | 13 |
| BREACH | 1701 | visible-defense | 165.278 | EXT1>WEB>AUTH | 11.742 | 18 / 12 / 10 | 10 |
| BREACH | 1701 | kill-all | — | — | 0 | 57 / 37 / 33 | 35 |
| BREACH | 1702 | unattended | 119.532 | EXT1>WEB>AUTH | 28.588 | 11 / 8 / 5 | 5 |
| BREACH | 1702 | visible-defense | 206.957 | EXT1>WEB>AUTH | 0.25 | 21 / 16 / 11 | 12 |
| BREACH | 1702 | kill-all | 451.091 | EXT1>WEB>AUTH | 360.147 | 46 / 33 / 23 | 26 |
| BREACH | 3141 | unattended | 314.945 | EXT1>WEB>AUTH | 19.569 | 17 / 13 / 11 | 8 |
| BREACH | 3141 | visible-defense | 317.024 | EXT1>WEB>AUTH | 65.508 | 18 / 14 / 12 | 8 |
| BREACH | 3141 | kill-all | — | — | 0 | 35 / 29 / 26 | 27 |
| BREACH | 9001 | unattended | 122.351 | EXT1>WEB>AUTH | 20.976 | 21 / 12 / 9 | 12 |
| BREACH | 9001 | visible-defense | 122.351 | EXT1>WEB>AUTH | 30.375 | 21 / 12 / 9 | 12 |
| BREACH | 9001 | kill-all | — | — | 0 | 64 / 44 / 37 | 46 |

One-hit-away is the exact integrated time when the minimum **sum of remaining Integrity** over any ingress-to-critical physical path equals one. It is computed from damage-event boundaries, not one-second topology samples. It differs from the parent’s count of nodes still needing compromise.

Eligible means a legal, uncompromised candidate exists (on the offensive path for BREACH). Preferred includes selections already matching the ambient target; changed counts actual target changes. Fallback counts every decision that did not enter the preferred branch, including unsupported activities and the ambient probability branch. SPREAD has no preferred branch.

`metrics.csv` contains every run’s target distribution, per-node/per-source damage, fallback reasons, commands and horizon. `damage.csv` retains each causal strike. `clock-category-comparison.csv` compares each common observed prefix without requiring runs with different loss times to have equal lengths. `transit-target-tell.csv` separates direct WEB targets from benign WEB transits.
