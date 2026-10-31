# Connections — Windowed Workspace, Escape & Communications V0

**Status:** Agreed starting design direction from the October 30, 2026 discussion; **not implemented**. Some interaction details are explicitly provisional and require playtesting. This is a **Connections-specific** workspace and communications proposal, not a retroactive Packet contract, a new shared Uptime mechanic, or authorization to change Unity code.

## Intent

**The network is the game.** The surrounding interface should feel like operating a purpose-built, keyboard-driven computer rather than playing a game inside an ornamental HUD. Communications are part of that experience: they impose attention and spatial pressure while interpreting the meaning of events on the network.

Two useful shorthand principles:

- **The player never needs the mouse.** The game manages its windows and keyboard ownership rather than asking the player to drag, select, or manually arrange them.
- **Communications are meaningful, missable, and never mandatory to consume.** A missed message may cost the player a hint, context, surprise, or a human reaction, but must not make the network game mechanically unplayable.

These principles describe the intended experience, not an assertion that all of the controls or storyteller systems exist today.

## 1. Desktop and visual system

### Docked workspace

- Preserve the **everything-is-a-terminal-window** visual grammar. The network view and gameplay command terminal are permanent, tightly **docked** parts of one workspace.
- Reclaim wasted room: narrow, intentional gutters; compact titlebars; remove explanatory subtitles beneath window titles; avoid generous decorative margins and faux application chrome.
- Draw on efficient X11/Fluxbox-style window managers for density and grouping, **not** for freeform window dragging or desktop simulation.
- Permanent windows do not need minimize, maximize, or close icons that suggest clickable controls. A small, noninteractive pinned/locked indicator is a possible replacement, not a finalized icon.
- Keep a **global top status bar** as a stable home for the clock and Network Health; reserve its right side for later information. Avoid clickable-looking chat, webcall, or notification entry points.
- The underlying terminal and topology should not resize/reflow every time a floating communication appears. Temporary windows primarily overlay the **network view**.

### A coherent family of windows

Cleaning interfaces, future minigames, webcalls, email, settings, system menu, and inter-shift presentations should all look like members of the same window system. They can contain text, animated portraits, graphical interaction, or other content; "terminal window" is the visual and interaction language, **not** a requirement that everything be plain terminal text.

Prefer shared window-chrome/titlebar prefab patterns where useful, but do not prescribe a single universal prefab or a large UI framework before testing the interactions. Permanent docked panes and temporary overlays share a visual language without requiring identical behavior.

### Art direction deliberately deferred

The current interface is comparatively sleek, smooth, and neon-polished. The eventual aesthetic may lean toward **clean Fluxbox plus a crunchy/pixel-indie twist**. Do not decide the final palette, font, pixel scale, glow, or border treatment in this phase. Establish the spatial and interaction machinery first, leaving room for reskinning and for typography-driven layout adjustments.

## 2. Notification and webcall surfaces

### Notifications

- Small, transient communications stack in a consistent **corner of the network view**, broadly like the Packet-era OS-style notifications.
- They may expire on their own or be dismissed early. Exact lifetime, count, animation, and clock domain remain tuning decisions; Packet's current values are examples, not a Connections specification.
- Notifications are **not buttons** and should not require a click, focus transfer, archive, or separate inbox to understand the game.
- The active Escape target can be a notification, with the same visible Escape-key treatment used for larger windows.

### Webcalls / webcam calls

- Calls appear as **terminal-style floating windows** over the network, containing an animated character/portrait or video area, subtitled speech, and an explicit hang-up affordance such as a red hang-up glyph next to **`[ESC]`**. The glyph teaches the keyboard action; it is not an invitation to click.
- A rough early visualization is **about one-sixth of the network view**; this is not a fixed size requirement.
- **Live calls:** the network continues and the player can continue typing network commands or working in a minigame. The call may obstruct part of the network; hanging up is a valid response to that pressure.
- **Paused story/briefing calls:** may be presented with the simulation paused, and may use a deliberately slower hold-to-skip/dismiss affordance (see Escape below).
- Placement can start with a small number of sensible candidate positions. Intelligent placement is an experiment, not a prerequisite. Once placed, a call should not constantly chase available space as network conditions change.
- We do **not** need to guarantee that a call never overlaps an important node. Fast, reliable dismissal is part of how the player regains visibility.

No complete policy is yet set for overlapping calls, arrival scheduling, how calls end naturally, or whether a second caller queues/replaces an existing one.

