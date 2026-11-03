# Connections Reactive Identity Misdirection V0

**Scope:** Connections implementation only. A manual hostile connection can advertise one stable false ID and replace it only after a failed `inspect` or `kill` against that currently advertised ID. Its immutable canonical ID remains the simulation key and is always a valid command target. The behavior does not change operations, LOAD, WORK, routing, or Health.

`spawn basic-threat idreact` queues the manual fixed hostile profile with this behavior. `idhop` remains available; the two options cannot be combined. Purpose templates, benign profiles, and ambient traffic do not select reactive identity. The concrete plan and recorder definition carry the explicit identity mode and a deterministic alias seed derived separately from ordinary traffic generation.

**Later generation addition:** [Generated Identity Behaviors V0](connections-generated-identity-v0.md) now permits an ordinary ambient hostile LOADER or INFECTOR connection to select reactive misdirection. The exclusion above records the original Reactive Identity V0 scope; developer spawns and other hostile-activity categories still do not draw ambient identity modes.

The existing 260-ID seeded alias pool and collision checks select an initial false label when Connecting begins. They reject used or queued canonical IDs and other live aliases. If no fresh alias exists, the current legal alias remains; if that also conflicts, the true ID is shown. Normal command resolution runs first. A failed address to the currently visible reactive alias causes one immediate alias replacement, while the ordinary failure message remains. Unknown and stale IDs, duplicate skipped targets, successful canonical commands, and concealed aliases cause no reaction. A multi-ID `kill` continues its existing per-target processing. A multi-ID `inspect` still stops at its first unknown ID.

Probe temporarily shows the true ID once the connection has a visible segment on the selected edge. The hidden alias stays fixed, and a failed command using it cannot trigger a change during revelation. At Probe expiry that same alias returns. Network pills and live LOAD/WORK rows reuse their existing objects and subtle ID-change emphasis. The recorder's `identity_alias_changed` event includes canonical ID, previous and replacement aliases, attempted ID, triggering command, simulation time, and the failed player-action sequence when recording is active. Recorder evidence can show true IDs; ordinary failure text does not identify the behavior.

## Review

Run the Reactive Identity, ID Hopping, Probe, Probe Sweep, Inspect V2, LOAD Output, WORK Output, Traffic, Geometry, and Recorder witnesses. In both Connections scenes at 1280×720 and 1920×1080, use `spawn basic-threat idreact` on sparse and crowded routes. Try the displayed alias with `kill` and `inspect`, then repeat the stale alias. Probe an occupied edge, test the concealed alias, wait for expiry, and use the true ID for inspection and kill. Watch pill and live panel text and verify terminal focus and error wording.

The [Editor captures](../Review/ConnectionsReactiveIdentityV0/README.md) show stable, reacted, and Probe-revealed states at both resolutions in both scenes; they do not establish live motion or input focus.
