# Connections — Windowed Workspace, Escape & Communications V0

**Status (November 1, 2026):** Original Connections-specific design direction from the October 30 discussion, **partially implemented through merged PRs #69–#74**. This remains a design record, not a substitute for current code or [Connections Current State](Current_State.md). Some original hypotheses were superseded during implementation and playtesting; those are reconciled below. Packet remains independent.

## Implementation reconciliation — November 1, 2026

| Area | Implemented in Connections | Still open / deferred |
| --- | --- | --- |
| Desktop | [#69](https://github.com/abeering/uptime-game/pull/69) adds independently framed/pinned docked windows and full-width Health strip. [#70](https://github.com/abeering/uptime-game/pull/70) removes expanded Health and Tab expansion. | Final Fluxbox/pixel-indie art treatment, further status strip features. |
| CLEAN | [#71](https://github.com/abeering/uptime-game/pull/71) gives PID-Select the shared frame/titlebar and an Escape hint only during cancelable phases. | Other minigames/utilities. |
| Calls and Escape | [#72](https://github.com/abeering/uptime-game/pull/72) adds an auto-opening call API, one active call, and independent LIFO Escape/typing ownership with fresh-key and target-change protection. [#73](https://github.com/abeering/uptime-game/pull/73) adds a 16:9 video-first call, translucent subtitles, tintable handset, and content-aware positioning. | Actual caller art, authored call events, special ringing/paused calls, hold-to-dismiss. |
| Spatial arrangement | [#73](https://github.com/abeering/uptime-game/pull/73) prefers calls upper-right and CLEAN near its target node, allows minor frame overlap, and does not move existing windows. | Further tuning for more window types and larger levels. |
| Text communications | [#74](https://github.com/abeering/uptime-game/pull/74) delivers instant messages/system alerts in an upper-left network stack, with avatars/icons, sender, time, teal/amber rails, immediate full text, and automatic unscaled expiration. **Ordinary messages do not own Escape.** | Actual content/event director, length/lifetime/density tuning, possible upper-terminal placement. |
| Menu, pacing, consequences | Reusable presentation/delivery systems and labeled developer demonstrations exist. | Pause/menu, end-of-shift UI, event-to-meaning narrative orchestration, reactions to ignored/hung-up communications. |

The original principles remain useful; **current implementations take precedence over any superseded proposal below**. Developer test content is not an authored story or evidence that the narrative system exists.

## Intent

**The network is the game.** The surrounding interface should feel like operating a purpose-built, keyboard-driven computer rather than playing a game inside an ornamental HUD. Communications are part of that experience: they impose attention and spatial pressure while interpreting the meaning of events on the network.

Two useful shorthand principles:

- **The player never needs the mouse.** The game manages its windows and keyboard ownership rather than asking the player to drag, select, or manually arrange them.
- **Communications are meaningful, missable, and never mandatory to consume.** A missed message may cost the player a hint, context, surprise, or a human reaction, but must not make the network game mechanically unplayable.

These principles describe the intended experience. The keyboard-first workspace and delivery surfaces now exist, but storyteller systems, pause/menu behavior, and several interaction proposals remain future work.

## 1. Desktop and visual system

### Docked workspace

- Preserve the **terminal-window** visual grammar for the network, gameplay terminal, CLEAN, video calls, and future interactive utilities. The network and command terminal are permanent, tightly **docked** parts of one workspace. Textual notifications deliberately use lighter cards instead of full window chrome.
- Reclaim wasted room: narrow, intentional gutters; compact titlebars; remove explanatory subtitles beneath window titles; avoid generous decorative margins and faux application chrome.
- Draw on efficient X11/Fluxbox-style window managers for density and grouping, **not** for freeform window dragging or desktop simulation.
- Permanent windows do not need minimize, maximize, or close icons that suggest clickable controls. Both permanent windows now use small, noninteractive filled-square pinned indicators.
- Keep a **global top status bar** as a stable home for the clock and Network Health; reserve its right side for later information. Avoid clickable-looking chat, webcall, or notification entry points.
- The underlying terminal and topology should not resize/reflow every time a floating communication appears. Temporary windows primarily overlay the **network view**.

### A coherent family of windows

Cleaning interfaces, future minigames, webcalls, email, settings, system menu, and inter-shift presentations should look like members of the same window system. Lightweight textual communications are deliberately a related but distinct visual surface. They can contain text, animated portraits, graphical interaction, or other content; "terminal window" is the visual and interaction language, **not** a requirement that everything be plain terminal text.

Prefer shared window-chrome/titlebar prefab patterns where useful, but do not prescribe a single universal prefab or a large UI framework before testing the interactions. Permanent docked panes and temporary overlays share a visual language without requiring identical behavior.

### Art direction deliberately deferred

The current interface is comparatively sleek, smooth, and neon-polished. The eventual aesthetic may lean toward **clean Fluxbox plus a crunchy/pixel-indie twist**. Do not decide the final palette, font, pixel scale, glow, or border treatment in this phase. Establish the spatial and interaction machinery first, leaving room for reskinning and for typography-driven layout adjustments.

## 2. Notification and webcall surfaces

### Notifications / text communications

- **Workplace instant messaging is the primary fiction.** System notifications use the same Connections-owned delivery model and card view, differentiated by a **left accent rail** rather than by repeated printed type names.
- Each card shows an avatar or service icon, sender/source, small timestamp, and message body. V0 uses a teal/cyan rail for instant messages and amber/gold for system alerts; missing human avatars use a neutral silhouette.
- The complete text appears **immediately**; there is no typewriter effect. The container alone slides/fades and expires. This distinguishes IM delivery from video-call speech.
- The V0 stack is upper-left in the network view, newest first, with a configurable cap of four live messages. Default unscaled reading durations are length-aware, roughly 6–16 seconds; these are **tuning values, not final authored-message rules**.
- Ordinary messages are **passive**. They do not receive typing focus, become Escape targets, require clicks, provide a reply box, or have a default archive/inbox. They can be missed entirely.
- Bursts revealed a real concern about obscuring an already constrained network view. Message length, linger time, density, and even a possible upper-terminal home should be evaluated against actual authored content and gameplay, not fixed based solely on developer burst samples.
- Text arrivals never move an already-open CLEAN or video-call window. Subsequent window placement can consider currently readable notification content, without reserving the corner permanently.

### Webcalls / webcam calls

- Calls are **terminal-style floating windows**, currently opening **automatically** when requested. Ordinary calls do not require ringing or an answer action. Special ringing calls remain a possible future storytelling device rather than the V0 default.
- V0 uses a camera-first **520×400** window with a **16:9** primary video region, optional portrait/talking animation, and unscaled typewriter **subtitles** over a translucent lower-feed scrim (not a separate side text column).
- The supplied monochrome handset sprite is tinted red/gray through Unity in a circular bottom-strip control beside **[ESC]**. The shared titlebar also retains **[ESC] HANG UP**. Both controls display active/red when the call owns Escape and subdued/gray when another window owns it; the call itself keeps playing. Neither affordance is clickable.
- Live calls leave simulation and current terminal/PID typing focus running. Immediate hang-up, including during subtitle typing, is an intended response to an imposed interruption.
- A paused, narratively important story/briefing call with possible hold-to-skip remains a **future proposal**, not an implemented mode.
- Calls prefer the upper-right, using a few deterministic candidate positions and protecting important content more than outer frames. CLEAN prefers placement near the serviced node. Windows remain stable once opened; a little corner/frame overlap is acceptable when content stays legible.
- One video call is currently supported at a time. Duplicate starts are rejected rather than queued or replacing the active call. How authored calls begin, end naturally, or affect later character reactions remains open.

## 3. Typing focus and Escape are distinct systems

This is a central decision, not just an implementation detail.

- **Typing focus** answers: *where do ordinary typed characters and commands go?* The currently relevant owners are the gameplay terminal and PID-Select cleaning interface. Other purpose-built input workflows may be added later.
- **Escape target** answers: *which visible surface will Escape dismiss or back out of?*
- **Selecting or changing an Escape target must not itself steal or redirect typing focus.** A non-modal webcall can own Escape while the player's typing continues in PID-Select or the gameplay terminal. **Ordinary text notifications never own Escape in V0.**
- Opening/closing an actual input workflow (such as a menu) may naturally give that workflow keyboard ownership and restore the previous owner on exit. That is a focus-lifecycle action, **not** a side effect of Escape-target selection.

Example: the player is entering a PID in CLEAN/DB when a CEO webcall arrives. The minigame still receives the typed PID; the webcall receives the visible Escape targeting. The player can finish cleaning while the CEO is speaking.

## 4. Escape V0: a visible, generic LIFO dismissal stack

**Implemented rule:** use simple **last-in, first-out (LIFO)** targeting for *eligible dismissible surfaces*, not for every incoming communication.

- CLEAN registers during its SSH splash and PID selection; it deregisters during its noncancelable result display. A live video call is eligible while active. Ordinary IMs and system notifications **do not register**.
- The most recent eligible target gets the **active** Escape treatment. An older call can still show its handset/titlebar controls in **subdued gray** while CLEAN owns Escape, rather than removing every visual hint.
- A fresh Escape press affects **one** target; holding the key cannot dismiss successive windows. The UI ownership of Escape does not imply ownership of ordinary typing.
- No Tab cycling or manual Escape-target choice is implemented. If CLEAN opens first and a call opens afterward, the first valid Escape hangs up, and the next fresh press can cancel CLEAN. In the reverse order, CLEAN dismisses first.
- **With no eligible surface, Escape currently does nothing.** A paused command-style system menu remains a design proposal, not current behavior.

**Superseded original hypothesis:** A newly arrived ordinary notification used to be imagined as another Escape-stack entry that might preempt a call. After reviewing real notification behavior, V0 keeps such messages passive so a stack of IMs cannot prevent hanging up. A special persistent dismissible message could be reconsidered later, but none exists now.

### Press, hold, and accidental cascades

- **Live eligible surfaces:** one fresh press dismisses; key repeat/holding never cascades.
- **Target changes:** current implementation applies a provisional **0.15-second unscaled protection interval** on automatic changes. Protected keypresses are consumed, not passed through or replayed afterward.
- **Paused protected story moments:** the original proposal was **hold Escape to skip/close**, perhaps with circular progress. This has **not** been implemented or tuned.
- The visible Escape affordance promises what the next valid press will do. Its state must stay synchronized with coordinator ownership, even if the call continues visually when not targeted.

## 5. Pausing and system-level interactions — future proposals, not implemented

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

This remains compatible with the broader shared [communications design](../Design/notifications.md): events should request **meaning** instead of always emitting fixed lines. It does **not** authorize implementing the full shared storyteller/event architecture. **#74 provides a message-delivery API, not automatic event interpretation; current notifytest/calltest examples are explicitly developer demo content.**

### Meaningful, missable, never mandatory to consume

- Communications may contain **actionable hints**, foreshadowing, humor rooted in events, stakes, and narrative consequences. They are *not* required to be inconsequential.
- They must **not** be the exclusive source of a password, command, critical objective, or mechanic the player cannot otherwise discover through the network/operating environment.
- A player who ignores every message and hangs up on every call should still have a **complete, understandable network game**, though they may miss important context, be surprised by events, or lose opportunities to interpret what happened.
- By default, **do not build an inbox, notification history, clickable status-bar notifications button, or required replay/recovery workflow**. Ephemerality is an intended design quality. Persistent run rules and mandatory objectives need homes outside fleeting communications.
- Call dismissal can eventually be a storyteller event ("hung up early"), and missed content can alter what the player understands, but no persistent consequence system is mandated by this V0.

Notifications are generally cheap, brief interpretation; webcalls are more expensive visual interruptions reserved for larger dramatic or contextual moments. Neither category is strictly "just flavor"; both must earn their attention cost.

## 7. Remaining playtest questions, not settled rules

1. **Density and location:** How long and frequent should authored IMs be? How many should coexist, and for how long? Is the upper-left network stack best, or should messages appear over the terminal when actual traffic and inspection load are present?
2. **Collision:** How much soft overlap and network occlusion feels atmospheric versus unfair? More utilities or the proposed hacker tug-of-war duel might require more spatial options.
3. **Keyboard ownership:** How does a future pause/menu window gain focus and restore it? How can the player pause while CLEAN currently owns typing? **Expanded Health and its old Tab shortcut are gone;** no replacement key assignment is implied.
4. **Call lifecycle:** Should rare authored calls ring instead of opening automatically? How do callers end naturally, and what happens when an existing call blocks another? Would a paused briefing merit hold-to-dismiss?
5. **Narrative meaning:** Which events deserve brief IMs versus expensive calls? How can characters react to network state or early hang-ups without leaking hidden hostile intent or creating filler?
6. **Visual skin:** How much retro/pixel character or far-future desktop customization belongs in the experience?

Avoid answering these through a large generalized window-management or storyteller framework before playtesting authored events.

## 8. Implementation boundary and future validation

This is a **design-history and reconciliation document**, not an instruction to reimplement or extend already completed phases. Current executable contracts are documented in:

- [Connections Current State](Current_State.md) — runtime/scene authority.
- [Network Health V0](connections-network-health-v0.md) — permanently compact strip.
- [Infections V0](connections-infections-v0.md) — CLEAN mechanics and its input lifecycle.
- [Video Calls & Escape Coordination V0](connections-video-calls-escape-v0.md) — call API, LIFO Escape, input preservation.
- [Video Call Presentation & Soft Placement V1](connections-video-call-presentation-v1.md) — camera-first window, subtitles, icon and placement.
- [Text Communications V0](connections-text-communications-v0.md) — typed delivery model, avatar/rail styling, stack, expiry and passive Escape behavior.

Packet's [NotificationDirector](../../Assets/Scripts/NotificationDirector.cs), [VideoCallDirector](../../Assets/Scripts/VideoCallDirector.cs), and [VideoCallView](../../Assets/Scripts/VideoCallView.cs) remain reference material, not implicitly shared code or behavior.

Interactive review priorities now include: physical keyboard/IME focus, a real CLEAN/call/notification sequence under generated traffic, message-density and readability tuning, subtitle contrast with actual portrait art, and whether long-term notification placement should use otherwise empty terminal space. Paused-story hold, the command-style system menu, and `pause` are only relevant for tests **after** they are implemented.

**Source authority:** The current Connections runtime, scene, and prefab state still determine what is actually implemented; [Connections Current State](Current_State.md) remains the starting implementation map. Packet stays independent.
