# Video Call & Escape Coordination V0 review

Connections-only implementation based on `main` at `3dd3813` (merged PR #71). One authored 560×360 call window sits at NetworkPanel's upper right, using actual nested instances of the shared 40-unit titlebar and 2-unit perimeter frame. The source chrome prefabs are unchanged. Packet's static texture is reused by reference as the sample fallback; its scripts, prefabs, scenes, and import settings are unchanged. No new person portrait or animation asset is claimed.

The sample is explicitly `TEST OPERATOR` / `DEVELOPER TEST CALL`, not an inferred network incident. Runtime caller data accepts a real sprite and optional Animator controller, tested separately. Calls type subtitles at 45 characters per unscaled second, follow wrapped subtitle pages, hang up immediately even during playback, and never take typing focus. The call persists after dialogue until Escape or `EndCall`; duplicate starts do not replace it. CLEAN and calls now share one LIFO Escape authority, with only the current eligible target displaying its affordance.

## Captures

These are scripted Unity 6000.3.11f1 **Play Mode** renders of the authored Canvas through a temporary camera. The tests use real CLEAN sessions, command/PID listeners, native TMP events, and Input System keypresses. Ambient generation is disabled for controlled checks; ordinary simulation stepping continues except in an explicitly isolated timeScale-zero protection test. This is automated presentation evidence, not a physical-keyboard player session. Full call text is present in its data and revealed over time; early captures intentionally show partial subtitles.

| Scene | State | 1920×1080 | 1280×720 |
| --- | --- | --- | --- |
| Connections | Call alone, terminal draft retained | [Capture](Connections-call-alone-1920x1080.png) | [Capture](Connections-call-alone-1280x720.png) |
| Connections | CLEAN alone | [Capture](Connections-clean-alone-1920x1080.png) | [Capture](Connections-clean-alone-1280x720.png) |
| Connections | Call above CLEAN, call owns Escape | [Capture](Connections-call-over-clean-1920x1080.png) | [Capture](Connections-call-over-clean-1280x720.png) |
| Connections | CLEAN above call, CLEAN owns Escape | [Capture](Connections-clean-over-call-1920x1080.png) | [Capture](Connections-clean-over-call-1280x720.png) |
| ConnectionsAdvanced | Call alone, terminal draft retained | [Capture](ConnectionsAdvanced-call-alone-1920x1080.png) | [Capture](ConnectionsAdvanced-call-alone-1280x720.png) |
| ConnectionsAdvanced | CLEAN alone | [Capture](ConnectionsAdvanced-clean-alone-1920x1080.png) | [Capture](ConnectionsAdvanced-clean-alone-1280x720.png) |
| ConnectionsAdvanced | Call above CLEAN, call owns Escape | [Capture](ConnectionsAdvanced-call-over-clean-1920x1080.png) | [Capture](ConnectionsAdvanced-call-over-clean-1280x720.png) |
| ConnectionsAdvanced | CLEAN above call, CLEAN owns Escape | [Capture](ConnectionsAdvanced-clean-over-call-1920x1080.png) | [Capture](ConnectionsAdvanced-clean-over-call-1280x720.png) |

Fractional 1577×887 overlap evidence: [Connections, call target](Connections-call-over-clean-1577x887.png), [Connections, CLEAN target](Connections-clean-over-call-1577x887.png), [Advanced, call target](ConnectionsAdvanced-call-over-clean-1577x887.png), [Advanced, CLEAN target](ConnectionsAdvanced-clean-over-call-1577x887.png).

Noncancelable results with the call owning Escape: [Connections](Connections-clean-result-with-call-1920x1080.png), [Advanced](ConnectionsAdvanced-clean-result-with-call-1920x1080.png). The result window has no CANCEL indicator. It retains its short existing result hold while the call can still be hung up.

All five states were rendered in both scenes at all three sizes; 22 selected PNGs are retained. Later windows draw in front; occluding part of the network or an older window is expected. The active indicator remains visible at the authored positions. The keyboard prompt, permanent filled pins, global Health strip, and topology geometry remain in place.

## Actual validation

Unity **6000.3.11f1** compiled the final source in an isolated project copy. Passed Editor witnesses: CLEAN Window, Infection, Network Health, Inspect V2, Resolved Inspect, and Advanced Scene. Both scenes also passed static call/coordinator/adapter bindings, actual nested chrome provenance, and absence of selectable/raycast call controls. Infection retains PID outcomes, exact five-Health innocent penalty, captured-instance removal/provenance, later-arrival protection, and normal simulation behavior. Inspect/history checks retain scrolling, bottom-follow and input preservation.

The committed `EscapeVideoCallWitness` passed separately in **both scenes**:

- Partial subtitle progress and simulation-clock advance; immediate hang-up during typing; no duplicate/null-call replacement.
- Terminal and PID focus, EventSystem selection, draft and selection preservation during unrelated ownership changes. Tests inject both native IMGUI Escape into TMP and virtual Input System Escape/UI cancellation. This caught TMP's independent `OnCancel` path as well as its native editing cancellation; both are now reserved by the narrow input adapter.
- Native character replacement, arrow/Backspace editing, Enter submission, and terminal focus restoration.
- Both LIFO opening orders, one visible active indicator, one dismissal per fresh press, and no held-key cascade.
- Appearance/removal protection, consumed protected presses, no fallthrough or replay after grace expiry, re-enable while held, and unscaled expiry at timeScale zero.
- CLEAN result deregistration, successful removal, noncancelable results, and normal result/focus completion.
- Explicit/idempotent call end, component and GameObject disable, destruction, external CLEAN disable, and host-reset cleanup.
- Dynamic caller/sprite binding, an in-memory test Animator controller's `IsTalking` start/stop, complete subtitles, long-dialogue page following, and ellipsized long caller names.
- Network pane geometry and all node positions remain identical before and after the lifecycle cases.

The [validation excerpts](validation.txt) retain final outputs. Tests and capture entry points are in `Assets/Editor/EscapeVideoCallWitness.cs`; no temporary authoring/batch driver is committed. PNG integrity/dimensions, shared chrome and Packet byte comparisons, Project Core freshness, and `git diff --check` were also checked.

The isolated Editor logged a `SearchDatabase.EnumerateAll` startup indexing `ArgumentOutOfRangeException` on Play launches, as in previous reviews. The gameplay witnesses continued and passed. Physical-keyboard delivery, IME/OS-specific editing, actual Game view resizing, and readability under ordinary generated traffic still require manual review. The sample static feed is test content; actual caller art/animation must be supplied by future authored content.

## Precise manual Play Mode review

1. In Unity 6000.3.11f1, open each Connections scene at 1920×1080, 1280×720, and a fractional size such as 1577×887. Keep ordinary generation enabled. Enter `calltest`; confirm caller identity, static fallback, typed subtitles, shared chrome, `[ESC] HANG UP`, continuing traffic/WORK/clock, and no topology reflow. Type/edit a partial network command; physical Escape must hang up immediately during typing without changing its draft, selection, or focus. A held Escape must not perform another action.
2. **CLEAN then call:** enter `infect DB phantom-load`, then `clean DB`. During selection, type part of a PID. Invoke the VideoCall component's **Start Developer Sample Call** context menu (or its public `TryStartSampleCall` entry point) while the PID field owns typing. Return OS keyboard focus to the Game view if needed. Only the call should display an active Escape label; PID typing ownership must stay with CLEAN. After the short arrival guard, first Escape hangs up; releasing and pressing again cancels CLEAN. Check no PID draft reset or focus loss.
3. **Call then CLEAN:** enter `calltest`, then `clean DB`. Only CLEAN shows `[ESC] CANCEL`. First valid Escape cancels CLEAN and restores command typing; the next fresh press hangs up the still-active call. Also try starting/removing a call immediately before pressing Escape: a protected press must do nothing and must not replay after being held through the interval.
4. With both windows present, complete CLEAN with the high-CPU PID. Its result must hide CANCEL and stay noncancelable for the existing hold; the call may own HANG UP. Verify invalid PID retry, an innocent PID's existing five-Health penalty, terminal feedback/focus on completion, and no interference with inspection/history scrolling or recall.
5. Inspect both orderings at all sizes. Check complete frame edges on the foreground window, readable subtitles/title/keycap, older-window occlusion, permanent filled pins, and stable window positions. There must be no clickable call control, manual target cycling, pause, or menu.
