# Connections — Network Health V0

**Lifecycle V1 update (October 25, 2026):** Health is connection-only. Blackout no longer has an outage drain or direct application penalty. Connecting attempts have no service deadline or per-second service impact. Established legitimate connections retain the slowdown and overdue rules below. A benign Killed attempt costs 10 and a benign Canceled attempt costs a provisional 5, once per resolution, whether it was Connecting or Active. Hostile terminations cost zero. Recovery can continue while Blackout remains active if no legitimate service is degrading or being canceled. See [Lifecycle V1](connections-lifecycle-v1.md) for route timing and receipts.

**Scope:** Connections implementation only. This is a provisional, live service instrument for the current small scenario. Packet FLOW and Packet scoring are separate. Performance, human notifications, and run endings remain future designs.

Network Health starts at 100 and stays within 0–100. The simulation host updates it once per fixed step from the allocator's actual grants. Only legitimate active connections contribute service damage. A connection's fixed deadline is set at activation from the sum of each operation's authored WORK / authored LOAD, multiplied by a tolerance. Later behavior modifiers do not move the deadline. Before its deadline, a legitimate connection below the substantial slowdown threshold contributes drain proportional to its LOAD shortfall. After its deadline, a larger drain replaces the slowdown drain and rises with time overdue. Passive recovery occurs only when no degradation is present; it never offsets concurrent drain. Killing a legitimate connection applies one immediate penalty. A hostile connection does not contribute direct service or kill damage, but its LOAD can indirectly reduce legitimate grants. An infection-delivery occurrence alone has no Health effect.

The simulation retains each connection's deadline, nominal duration, whether it was substantially slowed or late, existing resolution reason, and resolution timestamp. Slowed and overdue counts include all active connections, including hostile ones; resolution removes them from current counts while preserving lifecycle history. Completion applies no second duration penalty. A zero value latches `Failed`; this phase does not end a run. The read-only snapshot exposes value, actual signed net rate, recovery and degradation components, slowed/overdue counts, legitimate impact evidence for internal consumers, recent discrete events, and failure state. The compact panel shows the signed rate beside the bar. Its expanded view shows clock, Health, state and signed rate, slowed count, overdue count, peak requested/capacity node LOAD pressure, and recent discrete events. It presents aggregate conditions and anonymous service interruption events so it does not reveal hidden hostile intent through per-ID attribution.

## Provisional Inspector tuning

| Parameter | Initial value |
|---|---:|
| Deadline tolerance | 1.5 × nominal plan duration |
| Shortfall drain | 0.25 Health/s at full LOAD shortfall, per substantially slowed legitimate connection |
| Overdue drain | 0.50 Health/s per overdue legitimate connection |
| Overdue escalation | +0.02 Health/s per additional second overdue |
| Recovery | +0.35 Health/s when below 100 and no degradation is present |
| Incorrect legitimate kill | −10 Health once |
| Substantial slowdown threshold | satisfaction below 0.98 |

The 24-hour clock is display-only. Its serialized start is 09:00, end is 17:00, and mapping is one clock minute per simulation second. The end time is a configuration marker for later shift design; it does not trigger progression or evaluation. A future design may tint the clock for early-, middle-, and late-shift pacing, but V0 keeps neutral styling.

At 16:9, review generated baseline traffic, `demandtest`, `spawn basic-threat`, `spawn delayed-threat`, `spawn spiking-benign`, legitimate and hostile `kill`, recovery after congestion clears, and both TAB layouts. Check the compact signed rate and colors, expanded aggregate counts and peak LOAD label, newest short history just above the prompt, top clipping on expansion, preserved browsing position with long history, command text/caret/focus, `inspect` cards, crowded labels, and the unchanged right-side topology. Unity Editor witness: `Tools/Connections/Run Network Health Witness`.
