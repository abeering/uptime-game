# Connections — Hacker Challenges: Command Races, Outcomes & Event Feedback

> **Status:** Working design exploration captured from the November 1, 2026 discussion (Pacific time). **Not an approved V1 implementation contract**, balance specification, or instruction to implement. Numbers and command strings are illustrative.
>
> **Scope:** Uptime's active **Connections** prototype. This is the first proposed *hacker-initiated* skill encounter in the earlier "tug-of-war / hacker repartee" design space, now framed as a **race** rather than literal back-and-forth tug-of-war. Packet remains independent. The positive-outcome and event-presentation ideas may eventually apply to other Uptime interactions.

## The fantasy and why this is a distinct interaction

An adversary sometimes directly challenges the operator to a short, high-pressure, keyboard-first contest. The operator sees **two people typing competing sequences of credible shell commands**, attempting to finish before the other. The point is the immediately understandable one-versus-one feeling of TypeRacer, not a deep shell parser, real execution of the displayed tools, or a test of prior Unix knowledge.

This complements, rather than replaces, current network-defense decisions:

- **Player-initiated CLEAN:** The operator chooses when to give up attention and engage in a short, skill-based cleanup attempt. The simulation currently keeps running during PID-Select. See [Infections V0](connections-infections-v0.md) and shared [clean minigame designs](../Design/clean-minigames.md).
- **Hacker-initiated challenge:** The adversary chooses the timing and directly imposes an attention demand. The player can see it coming, but initially may have to face the confrontation when it begins. Future challenges could use different minigame verbs; this document captures the first **typing race**.
- **Existing network threats:** Hostile connections, Payloads and Worms pressure the network through their own movement, effects and counterplay. The race is an additional encounter form, not just a relabeled INFECTOR or [Payload](connections-payload-interception-v0.md).

The player should feel they are **operating the system** while the opponent is **trying to compromise it**. Speed and accuracy are central; the terminal fantasy and adversary identity make the mechanics memorable.

## Challenge lifecycle and warning

A likely first encounter shape:

1. A hacker challenge is announced with a **clear target** (e.g. AUTH), a recognizable hostile intent, and a countdown: "Intrusion contest begins in N seconds."
2. The warning gives the player a chance to finish urgent work, prepare mentally, or accept that some network problems will remain unresolved. It creates pressure in the spirit of Payload interception, but is not itself a traveling Payload.
3. At the indicated time, the race takes over the typing interaction, with both competitors visible and no extra tutorial or choice gate required for each encounter.
4. The first competitor to complete their sequence wins. Outcomes are applied to real simulation state where appropriate.
5. The player returns directly to the network. A **nonmodal terminal event card** states the cause and effect; any durable advantage or harm also appears on the corresponding network element.

Initially, an announced challenge may be **inevitable once scheduled**, specifically to test the race instead of designing a separate prevention route first. Whether the player can intercept, prevent, postpone, refuse or otherwise alter the challenge before it starts is intentionally open.

The warning duration, overlap limits, frequency, target selection and relation to the Level Director are all tuning/design questions—not locked timers or an implemented scheduling system. Some challenges may become deliberate "signature moments"; others may be selected from ambient hostile event opportunities.

## The race layout: two competing terminal panes

**Strong directional preference:** two side-by-side panes, **YOU on the left**, **HACKER on the right**, with a **separate progress bar above each pane's command area**.

- Large, unambiguous labels: **YOU** and **HACKER**, rather than role jargon that first-time players must decode.
- Prefer teal/green for the player's identity/accent and red for hostile identity/accent; do **not** rely on color alone. Position, labels, focus state and animation also communicate ownership.
- The player's pane has the unmistakable active typing focus: a visible/blinking input cursor, possibly a subtly brighter border or brief focus pulse when the challenge starts. The player should not have to click the pane to begin.
- The hacker's pane types visibly on its own. This provides a second immediate cue to who owns which pane.
- Each side uses the **same underlying visual grammar**: monospaced shell-like text, a simple `>` prompt at the beginning of **each** line, matching typography and spacing. The different *content*, progress and player focus make them distinct.
- Completed lines remain legible; future lines can appear muted. Both progress bars represent progress **toward finishing the entire assigned sequence**, not elapsed wall-clock time.
- The race may overlay the network in a floating window, consistent with Connections' current authored-window direction; exact window dimensions, layering and placement remain to be designed.

