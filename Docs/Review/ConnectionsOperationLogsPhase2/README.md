# Operation Logs V0 Phase 2 review evidence

Branch: `codex/connections-operation-logs-v0-phase2`.

## Editor validation

Unity 6000.3.11f1 ran in batch mode against an isolated copy of the project.
`LogsOutputWitness` passed, including both scene bindings, parsing boundaries,
pending/connecting/empty cards, same-step bursts, independent rolling tails,
READ-to-WRITE retention, canonical and historical command lookup, all four
resolution outcomes, summary timing, reset invalidation, narrow card width,
and recorder/simulation isolation.

The following existing witnesses also passed in the same isolated Editor run:
Operation Logs, Inspect V2, Resolved Inspect, WORK Output, LOAD Output,
Behavior, Recorder, BACKDOOR, and ID Hopping. The Recorder witness's expected
catalog count was updated from 19 to 20 because `logs-demo` is now a fixed
zero-weight profile; it also checks the profile remains non-ambient.

The earlier Resource Witness inspect-pill geometry/color assertion was
reproduced on unchanged `main` during Phase 1 review. It was not rerun or
changed in this phase.

## Manual Play Mode review still needed

Open each of `Assets/Scenes/Connections.unity` and
`Assets/Scenes/ConnectionsAdvanced.unity` at 16:9. Run `spawn logs-demo`, then
`logs <returned-id>` and `logs -n 20 <returned-id>` while the connection is
active. Inspect the initial burst, quiet interval, operation change, and final
tail. Compare two cards for the same ID. Check `inspect`, `load`, and `work`
remain legible alongside them; test input focus, command recall, bottom-follow,
and manual scrollback. Kill or complete a demo connection, observe the four
second hold and compact summary, then request a fresh historical snapshot.
Reset and confirm old cards say `RUN RESET` rather than following a reused ID.

The automated run used `-nographics`, so it produced no reliable 16:9 Editor
preview captures. Visual layout, caret behavior, and scroll feel require the
interactive review above.
