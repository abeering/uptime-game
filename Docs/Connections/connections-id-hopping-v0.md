# Connections ID Hopping V0

**Scope:** Connections implementation only. ID Hopping is an optional hostile connection identity behavior. It changes what is advertised, never the canonical `ConnectionId` used by simulation, command lookup, lane ordering, or recording. The [earlier Probe working design](connections-probe-xray-design.md) proposed temporary addressability; this implementation instead follows the later decision that information does not grant command permission. A player may always `inspect` or `kill` a guessed or remembered true ID.

The developer command `spawn basic-threat idhop` queues a long-lived manual hostile connection in either Connections gameplay scene. The `idhop` option is limited to fixed hostile profiles; ordinary purpose generation and ambient hostile generation do not select it. Each scene serializes the default two-simulation-second interval on its traffic director. The queued plan and recorder definition carry the interval and a deterministic alias seed derived from the run seed and canonical ID without consuming traffic random draws.

**Later generation addition:** [Generated Identity Behaviors V0](connections-generated-identity-v0.md) now permits an ordinary ambient hostile LOADER or INFECTOR connection to select hopping, while retaining this manual command and its runtime rules. The manual-only statement above records the original ID Hopping V0 scope.

When the connection enters Connecting, it advertises a false, short lowercase letter-digit alias. The simulation clock changes that alias every two seconds across establishment and operation transitions. Alias selection scans a seeded shuffle of the 260 ordinary ID shapes, excluding every used or queued canonical ID and aliases held by other live hoppers. Generated canonical IDs skip live aliases. If no fresh alias is available, the current legal alias is retained; if even that is unavailable, the true ID is shown rather than a colliding false label. No LOAD, WORK, route, lifecycle, infection, or Health data changes.

One `KnownIdentity` property on the connection state feeds every network identity pill and live LOAD/WORK row. Existing pill and row objects stay bound to the canonical connection and update their text in place. A short, mild outline accent accompanies a text change. Successful `inspect <trueId>` output retains the verified true ID, including after Probe expires; false aliases return the ordinary unknown or inactive command result. Developer spawn receipts and recorder diagnostics retain canonical IDs.

Probe temporarily displays the true ID everywhere for a hopping connection that is visibly represented on its selected physical edge. A later arrival is revealed only once its visible path reaches that edge. The underlying alias clock continues while revealed; expiry restores its current alias at the simulation boundary, even while the scanner rectangle performs its existing cosmetic fade. Reset clears all runtime aliases and revelation. Probe does not alter command authorization or its red/gray X-ray treatment.

The recorder stores the behavior interval and seed with each queued/spawned definition and emits `identity_alias_changed` events with canonical ID, selected alias, hop index, and simulation timestamp. These are internal run evidence, not ordinary inspection output.

## Review

Run ID Hopping, Probe, Probe Sweep, Geometry, Inspect V2, LOAD Output, WORK Output, Traffic, and Recorder witnesses. In Play Mode, review both scenes at 1280×720 and 1920×1080: use `spawn basic-threat idhop`, watch several hops on sparse and crowded paths, open `load db` and `work db`, Probe an edge on the route, then test alias and true-ID commands before and after expiry. Check that the small text accent is visible without distracting from traffic and that terminal focus remains stable.

The [Editor preview index](../Review/ConnectionsIdHoppingV0/README.md) shows before, during, and after Probe in both scenes; it does not replace interactive Play Mode tuning.