Illustrative layout, not a pixel specification:

```text
                     INTRUSION CONTEST  —  TARGET: AUTH

  YOU                                      HACKER
  [████████░░░░░░░]  54%                  [██████████░░░░░]  67%

  > connect auth                           > nmap -sV -p 22 auth.internal
  > lock sess|ions                         > ssh guest@auth.internal
  > seal gateway                           > id|
```

The above uses abbreviated fictional player commands purely to demonstrate **position, labels, progress and cursor**. Actual command-content direction is closer to real Unix below.

### Input and feedback contract to test

This is recognizably **TypeRacer**, expressed as shell command sequences:

- The player types the characters displayed for the **current line**. Correctly matched characters visibly light up, gain contrast or weight. Untyped characters stay muted.
- Mistyped characters show an obvious error treatment; the player must **Backspace and correct the mismatch** before advancing. Errors are not silently skipped.
- Completing a line advances automatically. **Enter may also advance a fully completed line but is never required**; typing the next line should flow without a forced Enter press.
- The prompt glyph is visual framing, not part of the sequence the player must enter.
- Progress should be computed from actual completed/correct text (with a legible policy for a partially typed line). Avoid a progress bar that contradicts the displayed typing.
- The hacker's commands are revealed one character at a time; **visible mistakes and Backspaces should change the hacker's actual simulated progress**, not be disconnected theatrical animations. Watching a fast hacker stumble should genuinely allow a comeback.
- Both sides may have different commands and different overall character counts. The pacing model must avoid declaring a winner merely because one side was assigned dramatically less work; exact opponent speed/accuracy/difficulty calibration is open.
- A race must finish, resolve cleanly, and restore ordinary terminal input. Input focus, held keys, Enter, Backspace and Escape should never leak into unrelated commands/windows.

The skill is **speed plus accuracy**, not memorization, command interpretation, Linux expertise, or a Simon Says pattern. Other hacker-confrontation minigames may explore those verbs later.

## Duration and whether the network pauses

**Do not hardwire a pause policy.** From the first implementation, **network simulation while racing must be a tunable option** independent of race length.

- It is reasonable to **start testing with simulation paused** to isolate the typing contest, but the broader Uptime fantasy is that the network is always doing things and sometimes the hacker simply demands attention at an awful moment.
- The long-term preference leans toward **the network continuing to run**, particularly if a challenge lasts only a few seconds. A rough **5–20-second range** expresses the desired *scalability* of encounter intensity, not a promised fixed session duration.
- A 5-second burst can blend into moment-to-moment play; a 20-second encounter can completely occupy the player's attention. These should be tunable without changing the minigame concept.
- If the network is paused, the **race and its warning/result UI must still progress on appropriate real/unscaled time**; be explicit about which timers run when paused. Network Health, effects, Conditions and hostile deadlines follow their authoritative simulation clock unless separately designed.
- "It came at a bad time" is not inherently a defect: forced pressure and imperfect timing are part of Uptime. We should test whether this pressure is exciting before deciding any pause policy is necessary.

Related open questions: how to handle simultaneous CLEAN/call/other windows, exactly when the warning countdown runs, whether an interrupted challenge is lost or deferred, and how the encounter affects the broader terminal and network visibility.

## Command fantasy: real-ish Unix, readable even at high difficulty

The commands are a major part of why this can feel exciting. The aim is to **impress an experienced operator/pentester with plausible tool choices and coherent context**, while making someone who has never administered a system feel competent and powerful.

**Lean into the real**, but do not make syntax an obnoxious obstacle or treat these as a live shell tutorial:

