# Connections Ambient Population V1 — Play Mode review

![Seed 1701 at about 30 simulation seconds](play-mode-seed1701-30s-1458x820.png)

This 1458×820 (approximately 16:9) frame was captured from `Connections.unity` in rendered Unity 6000.3.11f1 **Editor Play Mode**, using an isolated project copy. Randomize Seed was off and Seed was 1701. After Play initialization, an Editor review script advanced the host by 300 fixed 0.1-second simulation steps, paused time, let the scene redraw, and captured the Game view. Six runtime logical connections were present at capture; several paths and identity labels are visible. The text “NO ACTIVE CONNECTIONS” in the network heading is static authored scene copy, not a live population counter.

This is an automated Play Mode visual check, not an interactive input, inspection, or prompt-focus test. Those interactions still need manual review in the Editor at 16:9. A batch-mode screenshot attempt produced an unrendered cyan frame and was discarded.

## Deterministic population comparison

The same four fixed seeds were run for 600 simulation seconds before and after this change, stepping by 0.2 seconds. Full-run metrics sample established benign connections after every step; opening metrics use the recorder's 1-second snapshots. No player commands were issued. The 120-second column shows the opening before long-lived hostile infections dominate some unattended runs; the full-run column includes those consequences.

| Seed | Mean benign, first 120s before → after | Mean benign, 600s before → after | Zero-benign time, 600s before → after | Extra benign plans | Ordinary hostile plans before → after |
|---|---:|---:|---:|---:|---:|
| 1701 | 0.98 → 2.76 | 0.20 → 0.55 | 86.6% → 84.4% | 17 | 13 → 13 |
| 456 | 0.81 → 2.03 | 0.72 → 1.08 | 46.1% → 36.9% | 14 | 10 → 10 |
| 90210 | 0.75 → 2.38 | 0.70 → 1.31 | 45.6% → 33.3% | 15 | 13 → 13 |
| 42 | 1.23 → 2.05 | 0.25 → 0.41 | 89.5% → 89.5% | 11 | 10 → 10 |

The controller raises the opening population without changing the ordinary hostile roll count. In long unattended runs, Blackout can keep routes unavailable; the controller backs off after benign cancellations and cannot maintain the soft goal in that condition. This is a four-node scenario tuning result, not a guarantee of three established connections at every moment.
