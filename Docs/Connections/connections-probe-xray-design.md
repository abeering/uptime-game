# Connections — Probe: Edge X-Ray & Identity Hopping (Working Design)

**Status:** Working design captured November 2, 2026, from the October 31 Probe discussion. **Not implemented and not an approved implementation contract.** The ideas below describe a strong V0 direction while preserving open tuning and implementation questions.

**Scope:** Uptime **Connections**, not Packet. For executable truth use [Connections Current State](Current_State.md) and the runtime/serialized scenes. This note develops—and in places **supersedes the current design preference in**—the earlier [Threat Signals, Investigation, and Response](connections-threat-investigation-ideas.md) Probe discussion. That older document remains as history, including its exploratory "threat potential," evasion, trace/source-block, and multiple-probe ideas.

## The central reason for Probe

**"Show me what to kill right now because I'm overworked."**

Probe should be strong enough to belong in the player's basic/early-game toolbox. When several connections are competing for attention, the player can spend a **scarce, temporary intelligence opportunity** to eliminate routine investigation and immediately make useful decisions.

Its hard question is **where and when to deploy**, not how to decode its output.

- Probing a quiet link with **one visibly ID-hopping connection** gives a predictable payoff: reveal the identity of something deliberately evasive.
- Probing a crowded link with **six or more uninvestigated connections** is a speculative bet. Story cues, node trouble, past experience, or topology may suggest several are malicious; the Probe might find three threats—or none.
- A crowded edge is therefore tempting because one deployment may identify several enemies, but a single particularly dangerous concealed connection elsewhere can make the guaranteed choice better.
- The player still has to type commands, kill appropriate targets, and manage other network problems. A successful Probe does **not** automatically defend the network.

This is an intentional way to make Uptime *easier in a useful way*: save attention that would otherwise be spent inspecting every connection, while making prioritization and the opportunity cost of using Probe more important.

## Core V0 direction: `probe <linkId>`

**Physical scope:** One existing physical **link/edge** (for example `probe e3`), not a node, permanent application panel, or entire-network scan. Reserve the word **firewall** for a separate potential filtering/blocking tool. Probe itself is a temporary network tap / X-ray and never automatically blocks or kills traffic.

During its active window:

1. **Classify the logical connections currently present on the selected edge.** Connections known to the simulation to be hostile are unmistakably highlighted; benign connections become visually subdued. This is **authoritative threat identification**, not a LOAD anomaly heuristic.
2. **Continue evaluating new connections** that appear on/use the selected edge while Probe is running; it is not just a one-time snapshot.
3. **Reveal the stable, actionable identities of ID-hopping connections** on that link, temporarily removing their identity obfuscation.
4. Let the player act with ordinary tools such as `kill <id>`. Probe itself never takes the action.

**V0 preference: near-perfect truth within its narrow scope.** An ordinary-looking malicious INFECTOR should still light up red. A perfectly legitimate but unusually heavy connection should not. A benign connection becoming desaturated does not mean it is low-LOAD; it means Probe does not classify it as hostile. Exact evasion/confidence/false-positive rules were floated in an earlier design, but are **not** part of the favored straightforward first experiment.

**"Red == kill" is the central visual promise.** Only confirmed hostile **killable logical connections** should use that unambiguous red treatment. Do not indiscriminately apply the same cue to a Payload or Worm, which has different counterplay. Whether—and how—the Probe separately responds to those entities is a future design question, not a hidden V0 requirement.

Do not reveal an installed covert node infection, specific pending infection payload/effect, or other information belonging to `scan` or cleaning. A Probe catches threats *using an edge*; it does not diagnose the destination node.

### Temporary availability

Our initial deliberately generous experiment:

| Dimension | Working starting idea |
| --- | --- |
| Active devices | One Probe deployed at a time |
| Active duration | Roughly **10 simulation seconds** |
| Shared/global cooldown | Roughly **30–40 seconds** |
| Activation | `probe <linkId>`, immediate on a valid edge when available |
| On expiry | X-ray and its temporary threat labeling disappear |
| Charges | No separate consumable charge system required for V0 |

These are **playtest hypotheses**, not locked balance values or a runtime specification. Exact cooldown origin and time basis, ability to upgrade slots/duration, and any eventual per-run charges remain open. An earlier idea of 1–4 deployable probes is retained in the source history but is not the chosen V0 starting point.