- Prefer actual, recognizable tool names and plausible flags/arguments: `ssh`, `nmap`, `ss`, `curl`, `grep`, `tcpdump`, `journalctl`, `systemctl`, `dig`, `loginctl`, and selectively more specialized offensive tooling.
- Some invented or simplified commands are fine where gameplay needs them. Do not generate random technical-looking nonsense and imply it is a real Linux utility.
- **Both sides** should become more technical-looking as difficulty increases. Do not lock the player permanently into beginner-style English verbs while the hacker gets all the compelling arcane syntax.
- **Hacker sequences may be *slightly more arcane***, dense or obscure than the player's, because the hacker is animated and the player need not physically enter them. This gives adversaries personality without forcing extra difficulty onto the operator.
- The player should encounter recognizable English **mixed with** real-looking flags, paths, IP addresses, options, technical jargon and occasionally hex/regex. At advanced difficulty the player should feel like they are **flying through an impressive terminal**, not entering a random password.
- Syntax shape is a gameplay tuning dimension: punctuation, case, flags, pipes and quoted strings are harder to type than the same number of simple letters. Track **typing complexity separately from raw character count**.
- Generated sequences should fit the **target and narrative objective**. AUTH suggests sessions, SSH and privileges; DB suggests database/process investigations; web services suggest requests, logs and services. Sequences can be stylized, but should have a coherent reason to be typed.
- Readability and execution rhythm matter more than strict real-world completeness. Expert players may recognize tools; no player must understand their arguments to race.

Illustrative player-side progression (examples to curate and verify, not a final content catalog):

| Feel | Example lines | Typing challenge |
| --- | --- | --- |
| Beginner | `> ssh auth`; `> ss -tn`; `> systemctl status ssh` | Recognizable words, short flags |
| Intermediate | `> ss -tnp`; `> journalctl -u ssh`; `> loginctl list-sessions` | More technical names and punctuation |
| Advanced | `> sudo tcpdump -i eth0 port 443`; `> grep -i "unauthorized" /var/log/auth.log`; `> sudo iptables -A INPUT -s 10.0.4.12 -j DROP` | Flags, quotes, paths, address, punctuation |
| Expert | Plausible, dense command chains/pipes with recognizable words woven among technical syntax | Rhythm changes, high precision, not arbitrary gibberish |

Illustrative attacker-side snippets: `> nmap -sV -p 22 auth.internal`; `> ssh guest@auth.internal`; `> id`; potentially recognizable specialized tooling such as `rpcclient`, Impacket or `hashcat` in appropriately staged examples. Do not mistake tool name-dropping for technical plausibility: before content ships, **curate actual commands for flags, prerequisites and narrative sense**. The two competitors need not type identical sequences.

An interesting future hacker-identity dimension: one opponent is fast but error-prone; another is slower and precise; another starts slowly then accelerates. Their tool preferences can differ (noisy scanning, credentials, obscure shell one-liners). **Typing errors/backspaces are a real opponent behavior**, not only decorative flavor.

## Stakes: a direct hacker victory versus player initiative

Early question: is a hacker win "just an infection," or something more consequential? **Do not prematurely decide.** A persistent compromise or explicit **BREACH / INTRUDER CONTROL ESTABLISHED** of a targeted node feels more personal than simply reporting `INFECTED`. A first technical slice might reuse an existing infection effect while presenting it as a breach; richer consequences (hostile activity, lost access, special disadvantages) could come later. Avoid stacking excessive instant Health loss **and** a lasting infection **and** extra attacks before the basic race is tuned.

The more important realization is on **player victory**:

> Skillful play in Uptime need not merely prevent deterioration. It can create a *positive tactical opportunity* that changes the course of the shift.

The player knows what the hacker is trying to do, but **does not necessarily know in advance which benefit victory will produce**. Do **not** make the player stop to select one of three rewards. **Roll / dynamically resolve a benefit on success.** Some outcomes can feel especially powerful, others minor or awkwardly timed; that variance is acceptable and potentially interesting. It should be legible, attributable and at least mechanically valid, not necessarily optimized for the player's current situation.

Potential successful-challenge outcomes (candidate pool, **not** fixed guarantees):

| Benefit | Tactical meaning / caveat |
| --- | --- |
| **Source revealed** | Reveal a hacker source identity useful for the proposed `block <source>` command. This should expose an *actionable source*, not magically block the entire adversary forever. Connections' future source tracing/blocking remains design work; see [investigation ideas](connections-threat-investigation-ideas.md). |
| **Node protection** | Target node becomes immune to infections, Payload effects and/or Worm effects for a limited duration, with explicit scope. Exact protections and interactions with already-active infections/Conditions must be defined. |
| **Probe recharge** | Reset or shorten cooldowns on proposed edge-mounted `probe` tools; its value depends on the player's current tools and cooldowns. Probe remains a proposed surveillance device, not a firewall. |
| **Delayed hostile plan** | The attacker must postpone an upcoming action, when such a scheduled action actually exists. |
| **Hostile intelligence** | Reveal or flag relevant hostile traffic elsewhere, with knowledge distinct from hidden authoritative truth. |

