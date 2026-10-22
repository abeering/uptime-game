# Connections Arcade V1 — LOAD foundation

**Current status (October 22, 2026):** Integrated target-node LOAD contract from [PR #4](https://github.com/abeering/uptime-game/pull/4). The closing "later decisions" list records this phase's original boundary: operation arcs and directional presentation later merged in [#5](https://github.com/abeering/uptime-game/pull/5), generated traffic in [#6](https://github.com/abeering/uptime-game/pull/6), and run recording in [#7](https://github.com/abeering/uptime-game/pull/7). Scoring, infections, and topology generation remain outside the integrated Connections V1 slice. See [Connections Current State](Current_State.md).

**Scope:** Phase 1 implementation contract for `Assets/Scenes/Connections.unity`. This replaces the earlier CPU, memory, disk I/O, and bandwidth allocation rules for the Connections game. The older V0 briefs remain historical context. Packet is independent.

## Mechanical model

- A route's final node is its current target. Earlier nodes and physical links remain visible route geometry and do not consume or constrain LOAD.
- A node has `loadCapacity` and optional `baselineLoad`. Baseline is reserved first and is clamped to capacity.
- An active connection requests `desiredLoad` from its target. This phase uses **1 allocated LOAD = 1 WORK unit per second**. `totalWork` and `completedWork` stay operation-local; the current authored connections each have one operation.
- At each fixed simulation step, total active request at a node is summed in stable connection-ID order. Every active connection at that target receives its requested LOAD multiplied by `min(1, (capacity - baseline) / total request)`. Different targets allocate independently.
- Progress advances by allocated LOAD × step duration. A completing connection releases its LOAD in the published snapshot; peers receive the freed capacity on the next step. The completion/departure view still reads the same lifecycle state.
- `RequestedLoad` is baseline plus active requests and may exceed capacity. `AllocatedLoad` is baseline plus granted LOAD and must never exceed capacity. The node bar displays requested/capacity, caps its fill at 100%, and uses configurable ordinary, elevated, and overloaded colors. LOAD state does not indicate infection.

## Authored witness

The four-node, three-link scene remains. `c1` targets DB and requests 5 LOAD; `c2` targets AUTH and requests 5 LOAD; `c3` targets DB and requests 8 LOAD after a 3-second delay. With DB capacity 10, its 13 requested LOAD grants approximately 3.846 to `c1` and 6.154 to `c3`. `c2` keeps its full 5 LOAD at AUTH. When `c3` completes, `c1` returns to 5 LOAD on the next step. API has 1 baseline LOAD and no transit cost.

## Review

Run `Tools/Connections/Run Resource Witness` and `Tools/Connections/Run Geometry Witness` in Unity. At 16:9 in Play Mode, check the LOAD label and bottom bar on all four node cards, the 13/10 DB overload, path and identity label spacing for three connections, `inspect c1 c2`, command input focus, and c1's recovery after c3 departs.

Generation, TrafficDirector, operation arcs, scoring, infection behavior, and new animations are later decisions.
