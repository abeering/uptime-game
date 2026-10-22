# Packet — Current State

**Baseline:** Uptime `main` at `225d524d34905b700b128faafeaad3584333c2bf`, October 22, 2026. Packet is a parked, retained Uptime gameplay prototype, not an abandoned or deprecated game. Its scenes are `Assets/Scenes/GameScene.unity` and `Assets/Scenes/ABScene.unity`. This document is an orientation to the inspected code, not a claim of fresh Packet Play Mode validation.

## Representation and implementation boundaries

Packet represents traffic as individual packets moving along physical network connections in discrete ticks. `Assets/Scripts/NetworkRuntime.cs`, `TrafficDirector.cs`, `PacketView.cs`, and `ConnectionView.cs` are Packet-side runtime paths. In this codebase, Packet's `ConnectionView` is a **physical edge**; it is not the persistent logical connection modeled by the separate Connections prototype. Packet's `TrafficDirector.cs` must not be mistaken for `Assets/Scripts/Connections/ConnectionTrafficDirector.cs`.

The inspected Packet code includes:

- `CommandParser`, `CommandDirector`, and `ScanDirector` for typed commands and packet scan/trace interaction; `CommandDirector` also has block, clean, boost, throttle, spawn, and autospawn command paths.
- `LevelDirector` for tick events and traffic-loss/core-node failure state, plus traffic and infection classes. This is not evidence that the broader multi-day job/run proposal or a topology generator is implemented.
- `CleanDirector` for cleaning sessions. Its current dispatch uses Process Killer for supported infection types; the many alternative minigames in the design drafts are proposals, not a list of installed modes.
- `ScoreDirector` with packet event values and a score ledger, and `NotificationDirector` with an on-screen message feed. Their existence does not establish the full shared scoring or character/cue architecture proposed in design documents.

The exact effects, timing, serialized references, and tuning of those systems should be read from the working branch's code and `GameScene`/`ABScene` Inspector state. Packet Play Mode was **not** run during the Connections V1 integration or this documentation phase. Do not infer current polish, balance, or bug resolution from old notes.

## Design sources and status

Packet implements one representation of the wider [Uptime design](../Design/Uptime_Design.md). The broader company, adversary, run, upgrade, scoring, network-growth, infection, and notification goals are not Packet-only simply because many sources were written with packet examples.

For Packet-specific ideas and evidence, retain the complete [packet keyword document](keywords.md), [scan difficulty proposal](scandifficulty.md), [scan/trace note](scan-vs-trace.md), [scan timing reference](edge-traversal.md), [infection brainstorm](../Design/infections.md), [clean minigame drafts](../Design/clean-minigames.md), [level-phase checkpoint](../historical/levels-phase1.md), [debug spawn examples](spawn.md), and [UI ideation](gpt-prompts.md). These sources mix implemented observations, proposals, and old instructions. In particular, an item labeled “DONE” or “next” in an old document should be checked against current code and scene before being treated as present work.

The [older TODO](../historical/todo.md) and [bug observations](../historical/bugs.md) remain preserved but untriaged in historical sources. Do not treat them as a verified current task queue or dismiss them without reproduction.

## Validation and future work

The integrated Connections branch was compiled and witnessed in Unity 6000.3.11f1; those witnesses do not validate Packet gameplay. Before asserting exact Packet behavior or making Packet changes, open `GameScene.unity` and `ABScene.unity`, inspect serialized references, run the relevant scene in Play Mode, and check the command and event paths needed for the task. Keep Packet work separate from Connections runtime changes unless a shared component is deliberately chosen.
