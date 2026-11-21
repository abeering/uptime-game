# Duel Concession V0 review

Connections implementation on `feat/connections-duel-concede-v0`, based exactly on `main` at `5174f9400270ee951d3031827c37327e3c3b7f3e`. Tracks [#119](https://github.com/abeering/uptime-game/issues/119), step B. Leave this PR draft and unmerged.

## Review the change

- `ConnectionsDuelDirector`: explicit encounter outcome and one shared resolution function for ordinary wins/losses and intentional concession. Claim Result before invoking consequence/Integrity callbacks; retain a local encounter through reentrant CORE cleanup. Concession requires the current Active encounter and an unfinished race.
- `ConnectionsDuelOverlay`: coordinated Escape concedes before immediate closure, writes a truthful receipt and cancels splash/result coroutines and text subscriptions. The authored `DuelOverlay.prefab` titlebar provides `ESC — CONCEDE` during startup/racing, hidden at results. Resolved results still consume Escape without another action. The existing coordinator's exclusive ownership, fresh-press rule and 0.15-second automatic-target protection are retained.
- `ConnectionsRunRecorder`: three encounter outcomes; real elapsed race time and player/hacker progress added alongside actual completion, mistake and correction fields. `CommandRaceSession` is unchanged. No invented hacker completion or race winner.
- `Terminal`, `ConnectionsCleanOverlay`, `EscapeSafeInputField`: report concession/Integrity and applied-or-blocked effects in two compact rows, restore the valid owner, preserve drafts/selection and drain native held-key repeats until release while focus returns normally. CLEAN and terminal selection restoration wait for TMP's deferred activation before applying the captured selection.

The frozen plan executes normally. Immediate infection Integrity suppression and the direct `duel:<id>` strike share ordinary loss accounting. Secondary Payload/Worm/Spawner activity remains independent. Rejected immediate infection does not prevent the direct strike. Reset, presentation disable and run termination remain cleanup paths without concession. No scene, balance, WORK, shift, new consequence or Packet changes.

## Automated validation

See [validation.txt](validation.txt), [deterministic checks](deterministic-checks.txt), [graphical checks](graphical-checks.txt), [Escape checks](escape-checks.txt), and [saved recorder extracts](recorder-outcomes.json) for actual results. Scripted graphical Play Mode uses rendered authored scenes and Input System keyboard state plus native TMP key events; it is not a physical keyboard playtest. The final four graphical runs pass 92 concession assertions and retain 20 focused frames; the existing Escape witness passes 219 checks across the two scenes. The existing Duel capture still covers normal win/loss displays, reference layout, splash timing and CLEAN restoration before the concession cases.

Reproduce with Unity 6000.3.11f1 in a disposable copy (legacy witnesses write into historical review folders):

```text
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.DuelConcessionWitness.Run -quit -logFile <focused.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.DuelConcessionWitness.Regressions -quit -logFile <regressions.log>
Unity -batchmode -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.DuelCapture.Run -duelScene Connections -duelWidth 1920 -duelHeight 1080 -duelConcession true -duelCapturePath <output> -logFile <play.log>
Unity -batchmode -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.EscapeVideoCallWitness.Run -witnessScene Connections -logFile <escape.log>
```

Repeat graphical commands for `ConnectionsAdvanced`; repeat Duel captures with 1280/720. Concession captures 14–18 show the hint, terminal receipt, active race over CLEAN, restored CLEAN and concession-triggered CORE loss. Deterministic checks compare ordinary loss/concession, attribution, JSON round trip, immediate/global slot release, duplicate infection, independent Payload delivery, pending/stale/repeated rejection, reset, disable, win and reentrant CORE breach.

Known inherited UI failures remain explicit: Resource inspect-header pill geometry/color and Overclock Advanced compact tool-row width. Do not relax these assertions or describe the aggregate regression result as all green. Historical parent reproduction is recorded in [Integrity validation](../ConnectionsIntegrityV0/validation.txt). Unity Search startup indexing can emit an Editor-internal `ArgumentOutOfRangeException`; distinguish it from gameplay check failures.

## Physical keyboard review (still manual)

1. In **each** canonical scene at 1920×1080 and 1280×720, run `duel auth 3 infection`. Escape during the pending countdown must not concede. Once startup opens, press a fresh Escape: the window closes immediately, one direct Integrity strike applies, the terminal reports concession and the slot is free. Respect the retained automatic-target press protection; a key held before opening is not a new press.
2. Run `duel api 0 breach`, wait for the race, type a prefix and several errors, Backspace once, then Escape. Check the same immediate return, ordinary CLEAN LOCKED/infection consequences and one direct strike. Compare normal hacker loss and player win; their usual result display remains unchanged. Escape during either resolved display must not apply a second loss or cancel CLEAN underneath.
3. Schedule `duel api 5 infection`, then start `clean db` on an infected DB. Keep a partial PID draft/selection. Concede the overlay; CLEAN must resume its captured session/draft/selection. Hold Escape and Backspace or a letter through handoff: neither draft nor CLEAN cancellation changes until release and fresh input. The next fresh Escape may cancel CLEAN. Repeat with CLEAN ending beneath the Duel; focus should return to terminal.
4. Preserve a terminal draft/selection through an unsolicited Duel. Test held Enter, letters, Backspace, repeated Escape, a Duel opening while Escape is held, rapid re-entry, and a call beneath CLEAN. Verify one exclusive Escape action per fresh press and no stray submits/characters. Try IME/native editing if used.
5. Reset/restart during startup and active typing; disable/re-enable the scene. Presentation must close without applying loss. Arrange a final critical-node strike during concession and separately an unrelated CORE breach during a Duel: CORE latches once, overlay closes, terminal accepts `restart`, and no second consequence appears.
6. Review 16:9 layout with crowded logical connection labels, distinguish physical compromise links, and use `inspect` after return to confirm scrolling and command-input focus. Review normal traffic as well as the scripted fixtures.

After merge, manually refresh the uploaded ChatGPT Project Core source from the regenerated file. No merge is performed by this implementation.