A simple V0 could pair "hacker win → persistent compromise" with "player win → one rolled beneficial effect," but **neither the exact loss effect nor the reward table/odds are approved here**. Avoid building a full story director just to prototype the race.

### Future Level Director / storyteller

The proposed Level Director can eventually determine **eligible success benefits, weights, contextual influence and narrative framing**. It need not always pick the objectively best reward. Company, villain, target, recent events and run upgrades can influence the roll; randomness gives repeated races different consequences and can create stories out of unexpected timing.

This fits the existing [entities, Conditions, statuses and event design](connections-entities-conditions-events-ideas.md): an event coordinates gameplay consequences, while authoritative conditions/effects and player-facing statuses remain distinct. Random selection should be **seeded/recordable** when integrated into the simulation so that surprising outcomes can be investigated; do not infer this means a LevelDirector implementation already exists.

## Beyond races: successful execution as a reward trigger

The principle could generalize to **other successful skill interactions**, without mandating identical pools:

- Successful **CLEAN** could grant temporary immunity on the cleaned node (e.g. 15 seconds), a brief boost, or another useful benefit.
- Successful **Payload interception** could open a countermeasure or intelligence opportunity.
- Successful **hacker challenge** could roll a source reveal, immunity, probe cooldown reset, etc.

Beware farming: **a clean attempt on an already-clean node should not automatically generate success rewards** merely because the operator completed a minigame. The trigger needs an authoritative success criterion (such as removal of an actual infection).

This is an expansion of the existing upgrade direction: "doing the skillful thing" can become useful beyond simply restoring neutral state.

### Upgrade ideas to preserve

These are **future run-upgrade hooks**, not required initial race mechanics:

- **Sterile Environment:** successfully removing an infection grants the node N seconds of protection.
- **Counterintelligence:** winning a hacker encounter reveals a source, or raises the chance of intelligence rewards.
- Successful hacker challenges recharge Probes, or upgrade this reward's potency.
- Extend duration of protection rewards.
- Increase weighting of certain benefit families (information, protection, recovery, disruption).
- Small chance of an **additional rolled benefit** after a successful confrontation.
- Transform successful skill interactions into situational positive Conditions instead of flat "+X%" stats.

Fixed-trigger upgrades and roll-pool modifiers can coexist. Not every benefit needs to be upgrade-gated: **the baseline skill event should already have a meaningful possible positive outcome** so that victory is more than merely "nothing bad happened."

## One reusable terminal Event Card

The operator wants to **get back to action immediately**. A reward-selection modal or a giant outcome screen is therefore the wrong default.

Build toward a **reusable, clean, nonmodal event-history card in the existing terminal**, for both positive and negative events. Its core information is:

1. **Cause:** what just occurred (e.g. `HACKER CHALLENGE WON`, `CLEAN SUCCESS`).
2. **Effect:** what actually changed (e.g. `AUTH PROTECTED FOR 20s`, `ATTACK SOURCE EXPOSED`).

Optional metadata when necessary: **target**, **duration/expiry**, **time**, relevant identity, and enough detail to act on the outcome. The card should explain the causality, not expose unrelated hidden simulation truth.

Example, text only:

```text
SYSTEM EVENT                                      14:32:08
CAUSE   Intrusion challenge won — AUTH
EFFECT  AUTH protected from new infections for 20s
```

And the lasting protection must appear **on AUTH in the network view**, with a clear timer and scope where useful. If a Probe recharges, its tool/cooldown display must update. If a source is revealed, the relevant actionable identity must become accessible. If a breach persists, its authoritative node state and corresponding visible symptom must be shown without falsely revealing unrelated covert details.

**UI division of responsibility:**

- **Terminal history:** *What happened, why, and what effect did it apply?*
- **Network / relevant tool view:** *What is true right now, and how long will it last?*

