# Maximum Pressure V0 — measured results

Four seeds, 180 simulation seconds per run. U = unattended; L = limited visible-information intervention. Raw recorder JSON (gzip) and per-second samples are in `Runs/`. Counts include children and Duel consequences unless labeled opportunity selections.

| Scene | Seed | Policy | Selected / produced | Hostile established / resolved | Infections | Payloads / Worms | Duel attempts / accepted / rejected | Benign slowed samples / canceled | Health min / final |
|---|---:|:---:|---:|---:|---:|---:|---:|---:|---:|
| baseline | 1701 | L | 5 / 5 | 7 / 3 | 1 | 0 / 0 | 0 / 0 / 0 | 4 / 0 | 99.74 / 100.00 |
| baseline | 1701 | U | 5 / 5 | 7 / 3 | 1 | 0 / 0 | 0 / 0 / 0 | 6 / 0 | 99.74 / 100.00 |
| baseline | 1702 | L | 4 / 4 | 2 / 2 | 0 | 2 / 0 | 0 / 0 / 0 | 22 / 0 | 99.16 / 100.00 |
| baseline | 1702 | U | 4 / 4 | 2 / 2 | 1 | 2 / 0 | 0 / 0 / 0 | 67 / 0 | 97.86 / 97.86 |
| baseline | 3141 | L | 3 / 3 | 0 / 0 | 1 | 0 / 2 | 2 / 2 / 0 | 0 / 0 | 100.00 / 100.00 |
| baseline | 3141 | U | 3 / 3 | 0 / 0 | 1 | 0 / 2 | 2 / 2 / 0 | 0 / 0 | 100.00 / 100.00 |
| baseline | 9001 | L | 10 / 9 | 6 / 4 | 1 | 3 / 2 | 3 / 2 / 1 | 39 / 0 | 95.92 / 100.00 |
| baseline | 9001 | U | 10 / 9 | 6 / 4 | 1 | 3 / 2 | 3 / 2 / 1 | 73 / 0 | 76.09 / 76.09 |
| pressure | 1701 | L | 27 / 27 | 15 / 23 | 4 | 5 / 1 | 1 / 1 / 0 | 66 / 3 | 67.96 / 83.16 |
| pressure | 1701 | U | 27 / 27 | 18 / 23 | 5 | 5 / 1 | 1 / 1 / 0 | 82 / 4 | 39.75 / 49.23 |
| pressure | 1702 | L | 19 / 19 | 12 / 6 | 1 | 4 / 4 | 2 / 1 / 1 | 15 / 0 | 99.69 / 100.00 |
| pressure | 1702 | U | 19 / 19 | 12 / 5 | 3 | 4 / 4 | 2 / 1 / 1 | 53 / 0 | 96.12 / 96.12 |
| pressure | 3141 | L | 12 / 12 | 7 / 5 | 0 | 5 / 2 | 1 / 1 / 0 | 10 / 0 | 99.56 / 99.56 |
| pressure | 3141 | U | 12 / 12 | 7 / 5 | 4 | 5 / 2 | 1 / 1 / 0 | 138 / 0 | 38.44 / 38.44 |
| pressure | 9001 | L | 32 / 30 | 20 / 7 | 4 | 10 / 3 | 3 / 1 / 2 | 75 / 0 | 69.20 / 69.20 |
| pressure | 9001 | U | 32 / 30 | 20 / 6 | 5 | 10 / 3 | 3 / 1 / 2 | 97 / 0 | 39.46 / 39.46 |

Selected activity attempts / successful production (queued plans count as produced; establishment and completion are separate above). Both policies have identical selections for these seeds.

| Scene | Seed | Ordinary | Recon | Payload | Worm | Launcher | Direct Duel | Carrier Duel |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| baseline | 1701 | 3 / 3 | 2 / 2 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| baseline | 1702 | 1 / 1 | 1 / 1 | 2 / 2 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| baseline | 3141 | 0 / 0 | 0 / 0 | 0 / 0 | 1 / 1 | 0 / 0 | 2 / 2 | 0 / 0 |
| baseline | 9001 | 2 / 2 | 2 / 2 | 1 / 1 | 2 / 2 | 0 / 0 | 2 / 1 | 1 / 1 |
| pressure | 1701 | 14 / 14 | 4 / 4 | 3 / 3 | 1 / 1 | 4 / 4 | 1 / 1 | 0 / 0 |
| pressure | 1702 | 7 / 7 | 2 / 2 | 3 / 3 | 3 / 3 | 2 / 2 | 1 / 1 | 1 / 1 |
| pressure | 3141 | 2 / 2 | 2 / 2 | 3 / 3 | 2 / 2 | 2 / 2 | 0 / 0 | 1 / 1 |
| pressure | 9001 | 12 / 12 | 4 / 4 | 8 / 8 | 3 / 3 | 2 / 2 | 2 / 0 | 1 / 1 |

First mechanically live activity means an established hostile connection, created Payload/Worm, or accepted Duel countdown. This includes hidden intent. Overt samples look only for Worms, red-hop Payloads, Duel countdown/race, Conditions, offline Blackout, node overload, or legitimate service degradation. Neither column establishes a human-recognized incident.

