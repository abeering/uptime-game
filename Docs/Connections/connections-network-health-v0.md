# Connections — Network Health V0

**Scope:** Connections implementation only. This is a provisional, live service instrument for the current small scenario. Packet FLOW and Packet scoring are separate. Performance, human notifications, and run endings remain future designs.

Network Health starts at 100 and stays within 0–100. The simulation host updates it once per fixed step from the allocator's actual grants. Only legitimate active connections contribute service damage. A connection's fixed deadline is set at activation from the sum of each operation's authored WORK / authored LOAD, multiplied by a tolerance. Later behavior modifiers do not move the deadline. A connection whose LOAD is short contributes a small drain proportional to its shortfall. After its deadline, a larger drain replaces the slowdown drain and rises with time overdue. Recovery is a separate positive rate; it can offset concurrent drain. Killing a legitimate connection applies one immediate penalty. A hostile connection does not contribute direct service or kill damage, but its LOAD can indirectly reduce legitimate grants. An infection-delivery occurrence alone has no Health effect.

The simulation retains each connection's deadline, nominal duration, whether it was substantially slowed or late, existing resolution reason, and resolution timestamp. Completion applies no second duration penalty. A zero value latches `Failed`; this phase does not end a run. The read-only snapshot exposes value, actual net rate, recovery and degradation components, slowed/overdue counts, individual impact evidence for internal consumers, recent discrete events, and failure state. The visible panel presents aggregate causes and anonymous service interruption events so it does not reveal hidden hostile intent through per-ID attribution.

## Provisional Inspector tuning

| Parameter | Initial value |
|---|---:|
| Deadline tolerance | 1.5 × nominal plan duration |
| Shortfall drain | 0.025 Health/s at full LOAD shortfall, per legitimate connection |
| Overdue drain | 0.12 Health/s per overdue connection |
| Overdue escalation | +0.006 Health/s per additional second overdue |
| Recovery | +0.02 Health/s when below 100 |
| Incorrect legitimate kill | −5 Health once |
| Substantial slowdown threshold | satisfaction below 0.98 |

The 24-hour clock is display-only. Its serialized start is 09:00, end is 17:00, and mapping is one clock minute per simulation second. The end time is a configuration marker for later shift design; it does not trigger progression or evaluation. A future design may tint the clock for early-, middle-, and late-shift pacing, but V0 keeps neutral styling.

At 16:9, review generated baseline traffic, `demandtest`, `spawn basic-threat`, `spawn delayed-threat`, `spawn spiking-benign`, legitimate and hostile `kill`, recovery after congestion clears, and both TAB layouts. Check command text, caret, focus, scroll history, `inspect` output, crowded labels, and the unchanged right-side topology. Unity Editor witness: `Tools/Connections/Run Network Health Witness`.
