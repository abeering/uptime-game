# Working in Uptime

## Project posture / read first

Uptime is one game design currently expressed through two independent gameplay prototypes: the original packet-and-tick representation (Packet) and the connection-based representation (Connections). Keep both playable and free to evolve. Connections is the current focus, but neither prototype has been declared final or a migration of the other. Overarching game design can apply to both; share implementation only through deliberate decisions.

Before making changes, read:

1. `Docs/README.md`, `Docs/Uptime_Current_State.md`, and `Docs/Design/Uptime_Design.md` for the project-wide source map, current posture, and shared game direction.
2. `Docs/Packet/Current_State.md` or `Docs/Connections/Current_State.md` for the prototype in scope. Name whether work concerns shared Uptime design, Packet implementation, or Connections implementation.
3. The relevant code, prefabs, scenes, and serialized settings **on the working branch**; runtime code and Inspector state are executable truth about implemented behavior.
4. The full source design documents relevant to the task. `Docs/Design/connections-v0.md` is the original Connections brief, not its current mechanics contract. Older Packet-worded material may express shared Uptime design; translate concepts deliberately without importing Packet mechanics automatically.

Do not silently promote open design hypotheses into settled rules or discard substantive proposals merely because they are unimplemented. Preserve source wording and provenance when organizing documentation. Call out assumptions and preserve the current Connections scenario's small scope.

## Git, scope, and safety

- Do not write, merge, or force-update `main` without explicit user authorization. Work on focused branches; keep commits reviewable.
- Preserve unrelated user changes. Never silently discard, revert, rewrite history, or overwrite uncommitted work.
- Keep Packet's `Assets/Scenes/GameScene.unity` and `Assets/Scenes/ABScene.unity` functional unless explicitly asked to modify them. Keep Connections in `Assets/Scenes/Connections.unity` and its own runtime code paths.
- Treat the repository as one Uptime game with two prototype implementations, not as a request for a broad refactor or Packet-system cleanup.
- At handoff report branch, commit SHA(s), files changed, tests and checks **actually run**, outstanding caveats, and the manual Unity steps needed to review. Never claim Editor validation if the Editor did not run.

## Architecture for Connections

- Distinguish **physical network links** from **logical connections**. Existing `Assets/Scripts/ConnectionView.cs` names physical edges; do not reinterpret or overwrite that component's legacy behavior in-place.
- Prefer an isolated simulation host and explicit state/model types. Shared resource allocation must be authoritative, computed once per simulation step from all active demands, rather than being decided by independently resumed coroutines.
- The current Connections allocator shares target-node LOAD proportionally after baseline LOAD. Transit nodes and physical links are route geometry, not current CPU/disk/memory/bandwidth allocation constraints. Frame-rate animation must not determine LOAD allocation, WORK progress, stage transitions, or completion.
- Make behavior inspectable and causally consistent: progress, actual rate, node requested/allocated LOAD, and performance treatment must derive from the same resource model. Do not fake resource contention using display-only values.
- `Connections.unity` has a four-node, three-link topology and defaults to generated benign traffic. Authored Witness mode retains c1/c2/c3, including a multi-operation arc. Do not treat unimplemented shared Uptime systems or original V0 phase ideas as approved Connections mechanics.

## Input, terminal, and presentation

- Preserve fast, keyboard-first command entry. Default commands and information requests return promptly; consequences progress in simulation time. Do not recreate scan queues or artificial inspection delays.
- The Connections scene targets approximately one-third terminal and two-thirds network. The terminal should support both compact text messages and **structured output prefabs** (e.g., a connection comparison), with a persistent command prompt and scrollable history.
- Prefer authored scenes, prefabs, containers, and Inspector/serialized references for visible UI. Code should instantiate and bind reusable prefab elements, not procedurally build ordinary UI hierarchies. Procedural geometry for transient network paths is appropriate.
- Physical links and logical connection paths must remain visually distinguishable. Give logical connections stable short IDs and readable labels; avoid overlaps and jitter under load.
- Match the existing retro technical aesthetic, but prioritize fast information parsing, legible status, and explicit command results over strict text-only terminal fidelity.

## Validation and reporting

- For Connections allocator changes, run deterministic small examples: shared target-node LOAD oversubscription, independent targets, baseline LOAD, no oversubscription, operation transition, completion, blocking, and released resources. Record expected versus observed outputs. Use Packet-specific tests for Packet mechanics.
- For terminal and presentation changes, describe the actual scene/prefab review required: layout at 16:9, multi-connection label crowding, `inspect` output and command input focus.
- Check compile/tests using tools that are really available. If Unity/compilation is unavailable, explicitly say so and leave manual verification instructions instead of claiming success.
- Avoid building new harnesses, generators, abstractions, or broad refactors merely because they could be useful someday. Prefer the smallest test or witness that establishes the phase's correctness.

## Workflow and handoff

- ChatGPT/web discussion is a place for design hypotheses and compact handoff briefs; Codex/local Unity workflow is the default home for implementation, scene/prefab authoring, compile validation, and code commits.
- A build handoff should include: objective, agreed behavior, explicit non-goals, files/seams to inspect, and a small set of observable acceptance checks.
- When a request reveals an ambiguity, make the smallest reversible decision that preserves the stated phase, document the assumption, and surface larger design choices instead of making them silently.
- The generated ChatGPT Project Core is not yet established. When later authorized, treat it as a derived navigation snapshot; update individual source documents and preserve the full research and historical record.
