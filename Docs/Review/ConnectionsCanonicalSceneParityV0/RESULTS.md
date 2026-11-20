# Natural canonical-scene checks

Sixteen unforced mixed-activity runs; seconds are simulation time. No breach means the run reached approximately 600s.

| Scene | Directive | Intensity | Seed | Breach s | Winning path | Benign / hostile plans |
|---|---|---|---:|---:|---|---:|
| Connections | SPREAD | authored | 1701 | 308.278 | EXT1>API>DB | 11 / 6 |
| Connections | SPREAD | authored | 3141 | — | — | 26 / 1 |
| Connections | SPREAD | high8s | 1701 | 126.051 | EXT1>API>DB | 5 / 12 |
| Connections | SPREAD | high8s | 3141 | 174.24 | EXT1>API>AUTH | 8 / 3 |
| Connections | BREACH | authored | 1701 | 376.692 | EXT1>API>DB | 14 / 8 |
| Connections | BREACH | authored | 3141 | 407.811 | EXT1>API>AUTH | 19 / 0 |
| Connections | BREACH | high8s | 1701 | 134.932 | EXT1>API>DB | 5 / 14 |
| Connections | BREACH | high8s | 3141 | 185.043 | EXT1>API>DB | 9 / 3 |
| ConnectionsAdvanced | SPREAD | authored | 1701 | 274.907 | EXT2>JOBS>API>DB | 15 / 10 |
| ConnectionsAdvanced | SPREAD | authored | 3141 | — | — | 31 / 3 |
| ConnectionsAdvanced | SPREAD | high8s | 1701 | 176.441 | EXT2>JOBS>API>DB | 9 / 22 |
| ConnectionsAdvanced | SPREAD | high8s | 3141 | 257.77 | EXT1>WEB>AUTH | 13 / 9 |
| ConnectionsAdvanced | BREACH | authored | 1701 | 285.781 | EXT1>WEB>AUTH | 17 / 10 |
| ConnectionsAdvanced | BREACH | authored | 3141 | 573.688 | EXT1>WEB>AUTH | 29 / 2 |
| ConnectionsAdvanced | BREACH | high8s | 1701 | 136.132 | EXT1>WEB>AUTH | 8 / 14 |
| ConnectionsAdvanced | BREACH | high8s | 3141 | 276.272 | EXT1>WEB>API>DB | 13 / 11 |

Authored means Small 54s / goal3 or Advanced 30s / goal4; high8s changes only the existing hostile-interval setting. All family weights remain positive. Counts are constructed connection plans, including secondary follow-ups/children, not standalone Payload/Worm/Duel counts. `natural-activities.csv` separates selected categories from successful production. A short run can legitimately contain no ordinary hostile connection despite real non-connection attacks.

All seven families actually produce in each scene across these runs. Every internal node receives legitimate benign work. See `natural-metrics.csv` for targets, objectives and influenced selections; complete compressed recorder logs and one-second graph samples remain in each scene/directive/intensity folder. Four exact seed1701 authored-mode replays are recorded separately. This is integration evidence, not a balance or directive-effectiveness verdict.