| Scene | Seed | Policy | First mechanically live (s) | First overt sample (s) | No mechanical presence ≥10 s | No overt symptom ≥10 s |
|---|---:|:---:|---:|---:|---|---|
| baseline | 1701 | L | 23.9 | 93.02 | 1-22s | 1.0-93.0s; 113.0-134.0s; 169.0-180.0s |
| baseline | 1701 | U | 23.9 | 93.02 | 1-22s | 1.0-93.0s; 113.0-134.0s |
| baseline | 1702 | L | 4.5 | 45.0 | 81-102s; 141-180s | 1.0-45.0s; 51.0-75.0s; 81.0-104.0s; 109.0-120.0s; 140.0-180.0s |
| baseline | 1702 | U | 4.5 | 45.0 | none >=10s | 1.0-45.0s; 51.0-75.0s; 81.0-104.0s; 140.0-151.0s |
| baseline | 3141 | L | 73.06 | 74.02 | 1-73s | 1.0-74.0s |
| baseline | 3141 | U | 73.06 | 74.02 | 1-73s | 1.0-74.0s |
| baseline | 9001 | L | 10.26 | 11.0 | 1-10s | 1.0-11.0s |
| baseline | 9001 | U | 10.26 | 11.0 | 1-10s | 1.0-11.0s |
| pressure | 1701 | L | 9.46 | 34.0 | none >=10s | 1.0-34.0s |
| pressure | 1701 | U | 9.46 | 34.0 | none >=10s | 1.0-34.0s |
| pressure | 1702 | L | 3.4 | 15.0 | none >=10s | 1.0-15.0s |
| pressure | 1702 | U | 3.4 | 15.0 | none >=10s | 1.0-15.0s |
| pressure | 3141 | L | 22.88 | 37.0 | 1-21s | 1.0-37.0s; 57.0-79.0s |
| pressure | 3141 | U | 22.88 | 37.0 | 1-21s | 1.0-37.0s |
| pressure | 9001 | L | 4.94 | 5.0 | none >=10s | none >=10s |
| pressure | 9001 | U | 4.94 | 5.0 | none >=10s | none >=10s |

Unattended concurrency. Each tuple is **established hostile connections / moving Payloads / live Worm branches / infections / Conditions / pending-or-active Duel**. Do not add these into an actionability score. Connecting hostiles and benign counts are retained in `concurrency.csv` and the per-second files.

| Scene | Seed | 30 s | 60 s | 90 s | 120 s | 150 s | 180 s |
|---|---:|---|---|---|---|---|---|
| baseline | 1701 | 1/0/0/0/0/0 | 1/0/0/0/0/0 | 2/0/0/0/0/0 | 1/0/0/1/0/0 | 2/0/0/1/1/0 | 4/0/0/1/0/0 |
| baseline | 1702 | 0/1/0/0/0/0 | 0/1/0/1/0/0 | 0/0/0/1/0/0 | 1/0/0/1/1/0 | 0/0/0/1/0/0 | 0/0/0/1/0/0 |
| baseline | 3141 | 0/0/0/0/0/0 | 0/0/0/0/0/0 | 0/0/0/0/0/1 | 0/0/0/1/0/0 | 0/0/0/1/0/1 | 0/0/3/1/4/0 |
| baseline | 9001 | 0/0/1/0/2/0 | 1/0/2/0/3/0 | 3/0/1/0/3/0 | 2/0/0/0/2/1 | 3/1/0/1/0/1 | 2/2/0/1/1/0 |
| pressure | 1701 | 2/0/0/0/0/0 | 7/0/0/0/0/0 | 7/0/0/0/0/0 | 7/1/0/1/0/0 | 11/1/1/1/1/1 | 0/2/0/5/0/0 |
| pressure | 1702 | 0/1/1/0/0/0 | 2/0/2/1/3/1 | 2/0/1/2/2/0 | 5/0/0/2/2/0 | 6/0/1/2/2/0 | 7/1/1/3/0/0 |
| pressure | 3141 | 1/0/0/0/0/0 | 1/0/0/3/0/0 | 0/0/1/3/1/0 | 0/0/2/3/2/0 | 1/1/1/3/4/0 | 2/0/1/4/3/1 |
| pressure | 9001 | 3/0/1/0/3/0 | 4/2/1/0/4/1 | 6/1/2/3/4/0 | 8/4/1/3/3/0 | 9/2/0/4/0/0 | 14/0/0/5/0/0 |

`summary.json` retains infection kinds, resolution outcomes, branch counts, unique benign slowed IDs, Payload delivery/rejection, slot rejection reasons, and quiet-interval statistics for every run. `activity-counts.csv` and `concurrency.csv` are flat comparisons. One slowed sample means at least one established benign connection had satisfaction <0.98 at that one-second observation; it is not exact integrated slowdown duration. Minimum Health is observed every 0.02-second step; final values come from the last step. Overt intervals use the recorder’s approximately one-second snapshots; endpoints and all quiet intervals are sampled, not sub-step guarantees.
