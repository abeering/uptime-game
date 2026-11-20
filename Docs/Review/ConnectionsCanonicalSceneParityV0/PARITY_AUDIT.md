# Canonical scene audit before editing

Baseline: `e551207c30236cfeb0dd95e7a5a6ce5ab0eb52df`, integration branch `feat/connections-canonical-scene-parity-v0`, November 20, 2026. Connections implementation only. Read actual serialized host/director/component bindings, prefab references, runtime ownership and current contracts; component presence alone is not treated as availability. Tracking [#119](https://github.com/abeering/uptime-game/issues/119). Living Shift #115 excluded.

**A** available with existing serialized bindings/content; **C** implemented but disabled in configuration; **M** missing scene component/presentation binding; **T** missing authored traffic content; **S** intentional scenario/tuning difference. This is the pre-edit matrix, not a claim about the final scenes.

| Feature | Small / Connections | Advanced | Pressure fixture | Integrity fixture |
|---|---|---|---|---|
| Topology / ingress | S: EXT1/API/DB/AUTH, 3 links, EXT1 only | S: 8 nodes, 7 links, EXT1/EXT2 60/40 | Same Advanced geometry | Same Advanced geometry |
| Generated benign PURPOSEs | A at runtime; T: `profiles: []` falls back to six built-ins | A: six authored templates | A: six authored templates | A: seven, including storefront |
| Legitimate destination coverage | A: API/DB/AUTH via fallback | T: no legitimate WEB destination; other internal nodes covered | Same WEB gap | A: every internal node covered |
| LOADER/INFECTOR/anomalies/keywords/logs | A: existing weighted PURPOSE pipeline and UI | A | A | A |
| Recon / standalone Payload / Worm / launcher | C: ambient weights all zero; construction and presenter/tool bindings exist | A: weights 2/2/1/1 | A: 2/2/2/2 | A: 2/2/2/2 |
| Direct / carrier Duel | C: both ambient weights zero; overlay/director and manual path exist | A: 1/1, overlay and director bound | A: 1/1 | A: 1/1 |
| Infection kinds | C: Payload Spawner and Backdoor weights zero; other three positive | A: all five positive | A | A |
| Surprise consequences | A: shared runtime planner/executor owned by Duel director; no separate scene planner required | A | A | A |
| PROBE / interception / isolation | A: host, Payload/Worm directors, path/presenter/terminal and readiness bindings | A | A | A |
| Async scan / PID CLEAN / overlays | A: terminal, prefab, overlay and host bindings | A | A | A |
| Two-point Integrity / hidden knowledge / overload strikes | C: host runtime default disabled | C | C | A: enabled, 150%/10s |
| Integrity card/physical-link/core ending presentation | M: no IntegrityPresenter, node/link bindings, markers or breach panel | M | M | A: bound presenter, authored markers/result panel |
| Overload status row | M for Integrity: original six-row status prefab | M: same six-row prefab | M | A: seven-row Integrity prefab |
| SPREAD / BREACH | M: no HackerDirectives component | M | M | A: SPREAD, 60s, 0.7; host bound |
| Visible survival strip | S: Health value/bar/rate + existing clock/tools | Same | Same | Health value/bar/rate hidden; same clock/tools retained |
| Default pressure / benign goal | S: 54s hostile mean, goal 3, ordinary weight 1 | S: 30s, goal 4, 4/2/2/1/1/1/1 | S: 8s, goal 4, 6/2/2/2/2/1/1 | Same Pressure mix |

Promotion decisions: preserve both canonical scene GUIDs/topologies/layouts. Reuse the same Integrity authoring/runtime/prefab path in both. Serialize Small's existing six built-ins with legal original API/DB/AUTH destinations; add only the complete storefront template to Advanced. Use Advanced's existing positive category mix in both, enable all five infection kinds, preserve Small 54s/goal3 and Advanced 30s/goal4. No new preset architecture. Historical Pressure/Integrity assets and recorded evidence remain intact; new parity checks become authority for canonical configuration. Old tests with explicitly superseded scene assumptions must be identified and updated deliberately, retaining their functional assertions or preserving their historical fixture.
