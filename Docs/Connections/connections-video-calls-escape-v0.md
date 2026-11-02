# Connections — Video Calls & Escape Coordination V0

**Presentation follow-up:** [Presentation & Soft Placement V1](connections-video-call-presentation-v1.md) supersedes this PR #72 baseline's interior, fixed window homes and titlebar hang-up label. Call lifecycle, typewriter, animation and Escape/input rules below remain in effect. V1 adds bounded delayed developer samples. Historical review captures retain the V0 presentation.

**Implemented slice:** Connections only, based on `main` at `3dd3813` after PR #71. The [workspace design in PR #67](https://github.com/abeering/uptime-game/pull/67) remains the broader design reference; this slice implements only one reusable call surface and shared Escape coordination with CLEAN. Packet remains independent.

## Calls and content entry points

`ConnectionsVideoCall` is an authored, initially inactive scene window in NetworkPanel. It nests the existing `DockedTitlebar` and `DockedWindowFrame` prefabs. Its initial reference size is 560×360, roughly one-sixth of the network pane, anchored 24 units from the right and 64 below the pane top. Placement stays authored and stable; ordinary opening/closing does not resize or reposition the topology. A newly opened call or CLEAN window moves to the last sibling so the later window is visually in front.

A future director holds a serialized reference to that scene component and calls:

```csharp
bool opened = videoCall.TryStartCall(new ConnectionsVideoCallData {
    CallerName = "Caller identity",
    Body = "Subtitled dialogue.",
    Portrait = callerSprite,
    PortraitAnimation = optionalAnimatorController
});
// When the conversation ends programmatically:
videoCall.EndCall();
```

`TryStartCall` returns false for null data, an already active call, or disabled/unavailable presentation/coordinator dependencies. Duplicate requests preserve the current content and Escape order; there is no replacement, queue, or scheduling. Whitespace caller identity becomes `UNKNOWN CALLER`; null dialogue becomes empty. `EndCall` is idempotent and releases Escape ownership immediately. Disabling/destroying the window or its component also stops playback and releases ownership. Call opening and ending never select/deselect input fields, clear drafts, or change simulation time. Completed subtitles remain on screen until Escape or `EndCall`; there is no automatic lifetime.

The call independently adapts Packet's presentation ideas: unscaled, 45-character/second typewriter disclosure and an optional `IsTalking` Animator bool. Portrait sprites preserve aspect ratio. A supplied controller is bound to the portrait Image; if it has the configured bool, that bool is true during typing and false on completion/end. There is no audio, ringing, invitation, answer phase, or clickable close control. Long caller names ellipsize within the reserved title region; wrapped subtitles follow their current TMP page as disclosure advances, keeping long dialogue inside the compact window. No acknowledgment or dialogue choice is added.

Editor/development builds provide `calltest` and the component context menu **Start Developer Sample Call**. `TryStartSampleCall()` is also callable while PID-Select owns typing. The sample identifies itself as `TEST OPERATOR` and `DEVELOPER TEST CALL`, explicitly says it is not a network event, and uses the existing Packet static texture as a fallback feed. No authored person portrait/animation was present in Packet; actual portraits and optional animation are content supplied through the API. Call content is not attributed to simulated traffic or recorded as a fabricated network incident.

## Escape coordination

Each Connections scene authors one `EscapeCoordinator` on its Canvas. It owns Escape input and a small LIFO list of `IEscapeTarget` windows. The interface exposes eligibility, active-indicator binding, and dismissal; it does not expose typing focus, movement, or layout.

- Register once when a window becomes eligible. Duplicate registration does not reorder it. Deregister on ineligibility, close, disable, or destruction. Invalid/destroyed entries are pruned.
- Only the current eligible top target displays its active affordance: `[ESC] HANG UP` or `[ESC] CANCEL`.
- Input is handled after ordinary UI updates. One fresh press binds to at most one target. Holding Escape never cascades, including if a new target appears, a target ends, or the coordinator is re-enabled while held.
- Automatic target changes use an Inspector-configurable **0.15-second unscaled protection interval**, provisional V0 tuning. A press during protection is consumed; it never falls through to an older target or replays when the interval expires. The player must release and press again.
- A successful Escape dismissal immediately exposes the older eligible target without adding another automatic-change delay. It still requires a fresh press. Programmatic ending, eligibility loss, appearance, and re-enabling use the protection interval.
- With no target, Escape does nothing. Disabling the coordinator hides its active affordance; re-enabling reconciles its eligible windows and held-key state. No menu, Tab cycling, priority policy, or hold-to-dismiss is implemented.

## CLEAN and native input

CLEAN registers during its existing SSH splash and PID selection. Its result display deregisters before the existing 1.3-second hold; results cannot be canceled and never display `[ESC] CANCEL`. Its old independent Escape handler is removed. Correct/invalid/innocent PID consequences, infection capture/removal/provenance, five-Health failure penalty, ordinary timings, feedback, and terminal/PID focus transfers remain unchanged. External window disable cancels an owned unfinished session without penalty, releases ownership, and returns typing when the terminal is active; host reset also closes/deregisters the UI.

Two orders are supported:

| Order | Typing owner | First valid Escape | Next fresh Escape |
| --- | --- | --- | --- |
| CLEAN, then call | CLEAN PID field | Hang up call | Cancel CLEAN |
| Call, then CLEAN | CLEAN PID field | Cancel CLEAN, restore terminal | Hang up call |

Both terminal and PID fields use `EscapeSafeInputField`, a narrow TMP subclass. TMP otherwise cancels editing through both its native Escape event and the Input System UI `OnCancel` callback, restoring the old draft and dropping focus even when a call owns dismissal. The adapter suppresses those two cancellation paths. It retains TMP's protected key-editing, selection, and submit handlers, native SelectAll commands, composition handling, and text-mesh refresh. It is not a focus manager and does not dismiss windows. Native editing/Enter and Escape preservation are exercised by the focused witness; physical keyboard/IME review remains manual.

## Validation and limits

`Tools/Connections/Run Escape and Video Call Witness (Play Mode)` runs the focused checks in the four-node scene and stops Play Mode when done. Batch mode accepts `-witnessScene ConnectionsAdvanced`; optional `-capturePath <directory>` records the authored Canvas. The V1 witness selects one persistent render size per run with `-witnessWidth` / `-witnessHeight`; run each requested size separately. The witness covers native TMP/UI cancellation, both ordering cases, focus/draft/selection preservation, one dismissal per press, held keys, automatic appearance/removal protection, result ineligibility, reset/disable/destroy cleanup, simulation continuation, subtitle interruption/completion, sprite/talking hooks, long content, and unscaled protection. Existing CLEAN, Infection, Health, Inspect V2, Resolved Inspect, and Advanced Scene witnesses cover adjacent mechanics and layout.

See the [review evidence and manual checklist](../Review/ConnectionsVideoCallEscapeV0/README.md) for actual runs and caveats. No notifications, storyteller, Level Director, traffic/threat changes, pause/menu, source blocking, player progression, or Packet changes are part of this slice.
