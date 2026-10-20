# Working in Uptime

## Project posture / read first

Uptime currently contains the original packet-and-tick game **and** an experimental connection-based toy. Treat these as parallel playable explorations in one Unity repository, not an approved replacement or a migration.

Before making changes, read:

1. `Docs/Design/connections-toy-v0.md` when the task concerns the connection experiment (proposed design and phased acceptance criteria).
2. The relevant code, prefabs, scenes, and serialized settings **on the working branch**; runtime code and Inspector state are executable truth.
3. Existing `Docs/Design/` material for the legacy game only as necessary. Older design documents describe the packet-era prototype and are **not** automatically requirements for the toy.

Do not silently promote open design hypotheses into settled rules. Call out assumptions and preserve the toy's small scope.

## Git, scope, and safety

- Do not write, merge, or force-update `main` without explicit user authorization. Work on focused branches; keep commits reviewable.
- Preserve unrelated user changes. Never silently discard, revert, rewrite history, or overwrite uncommitted work.
- Keep the original `Assets/Scenes/GameScene.unity` and `Assets/Scenes/ABScene.unity` functional unless explicitly asked to modify them. Put the toy in its **own** scene and its own runtime code paths.
- Treat the repository as one Unity project with two experiments, not as a request for a broad refactor or legacy-system cleanup.
- At handoff report branch, commit SHA(s), files changed, tests and checks **actually run**, outstanding caveats, and the manual Unity steps needed to review. Never claim Editor validation if the Editor did not run.

## Architecture for the connections toy

- Distinguish **physical network links** from **logical connections**. Existing `Assets/Scripts/ConnectionView.cs` names physical edges; do not reinterpret or overwrite that component's legacy behavior in-place.
- Prefer an isolated simulation host and explicit state/model types. Shared resource allocation must be authoritative, computed once per simulation step from all active demands, rather than being decided by independently resumed coroutines.
- Rendering should consume simulation state; frame-rate animation must not determine CPU, memory, disk-I/O, bandwidth allocation, or completion.
- Make behavior inspectable and causally consistent: a connection's progress, actual rate, node load, and link utilization must derive from the same resource model. Do not fake resource contention using display-only values.
- Start with the four-node, three-link, benign single-stage witness in the design brief. No villains, infections, full run systems, or speculative command proliferation without a new instruction.

## Input, terminal, and presentation

- Preserve fast, keyboard-first command entry. Default commands and information requests return promptly; consequences progress in simulation time. Do not recreate scan queues or artificial inspection delays.
- The toy scene targets approximately one-third terminal and two-thirds network. The terminal should support both compact text messages and **structured output prefabs** (e.g., a connection comparison), with a persistent command prompt and scrollable history.
- Prefer authored scenes, prefabs, containers, and Inspector/serialized references for visible UI. Code should instantiate and bind reusable prefab elements, not procedurally build ordinary UI hierarchies. Procedural geometry for transient network paths is appropriate.
- Physical links and logical connection paths must remain visually distinguishable. Give logical connections stable short IDs and readable labels; avoid overlaps and jitter under load.
- Match the existing retro technical aesthetic, but prioritize fast information parsing, legible status, and explicit command results over strict text-only terminal fidelity.

## Validation and reporting

- For changes to the allocator, run deterministic small examples: shared-link oversubscription, node CPU/disk bottlenecks, no oversubscription, connection completion and released resources. Record expected versus observed outputs.
- For terminal and presentation changes, describe the actual scene/prefab review required: layout at 16:9, multi-connection label crowding, `inspect` output and command input focus.
- Check compile/tests using tools that are really available. If Unity/compilation is unavailable, explicitly say so and leave manual verification instructions instead of claiming success.
- Avoid building new harnesses, generators, abstractions, or broad refactors merely because they could be useful someday. Prefer the smallest test or witness that establishes the phase's correctness.

## Workflow and handoff

- ChatGPT/web discussion is a place for design hypotheses and compact handoff briefs; Codex/local Unity workflow is the default home for implementation, scene/prefab authoring, compile validation, and code commits.
- A build handoff should include: objective, agreed behavior, explicit non-goals, files/seams to inspect, and a small set of observable acceptance checks.
- When a request reveals an ambiguity, make the smallest reversible decision that preserves the stated phase, document the assumption, and surface larger design choices instead of making them silently.