No mandatory acknowledgment, reward picker or dismissal. Preserve command-input focus and scroll/draft behavior. Long-lived history cards can **collapse to compact receipts after a breathing period**, taking inspiration from [Inspect V2](connections-inspect-v2.md), without losing their cause/effect. Their exact lifecycle is a later presentation choice.

A single presentation pattern could later cover challenge outcomes, successful cleaning, interception, expiring protection, infrastructure incidents and other Level Director events. This **does not mean** the future Level Director, notification system, Condition model or event schema must all be implemented together.

## Example encounter (illustrative, not locked)

1. AUTH receives an announced challenge: `INTRUSION CONTEST — AUTH — 00:18`. The player finishes an urgent inspection.
2. A two-pane race opens: **YOU** left, **HACKER** right. Both progress bars begin. Player text visibly fills; one error must be corrected with Backspace. The hacker makes a visible typo, reverses two characters, corrects it and loses real progress.
3. The player wins. The contest closes quickly and restores terminal/network attention.
4. A terminal card records `CAUSE: Challenge won at AUTH` and `EFFECT: Probe cooldowns reset`. There is no reward-selection screen.
5. In another playthrough the *same successful encounter* could instead roll `AUTH immune for N seconds`; the node's Beneficial status shows the live protection timer. On loss, the outcome might be a persistent breach and its corresponding network status.

The example's names, durations and reward are presentation/design hypotheses. Actual behavior must follow authoritative simulation state.

## Open questions to resolve through playtesting

- What is the first **minimum fun slice**: which target, how many commands, what character count, how does the hacker's WPM/error pattern behave?
- How do we tune the **5–20-second** range, difficulty, rival pacing, command complexity and simultaneous network activity independently?
- Should the warning be a compact network cue, terminal message, authored event banner, or combination? How much lead time gives preparation without dead air?
- What exactly happens when another CLEAN session, video call or other focus-owning window overlaps a forced challenge? Is the encounter queued, deferred, or allowed to preempt?
- When does the simulation pause (if configured), and what clocks drive warning, racing, result display and persistent effects?
- What is the hacker-win effect for the first experiment: an existing infection with breach flavor, a new Condition, or a stronger explicit compromise?
- Which success benefit(s) can actually be realized before `block <source>` and Probe are built? How are rolls filtered when a reward depends on absent tools, resources or upcoming attacks?
- How random/contextual should the Level Director eventually be, without always gifting the most situationally powerful option?
- How should a source reveal remain actionable without trivializing the entire hacker campaign?
- How do event-card presentation, current node status families and future hidden information avoid misleading certainty?
- What is a natural "hacker personality" through typing rhythm, errors and tool preference without excessive narrative exposition?
- How do upgrades alter reward outcomes without making the base mechanic feel unrewarding or creating trivial farming loops?

## Related sources and boundary

- [Connections documentation index](README.md) and [Current State](Current_State.md) for implemented state; the prototype's **runtime, prefab and scene** determine executable truth.
- [Existing threats](THREATS.md), [ambient hostile activities](connections-ambient-hostile-activity-v2.md) and [Payload & Interception](connections-payload-interception-v0.md) for the simulation/events this could eventually interact with.
- [Entities, Effects, Conditions, Statuses & Events](connections-entities-conditions-events-ideas.md) for the broader Level Director and state/presentation vocabulary.
- [Threat investigation and source blocking / Probes](connections-threat-investigation-ideas.md) for currently speculative success rewards.
- [Node Status V0](connections-node-status-v0.md), [Video Calls & Escape](connections-video-calls-escape-v0.md), [Inspect V2](connections-inspect-v2.md) for existing UI/focus/lifecycle constraints.
- Shared [clean-minigame pillars](../Design/clean-minigames.md), [upgrades](../Design/upgrade-strategies.md) and [notifications](../Design/notifications.md) for the broader Uptime concepts.

**Non-goal of this document:** No implementation, no gameplay changes, no forced migration of Packet mechanics, no guarantee that `block <source>` or Probe are already installed, no command content generator, no fixed reward probability table, no finalized adversary campaign or LevelDirector architecture. Preserve the design space until playtesting can answer the remaining questions.
