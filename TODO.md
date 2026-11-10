# Main priorities

- hacker duels 
- trace/block 
- logs 

# TODO — WAVING / Natural LOAD Noise

WAVING V0 works well, but the one-decimal LOAD readout can feel metronomic because its changes occur at predictable intervals.

When introducing WAVING to generated connections, evaluate it alongside existing natural LOAD noise before changing anything.

If necessary, experiment with faster, irregular noise cadence (~1 second) and a small absolute jitter floor (~±0.1 LOAD), preserving the recognizable underlying waveform.

Goal: make individual LOAD changes feel organic without sacrificing the larger behavioral pattern.

# bug - worms size on longer edges 

Maybe make it move faster at the same size rather than become elongated 


## TODO — Canonical Connection & Threat Generation Grammar

We need to revisit and formalize the **vocabulary, composition rules, and generation architecture** for Connections, both benign and hostile.

Specifically:

1. **Keywords / behaviors:** How are modifiers like SPIKING, OVERLOAD, WAVING, etc. selected and attached to connections or individual operations?
2. **Infection selection:** How do INFECTORs choose what infection they deliver, with what parameters and under whose control (ambient generation, villain, level, difficulty)?
3. **Spawner repertoires:** How do infections determine what they generate—LOADERs, INFECTORs, Payloads, status-applying connections, additional Spawners, etc.?

**The important goal is composability.** We should be able to express combinations like a *SPIKING INFECTOR that installs a Payload Spawner*, or a Spawner that produces different kinds of hostile connections, without building bespoke systems for every combination.

Audit what the current architecture already supports versus what is hardcoded or restricted. Then establish a coherent generation grammar that future level directors, villains, difficulty settings, and authored encounters can all leverage.

**Not an immediate refactor.** Preserve this as important design/architecture debt to address deliberately before generation complexity grows much further.