## 3. Typing focus and Escape are distinct systems

This is a central decision, not just an implementation detail.

- **Typing focus** answers: *where do ordinary typed characters and commands go?* The currently relevant owners are the gameplay terminal and PID-Select cleaning interface. Other purpose-built input workflows may be added later.
- **Escape target** answers: *which visible surface will Escape dismiss or back out of?*
- **Selecting or changing an Escape target must not itself steal or redirect typing focus.** A non-modal webcall or notification can own Escape while the player's typing continues in PID-Select or the gameplay terminal.
- Opening/closing an actual input workflow (such as a menu) may naturally give that workflow keyboard ownership and restore the previous owner on exit. That is a focus-lifecycle action, **not** a side effect of Escape-target selection.

Example: the player is entering a PID in CLEAN/DB when a CEO webcall arrives. The minigame still receives the typed PID; the webcall receives the visible Escape targeting. The player can finish cleaning while the CEO is speaking.

## 4. Escape V0: a visible, generic LIFO dismissal stack

**Starting rule:** use simple **last-in, first-out (LIFO)** targeting for eligible, currently presented dismissible interruptions, rather than building an intelligent priority scheduler or a second window-selection mode.

- Newly presented dismissible surfaces enter the Escape stack. The top eligible entry is what Escape will affect.
- The current target prominently displays a **`[ESC]` keycap/tag** in or around its window chrome (including notification chrome); other surfaces should not compete visually for that designation. A short label can explain the action: **HANG UP**, **CANCEL**, **CLOSE**, etc.
- A tap dismisses/backtracks **one target per fresh keypress**, then the indicator moves to the next eligible target. This is independent of ordinary keyboard focus.
- Notifications that expire on their own leave the stack; the marker moves accordingly.
- The default is **no Tab cycling or manual choice of dismissal order**. If the player is cleaning and a webcall arrives afterward, they must hang up before Escape can cancel cleaning. That extra friction may be intentional attention pressure.
- If there is **no presented interruption** to dismiss, Escape opens the game menu. The menu itself can then be escaped from normally.

The generic LIFO rule deliberately wins over special "webcalls always before notifications" ordering. For example, a notification arriving *after* a webcall becomes the next Escape target. Revisit only if playtesting exposes a compelling problem.

### Press, hold, and accidental cascades

- **Live/transient surfaces:** one tap dismisses.
- **Paused, deliberately protected story moments:** propose **hold Escape to skip/close** rather than blocking Escape entirely. The same keycap can show a circular/rotational hold-progress fill. Exact thresholds and which events qualify are not yet specified.
- Holding a key must **not repeat through multiple stack entries**. One press/hold is bound to one target until release; ignore key-repeat as repeated dismissals.
- Add a small **target-change protection/grace period**, especially after automatic expiry or a new arrival, to prevent a rapid Escape intended for one notification from accidentally closing a newly targeted webcall or opening the menu. The timing is a playtest parameter, not an approved millisecond value.
- The Escape marker is a promise about what the **next valid Escape input** does; protect that promise more carefully than any arbitrary animation timing.

Existing minigame Escape/cancel behavior must be reconciled with this routing when implemented; this document does **not** claim the current Connections input stack already behaves this way.

## 5. Pausing and system-level interactions

- Add a gameplay-terminal **`pause`** command that freezes the network simulation **without opening a menu or changing the desktop layout**. This is separate from dismissing interruptions. Its unpause counterpart, allowed read-only actions, and interactions while a minigame owns typing remain to be designed.
- With the Escape stack clear, Escape opens a **paused, full-workspace system-menu terminal**. It should be styled like the rest of the environment, with a prompt and discoverable commands rather than conventional clickable menu buttons.
- Illustrative menu vocabulary discussed: **`quit`** (save and exit), **`settings`**, and **`new`** (new run). Confirmation, save semantics, and final commands are not yet gameplay contracts.
- Between-run experiences should also live **over the command desktop** instead of switching to conventional videogame UI. An end-of-shift email/performance-review window is an example, not a requirement for the next implementation slice.

Escape and `pause` serve different purposes. The need for an always-accessible emergency pause shortcut when a minigame owns typing remains open; do not silently treat Escape as that shortcut.

## 6. Communications: meaning, not filler

**Every interrupting communication should change the player's understanding of something.** Remove chat/call concepts that merely make the UI feel busy or deliver irrelevant jokes.

Useful purposes include:

- **Clues:** "I just received a password-change email" could hint at suspicious authentication activity without being required evidence.
- **Anticipation and context:** "The sale starts in five minutes" explains or foreshadows an upcoming benign traffic surge.
- **Threat and personality:** a hacker's taunt can foreshadow an escalation and establish who is exerting pressure.
- **Interpretation and consequences:** a customer complaint explains why degraded service matters; a character reacting after a kill can reveal the significance of the defense; a message can explain a Health/score change or a failed hostile plan. (Connections scoring is not yet implied to exist.)
- **Human reaction:** hanging up on a boss might produce later annoyance or appreciation, depending on context. This is a storyteller opportunity, not a required relationship engine.

This is compatible with the broader shared [communications design](../Design/notifications.md): events should request **meaning** instead of always emitting fixed lines. It does **not** authorize implementing the entire shared storyteller/event architecture.

### Meaningful, missable, never mandatory to consume

- Communications may contain **actionable hints**, foreshadowing, humor rooted in events, stakes, and narrative consequences. They are *not* required to be inconsequential.
- They must **not** be the exclusive source of a password, command, critical objective, or mechanic the player cannot otherwise discover through the network/operating environment.
- A player who closes every notification and hangs up on every call should still have a **complete, understandable network game**, though they may miss important context, be surprised by events, or lose opportunities to interpret what happened.
- By default, **do not build an inbox, notification history, clickable status-bar notifications button, or required replay/recovery workflow**. Ephemerality is an intended design quality. Persistent run rules and mandatory objectives need homes outside fleeting communications.
- Call dismissal can eventually be a storyteller event ("hung up early"), and missed content can alter what the player understands, but no persistent consequence system is mandated by this V0.

Notifications are generally cheap, brief interpretation; webcalls are more expensive visual interruptions reserved for larger dramatic or contextual moments. Neither category is strictly "just flavor"; both must earn their attention cost.

## 7. Initial playtest questions, not settled rules

1. **Collision:** CLEAN is active, a webcall arrives, then notifications appear. Does literal LIFO remain tolerable, or does another policy become necessary?
2. **Timing:** How long should communications linger? What happens when a notification expires at the instant Escape is pressed? How do protection windows and hold actions feel?
3. **Placement:** Which corners and candidate slots preserve the topology best across the four-node and advanced Connections layouts? How much occlusion feels like pressure versus unfairness?
4. **Keyboard ownership:** How does the pause menu get typing focus and restore it? How can the player pause while a minigame currently owns input? How does existing **Tab-to-expand Health** coexist with later shortcuts?
5. **Call lifecycle:** When is a call finished, interrupted, timed out, or deferred? What happens if another call arrives? Can paused briefings always be skipped with a hold?
6. **Visual skin:** How much retro/pixel character should be introduced after the docking/chrome/interaction structure is in place?

Avoid solving these through a large abstract window-management framework before an actual scene playtest.

## 8. Implementation boundary and future validation

This document is **documentation-only**. No notification system, video-call overlay, window prefab, new command, Escape dispatcher, menu, storyteller, or pause behavior is asserted as implemented in Connections.

The Packet implementation contains relevant reference pieces, including [NotificationDirector](../../Assets/Scripts/NotificationDirector.cs), [VideoCallDirector](../../Assets/Scripts/VideoCallDirector.cs), and [VideoCallView](../../Assets/Scripts/VideoCallView.cs). Their close buttons, typewriter-skip behavior, lifetimes, and scene assumptions are **not** adopted automatically.

For the first future Codex implementation handoff, consider separately testable slices: tighten the docked shell/shared chrome, then a visible Escape-routing prototype, then notifications and webcalls, then narrative orchestration. Exact sequencing requires discussion.

Useful interactive 16:9 scenarios to validate when implementation begins:

- Ordinary commands still type correctly under live notifications and a webcall; no mouse is needed.
- PID-Select retains typing while a newly arrived webcall takes the visible Escape marker; first Escape hangs up, next can cancel cleaning.
- A later notification correctly takes LIFO precedence, while expiry/repeat/rapid presses cannot accidentally cascade through targets.
- A held Escape shows progress for a protected paused story surface and never dismisses more than that surface.
- `pause` stops the simulation without opening the menu; the menu is a separate paused command-window experience.
- The network, docked terminal, status row, and overlays remain readable without pretending that titlebar decorations are clickable.

**Source authority:** The current Connections runtime, scene, and prefab state still determine what is actually implemented; [Connections Current State](Current_State.md) remains the starting implementation map. Packet stays independent.