**No extra cost or artificial player-work step is presently proposed.** Its active-time/cooldown scarcity is the intended tradeoff.

## The on-network X-ray

The visual concept is deliberately physical and **edge-aligned**:

- Draw a thin outlined, softly translucent **rectangle rotated into the direction of the selected physical link**, whether horizontal, vertical, or diagonal.
- Center it across approximately **80% of the visible link length**, leaving a portion of the ordinary connection rendering visible beyond each end.
- Size its width to the actual outer bounds of the occupied connection lanes, with a little margin—not a fixed enormous overlay.
- Tint the interior in a distinct, subdued blue/steel X-ray treatment.
- Recolor **only the portions of the logical connection lines inside the rectangle**: nonhostile lines are desaturated/ghosted; confirmed hostile lines are saturated **red**, potentially with a restrained glow.
- Outside the rectangle, preserve the existing line treatment, including the game's separate teal/amber/orange performance language. The sudden transition at the scanner boundary makes the X-ray legible without putting numeric labels on every lane.
- Treat rectangles that expand/contract with lane packing gently enough not to jitter. The precise shader/mesh technique and treatment of connection identity pills are implementation choices for later visual testing.

**Do not force pills to fit inside the rectangle.** Many will naturally overlap its footprint, but some won't; that's okay. The line X-ray is spatial evidence, while a revealed ID uses the entity's **existing pill**, wherever that pill sits along the edge.

This replaced our earlier flirtation with a red/yellow/green **LOAD heatmap** and a terminal table. That design blurred with normal connection performance colors and addressed the wrong question: *which connections are causing a node's resource problem?* That is now the responsibility of `load <nodeId>`, not Probe.

The preferred V0 output is primarily **on the network**. A short terminal acknowledgment, activation/expiry feedback, or cooldown indicator may be useful, but a dedicated live LOAD table is not part of this mechanic.

## ID hopping: a separate adversarial behavior countered by Probe

**Later implementation decision:** [ID Hopping V0](connections-id-hopping-v0.md) preserves immutable true IDs and changing advertised aliases, but true IDs remain actionable even without Probe. The temporary addressability gate proposed below was superseded; the original exploration is retained here for provenance.

A proposed connection modifier, provisionally **ID HOPPING** (also called rotating identity during exploration), changes the entity's *displayed* short ID roughly every **1–3 seconds**. It does **not** change the underlying persistent connection or its authoritative lifecycle.

Examples: `a3 → p7 → m2 → h9`, all on the same logical connection.

**Crucial gameplay rule:** Those rotating aliases should **never** be valid for `inspect` or `kill`, even if the player types the currently displayed alias instantly. This is not a reaction-speed puzzle. Ordinary queries should effectively report the target doesn't exist. The true identity is not ordinarily advertised or guessable through a different terminal panel.

Probe can temporarily **stabilize the existing identity pill to the true, command-actionable ID** for a hopping logical connection whose path uses the probed edge—even when that pill is rendered outside the scanner rectangle. It is then possible to kill the connection during the active window. When Probe expires, the identity conceals itself again unless the connection has already been resolved.

The distinction is deliberate:

- **Detection:** Probe knows the connection is hostile; red means a target worth killing.
- **Addressability:** ID hopping prevents ordinary commands from naming that connection; Probe grants temporary access to a stable ID.

These are complementary but not identical capabilities. A suspicious hopping connection might have ordinary LOAD and not otherwise stand out; its evasive identity alone creates a reason to spend Probe on its link.

**Payloads with hopping identity were also proposed.** That possible future modifier should be preserved rather than discarded. However, the current Connections runtime now supports **node-wide `intercept @ <nodeId>` without a Payload ID**, as well as optional ID-filtered interception (`intercept x1 @ db`). Therefore the original idea that an ID-hopping Payload *must* be Probed before it can be intercepted is no longer guaranteed by current mechanics. How a future hopping Payload interacts with Probe, node-wide interception, and its existing circular identity body requires fresh design, not automatic import of the older assumption.

Neither ID hopping nor Probe is an implemented feature in this document.

## Deliberately separated: revealing Payload routes

We explored a second very appealing use for an intelligence tool:

