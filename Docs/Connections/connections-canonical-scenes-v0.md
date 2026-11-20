# Connections — Canonical Scenes V0

Integration contract, November 20, 2026. Based on #118 at `e551207c30236cfeb0dd95e7a5a6ce5ab0eb52df`, stacked through #117/#116; tracks [#119](https://github.com/abeering/uptime-game/issues/119). Living Shift #115 is excluded. All PRs remain unmerged.

## Supported gameplay scenes

| Scene | Content/topology | Default intensity |
|---|---|---|
| Small: `Assets/Scenes/Connections.unity` | EXT1/API/DB/AUTH, three links; six existing PURPOSEs serialized with original API/DB/AUTH destinations | 54s mean hostile interval, benign goal3 |
| Advanced: `Assets/Scenes/ConnectionsAdvanced.unity` | Eight nodes/seven links; EXT1/EXT2 ingress 60/40; original six PURPOSEs/destination overrides plus complete storefront WEB recipe | 30s interval, goal4 |

These are the default development, manual-play and future feature-test targets. Existing GUIDs, topology, positions and ordinary layout remain. Small retains c1/c2/c3 and Generated default. Its previous runtime fallback becomes an explicit catalog; Advanced's nonexistent WEB/JOBS/RPT references are not imported. Advanced keeps inventory at JOBS and sales export at RPT. Every internal node has legitimate destination cover and can receive hostiles through the same pipeline. No Small-specific simulation/generator is added.

## Shared mechanics and tuning

Both enable ordinary hostiles, reconnaissance, standalone Payloads, Worms, launchers, direct Duels and carrier Duels, weighted **4/2/2/1/1/1/1**. All five infection-kind weights are 1. Small's previous ambient exclusions are removed. Existing LOADER/INFECTOR objectives, operations, logs, anomalies, keywords, effects and queue/lifecycle remain. Small's API launcher to adjacent DB is legal without WEB. Existing propagation and target validation handle smaller topologies and fewer eligible burst destinations.

Both have PROBE/interception/isolation/overclock, asynchronous scan, PID CLEAN, identity behaviors, Duels and frozen Surprise plans. Commands/input/overlays are retained. Existing Inspector controls remain authoritative for interval, activity weights, population/coverage, objective/infection/anomaly and defense parameters. Low intensity uses interval/population instead of zeroing families. Eight seconds is an explicit high-pressure test setting, not canonical Advanced tuning; no preset framework is added.

Both use unchanged permanent **2/2 Integrity**: accepted infection, sustained requested LOAD above 150% effective capacity for ten seconds, and lost-Duel strikes. Knowledge/reveal rules, immediate Duel consequence suppression, zero clamp, no operational penalty at zero and CLEAN without repair remain. DB/AUTH are critical. The same physical graph model latches CORE BREACHED once, freezes simulation and supports same-scene/seed `restart`.

Both expose SPREAD/BREACH, authored SPREAD / 60s commitment / 0.7 preference. The shared selector and isolated RNG retain hidden intent internally. Parity means shared rules/availability, not identical outcomes or difficulty.

## Presentation and fixtures

Both use the same `IntegrityPresenter` bound to all cards/physical links, seven-row `NodeStatusIntegrity` prefab, infrastructure markers and authored CORE panel. Neutral unknown/known2, orange known1 and red0 retain the agreed language. Physical compromised/winning paths remain distinct from logical identity/performance colors. The existing clock/tools remain visible; Health stays diagnostic internally but its value/bar/rate are hidden. No WORK credit/quota, new titlebar, shift ending or runtime UI hierarchy generation is introduced.

`CanonicalSceneAuthoring.Promote` is an explicit one-time Editor recipe, refusing duplicate presentation and reusing `IntegritySceneAuthoring.ApplyToLoadedScene` for both topologies. Normal witnesses do not rebuild scenes. Older phase helpers retain historical scope and must not overwrite canonical assets.

Pressure and Integrity scenes remain unchanged **historical test/regression fixtures**, with their original evidence. They are no longer gameplay authority. Status Lab/Topology Stress remain specialized fixtures; Packet is unchanged. Dated contracts retain original scene exclusions as provenance; this contract supersedes current availability claims. Historical witnesses must use preserved fixtures/explicit old configuration when asserting old baselines.

## Validation

The [pre-edit matrix](../Review/ConnectionsCanonicalSceneParityV0/PARITY_AUDIT.md) distinguishes configuration, missing content/bindings and scenario differences. The [review guide](../Review/ConnectionsCanonicalSceneParityV0/README.md) records actual checks, manifest, natural evidence, graphical captures and manual steps. `CanonicalSceneWitness` checks serialized state and every family under both directives at low/high intensity, scan/CLEAN/defenses, hidden/revealed damage, a real physical breach and terminal restart. Natural runs separately use unforced mixed selection and actual lifecycle/damage. Graphical fixtures are constructed development evidence, not natural runs or human keyboard tests. Inherited failures remain explicit.
