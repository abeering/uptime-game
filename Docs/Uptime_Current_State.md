# Uptime — Current State

**Documented baseline:** `main` at `225d524d34905b700b128faafeaad3584333c2bf`, October 22, 2026. This is a dated repository snapshot, not a live branch-status feed. Check GitHub and the working branch before using it for a later implementation decision.

Uptime is one game being explored through two different gameplay prototypes. **Packet** models individual packets and discrete network traversal. **Connections** models persistent logical connections, operations, WORK, and shared LOAD. Both are retained. Connections is the current development focus and is promising, but no decision has been made to replace or abandon Packet. The broader game design is shared and can evolve as either prototype teaches us more.

## Playable boundaries

| Prototype | Scene and runtime | Current posture |
|---|---|---|
| Packet | `Assets/Scenes/GameScene.unity` and `Assets/Scenes/ABScene.unity`; principally `Assets/Scripts/` outside `Connections/` | Parked, not deprecated. Tick/packet traversal, scan/trace, blocking, infections, cleaning, scoring, and notifications have implementation code. See [Packet Current State](Packet/Current_State.md) for the evidence and limits of that summary. |
| Connections | `Assets/Scenes/Connections.unity`; `Assets/Scripts/Connections/` and `Assets/Prefabs/Connections/` | Active experiment. The scene defaults to generated benign and hostile traffic on its four-node, three-link network; Authored Witness mode remains available. Its first persistent infection is Phantom LOAD. See [Connections Current State](Connections/Current_State.md). |

The scenes and simulation paths are separate. `Assets/Scripts/TrafficDirector.cs` drives Packet; `Assets/Scripts/Connections/ConnectionTrafficDirector.cs` drives Connections. Packet's `ConnectionView.cs` names a physical edge, not a Connections logical connection. Sharing a game vision does not make their current mechanical rules interchangeable.

The project's `ProjectSettings/EditorBuildSettings.asset` lists `SampleScene`, not either playable prototype scene. A standalone player needs a game-specific build scene list or profile. The documented Editor version is Unity 6000.3.11f1.

## Integrated Connections milestone and validation

The current Connections stack was merged into `main` through merge commits: [#3 operation identity and abstract WORK](https://github.com/abeering/uptime-game/pull/3), [#4 target-node LOAD](https://github.com/abeering/uptime-game/pull/4), [#5 operation arcs](https://github.com/abeering/uptime-game/pull/5), [#8 inspect UI](https://github.com/abeering/uptime-game/pull/8), [#6 generated traffic](https://github.com/abeering/uptime-game/pull/6), and [#7 run recorder](https://github.com/abeering/uptime-game/pull/7). [PR #1](https://github.com/abeering/uptime-game/pull/1) established the original separate experiment and working instructions. The final integrated `main` file tree matched the recorder branch at the milestone.

Unity 6000.3.11f1 batch Editor compilation and Resource, Geometry, Traffic, and Recorder Witnesses passed against that integrated `main`. The checks cover deterministic model cases and scene/prefab bindings exercised by the witnesses. They are not a substitute for interactive scene review. A full Connections Play Mode smoke test and Packet Play Mode test were **not** run as part of the integration. In particular, the integrated check did not establish 16:9 readability, command focus under play, or the interactive folder-opening behavior.

## Game design and implementation authority

| Question | Start with | Then inspect |
|---|---|---|
| What game are we trying to build? | [Uptime Design](Design/Uptime_Design.md) | Its linked full design documents, including active working proposals and alternatives. |
| What does Packet now do? | [Packet Current State](Packet/Current_State.md) | Packet runtime and serialized `GameScene`/`ABScene` on the working branch. |
| What does Connections now do? | [Connections Current State](Connections/Current_State.md) | Connections runtime, prefabs, and serialized `Connections.unity` on the working branch. |
| Why was an approach chosen or changed? | The relevant current contract and linked PR | Original briefs, review assets, Git history, and research as evidence, with their original baseline in mind. |

Current executable and serialized state wins a disagreement about **what is implemented**; correct stale current prose. Shared game-design intent may describe a future feature that neither prototype implements. An old proposal or checkpoint does not become a current rule by being in `Docs/Design/`. The [generated Project Core](_generated/Uptime_Project_Core.md) is a cold-start navigation snapshot, not a source of independent authority or a live account of later changes.

## Open work, without implied authorization

Company contracts, hacker adversaries, multi-day runs, upgrades, scoring for Connections, network growth, infrastructure events, additional infection kinds, cleaning, and narrative notification systems remain important parts of the broader Uptime direction. Connections implements only the Phantom LOAD infection slice; the other mechanics and their integration remain unresolved. Packet already implements portions of some of these themes using packet-specific rules. The next task should name its shared-design or prototype scope and inspect the relevant current state before changing behavior.