- Reveal a Payload's **entire planned route to its final target**, rather than only its next visible hop.
- Keep the revealed route visible persistently, with dimmer future segments distinct from the current moving segment.
- Once its destination/path is known, suppress the constant "this might be the final hop" urgency treatment; re-enable a clear **final-approach cue when it enters its last edge**.
- The prize is **less babysitting and less memory work**, while the final timed interception still requires player attention and execution.

For a while this was considered part of `probe <linkId>`. We ultimately felt **route revelation is a different gameplay idea** from the coherent physical-link X-ray/threat-identification mechanism.

**Preserve it as an independently interesting future Payload intelligence/tracking tool, not as a required Probe V0 ability.** Its exact command, timing, persistence, and relationship to the newer node-wide interception option are not selected.

## Relationship to the rest of the investigation toolkit

| Tool | Question it answers | Relative role |
| --- | --- | --- |
| `load <nodeId>` | What LOAD are active connections requesting at this node? | Fast node resource triage; no hostility declaration |
| `work <nodeId>` | How is connection WORK progressing at this node? | Live node work monitoring; not a threat oracle |
| `inspect <connectionId>` / `inspect -v` | What does this connection claim and what are its WORK/operation details? | Targeted observation, interpretation |
| `logs <connectionId>` (working design) | What behavioral evidence emerges in its operation logs? | Higher attention cost, contextual clues |
| `scan <nodeId>` | Is this node infected? | Node infection investigation, separate from traffic detection |
| `probe <linkId>` (proposed) | Which killable connections on **this edge** are hostile, and which hidden identities can I act upon? | Brief, powerful intelligence; expensive in cooldown opportunity |

The [operation logs design](connections-operation-logs-design.md) explores graded evidence: some attacks are blatant in LOAD, others in verbose WORK, others in a few log lines; not all tools should be obligatory. **Probe is the deliberately strong shortcut** when the player is overworked and chooses a good place/time to use it.

A later company/villain/level may change *how much evidence* other tools provide or *where threats appear*, creating different reasons to deploy the same basic Probe. This does not justify introducing a new LevelDirector, charge system, or generalized detection framework in the first Probe experiment.

## Decisions still open for later design / playtesting

1. **Exact active duration, cooldown, time basis, and cooldown origin.** The 10s / 30–40s values are illustrative.
2. **What qualifies as "on this edge"?** Current Connections logical routes can span multiple physical links even after establishment. Probe should classify the traffic actually represented by its rendered edge lanes, but precise matching during arrivals, departures, and transitional states needs a concrete rule.
3. **Presentation granularity.** Does a hostile identity pill also tint red, or are only line segments red inside the box? How are red/gray lines displayed alongside unusually colored or crowded normal paths?
4. **Stable ID behavior outside the probe rectangle.** Existing pills change to true IDs for the active window; exact animation/format and interaction with duplicate aliases or stale player commands need later specification.
5. **Information retention.** V0 favors no lasting red markers after expiry. What, if anything, should terminal history record as an already discovered threat?
6. **Entity scope.** Payloads, Worms, and future unusual entities have different counterplay; avoid suggesting `kill` applies to all. A future edge sensor might support more than logical connections, but that is not a present promise.
7. **Probe upgrades and scarcity.** Additional simultaneous devices, upgradeable recharge, charges, and villain countermeasures were discussed as future exploration, not locked V0 requirements.
8. **Player feedback and fairness.** What should activation, expiration, a wrong/invalid link, cooling down, and a scan that finds nothing communicate? A busy edge with no hostile traffic should feel like a useful truthful answer, not an error.
9. **Evasion and certainty.** The earlier document allowed partial confidence or probe-evasive threats. The favored first pass is perfectly truthful for killable hostile logical connections on its chosen edge. Only revisit evasion if playtests show a need.

## Next experiment, not requested by this PR

Once we're ready to implement, build the **smallest playable test**: a temporary edge-aligned X-ray that truthfully highlights hostile logical connections and reveals hopping identities. Try it in ordinary and crowded Connections scenes, test with a single guaranteed hopper versus several ambiguous connections, and judge how its limited availability changes the player's decision-making. Don't bolt the separate Payload tracking idea into it simply to add features.

**This PR is documentation only.** No runtime, commands, simulation types, prefabs, scenes, test harnesses, UI presentation, or balancing changes are authorized by this note.

