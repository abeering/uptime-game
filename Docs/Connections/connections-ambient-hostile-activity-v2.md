# Connections — Ambient Hostile Activity V2

**Scope:** Connections ambient generation on the Advanced Scenario branch. This phase integrates the existing Payload and Worm entities; it does not change their movement, effects, interception, isolation, LOAD, WORK, or Health rules. Packet is unchanged.

## One hostile opportunity clock

`ConnectionTrafficDirector` retains its single exponential hostile opportunity schedule and its independent benign coverage process. At each hostile opportunity, `HostileActivityDirector` makes one weighted choice among ordinary LOADER/INFECTOR generation, Reconnaissance, Payload, and Worm. A zero weight disables that ambient choice without disabling the developer command. The Advanced scene begins at a 30-second mean and 4:2:2:1 weights; its 50% Variable Payload and 70% SCAN JAMMED Worm chances are playtest hypotheses. The four-node scene keeps its prior mean and ordinary-only activity weights.

An opportunity result retains the selected type, selection roll, success or failure reason, and the produced connection, reconnaissance event, Payload, or Worm ID. Connection ID exhaustion can still prevent ordinary and Reconnaissance plans; it does not stop later Payload and Worm opportunities. An available Payload origin is drawn from the scene's weighted ingress configuration, then a reachable *internal* target is chosen. Internal means a node outside the configured ingress list. Payload routes and hop timings remain the Payload Director's seeded mechanics. Worms choose a currently non-isolated configured ingress and use their unchanged propagation settings. Isolation does not become a Payload countermeasure. Ambient selectors may fail cleanly if no reachable internal Payload target or available Worm ingress exists.

The activity selector, ordinary hostile recipes, benign purpose sampling, Payload route/timing, Worm propagation, and hostile arrival clock retain deterministic seeded state. Large witness steps with ambient hostility enabled are processed in one-second pieces, so due opportunities produce entities during the step and those entities move from their creation time. This matches repeated one-second simulation steps; regular fixed-step Play Mode timing remains unchanged.

## Recorder

`hostile_activity_selected` stores all four configured weights, its selection roll and scheduled time, the produced identity, selected origin/target, Payload profile or Worm effect, and a failure reason when selection cannot produce an entity. `hostile_opportunity` carries the same resulting identity and outcome alongside its next interval. The run metadata records the weights and profile/effect probabilities. Existing entity and event receipts continue to record hidden routes, target effects, branch lineage, Reconnaissance follow-up, and service consequences. None of this hidden selection data appears in ordinary `inspect` output.

## Manual review

Reopen `Assets/Scenes/ConnectionsAdvanced.unity` from disk, enter Play Mode without issuing developer spawn commands, and leave ambient generation on. Observe both quiet and busy stretches at 16:9. Watch for a Payload while inspecting ordinary traffic, a Worm outbreak while another suspicious connection is active, and Reconnaissance or infection pressure alongside either entity. Try deliberate prioritization: ignore one pressure briefly while investigating or intercepting another. Check `inspect`, command entry focus, status placement, lane readability, and Health receipts during a mixed interval. `payload`, `worm`, `portscan`, `spawn`, and other developer commands should remain available when ambient hostile generation is disabled. Repeat in the four-node scene to confirm its ordinary hostile baseline remains familiar.

Scripted Play Mode captures and four seeded 600-second experiments are documented in the [review](../Review/ConnectionsAmbientHostileActivityV2/README.md). They show reproducible system behavior and are not a human playtest verdict about fairness or pacing.
