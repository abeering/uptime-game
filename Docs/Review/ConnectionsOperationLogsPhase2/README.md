# Operation Logs V0 Phase 2 review evidence

Branch: `codex/connections-operation-logs-v0-phase2`.

## Editor validation

Unity 6000.3.11f1 ran in batch mode against an isolated copy of the project.
`LogsOutputWitness` passed, including both scene bindings, parsing boundaries,
pending/connecting/empty cards, same-step bursts, independent rolling tails,
READ-to-WRITE retention, canonical and historical command lookup, all four
resolution outcomes, reset invalidation, and recorder/simulation isolation.
The presentation follow-up additionally checks the verified pill through
alias hopping and Probe expiry, alias rejection, immediate resolved colors,
the four-second collapse for live and historical requests, the resizing clip
boundary, larger row typography, narrow two-line rows, and tail 5/15/20 sizing.

The following existing witnesses also passed in the same isolated Editor run:
Operation Logs, Inspect V2, Resolved Inspect, WORK Output, LOAD Output,
Behavior, Recorder, BACKDOOR, and ID Hopping. The Recorder witness's expected
catalog count was updated from 19 to 20 because `logs-demo` is now a fixed
zero-weight profile; it also checks the profile remains non-ambient.

The earlier Resource Witness inspect-pill geometry/color assertion was
reproduced on unchanged `main` during Phase 1 review. It was not rerun or
changed in this phase.

## Unity Editor previews

These are 1920×1080 Unity Editor renders of the actual authored LOGS prefabs,
bound to a deterministic connection in `Connections.unity`. The surrounding
terminal column is a review canvas, not an interactive Play Mode capture.

- [Active tail 5](active-tail-5-1920x1080.png)
- [Active tail 15](active-tail-15-1920x1080.png)
- [Active narrow width](active-narrow-1920x1080.png)
- [Resolved muted hold](resolved-muted-1920x1080.png)
- [Collapsed summary](resolved-summary-1920x1080.png)

## Manual Play Mode review still needed

Open each of `Assets/Scenes/Connections.unity` and
`Assets/Scenes/ConnectionsAdvanced.unity` at 16:9. Run `spawn logs-demo`, then
`logs <returned-id>` and `logs -n 15 <returned-id>` while the connection is
active. Inspect the initial burst, quiet interval, operation change, and final
tail. Compare two cards for the same ID. Check `inspect`, `load`, and `work`
remain legible alongside them; test input focus, command recall, bottom-follow,
and manual scrollback. Kill or complete a demo connection, observe immediate
greying, the four-second hold, clipping, and compact summary. Request a fresh
historical view and confirm that it also folds after its four-second hold.
Reset and confirm old cards say `RUN RESET` rather than following a reused ID.

The witnesses ran headlessly, while the five Editor previews used Unity's
graphics renderer. Caret behavior and scroll feel require interactive review.
