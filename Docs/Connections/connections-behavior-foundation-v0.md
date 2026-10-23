# Connections — behavior and modifier foundation (Slice 3A)

**Status:** Slice 3A foundation merged; Slice 3B adds reusable OVERLOAD and SPIKING LOAD keywords and manual-only examples. The Slice 3A timed adjustment remains for deterministic witnesses.

## Ownership and terms

The **Connection Plan** is the intended sequence in `LogicalConnectionDefinition`: ordered operation definitions, completion effects, and optional behavior definitions. `ConnectionSpawnPlan` remains a different object: a concrete scheduled arrival with an ID, route, and copied plan data. Existing `basic-threat`, `delayed-threat`, and benign profiles have no modifier behaviors and retain their previous mechanics.

Each logical connection owns one `ConnectionBehaviorRuntime`. It instantiates the same small timed modifier behavior at either connection scope or operation scope. Connection-scope instances keep elapsed simulation time and active LOAD adjustments across operation transitions. Operation-scope instances begin when their operation starts and stop when it ends. A connection-scope WORK delta is rejected in this slice: whether persistent WORK belongs to a connection or a particular operation remains a design question. Operation-scoped WORK deltas are supported.

The Slice 3A timed adjustment adds one modifier at an authored elapsed time and can remove it at a later elapsed time; zero stop time keeps it until scope end. Slice 3B adds typed LOAD-only behaviors using the same executor and active modifier collection:

- **OVERLOAD** waits its nonnegative delay, adds one positive LOAD modifier, and holds it until scope end. Zero delay applies before the operation's first allocation.
- **SPIKING** starts in a normal phase, waits a positive normal interval, adds a positive LOAD modifier for a positive elevated interval, removes it, and repeats. Each burst gets a new modifier ID. Connection-scope phase and timer persist across operation transitions; operation-scope phase starts with its operation.

Behavior timers advance by fixed simulation delta even with zero allocated LOAD. At each step start, the runtime reconciles every threshold reached during previous steps, in phase order within each behavior, then allocates once using the resulting phase. Connection-scope definitions execute before operation-scope definitions, in their authored order. A threshold exactly at the step start takes effect in that step. If one coarse step spans several SPIKING boundaries, their additions and removals are all recorded at the next active step start in order, and only the final phase determines that step's LOAD; the preceding step used its starting phase. If the scope ended before that next step, no further phase change occurs. This preserves the host's one-allocation-per-step rule; coarse steps do not retroactively divide WORK progress among phases. An operation transition still activates its next operation on the following step.

Active modifiers are passive data with a connection-local unique ID, source behavior ID, connection or operation scope, operation index (`-1` for connection scope), and WORK/LOAD deltas. The runtime is the only owner of addition and removal. Every change appends a timestamped entry to that connection's modifier history and is also observed by the run recorder. Removing a modifier does not remove its history. No gameplay state is reconstructed from history.

For the current operation, effective required WORK is base WORK plus applicable active WORK deltas; effective requested LOAD is base LOAD plus applicable active LOAD deltas. Effective WORK must remain finite and positive; effective LOAD must remain finite and nonnegative. The host advances behaviors before its existing proportional allocator computes demand. WORK still advances only by allocated LOAD × fixed-step duration. A reduction of effective required WORK below already accomplished WORK completes through the ordinary completion path without reducing `CompletedWork`. Completion effects still fire only on normal WORK completion; `kill` stops behavior instances, removes their active modifiers, and does not fire unfinished completion effects.

Inspection, node bars, pulses, and snapshots consume the effective values through existing connection-state properties. The recorder retains base definitions, keyword kind and timing, effective snapshots, operation completion facts, and attributable `modifier_added`/`modifier_removed` events. The keywords do not classify a connection as hostile or deliver payloads; an operation's completion effect remains independent. Detailed modifier history is diagnostic data; there is no player-facing `logs` command or special threat presentation.

## Deterministic review

`Tools/Connections/Run Behavior Witness` uses only explicitly queued plans with the scene's four-node topology. It checks a base 2 LOAD operation with connection +3 and operation +5 modifiers (effective 10), same-step allocation, removal back to 5 then 2, unchanged authored values, operation cleanup, persisted connection timer, proportional sharing, ordered source-attributed history, zero-allocation time advancement, a WORK requirement reduced below 8 accomplished WORK, kill cleanup, independent concurrent instances, and reset/replay. Existing Resource, Traffic, Recorder, and Geometry Witnesses cover the no-modifier baseline and retained threat behavior.

Slice 3B extends that witness with immediate and delayed OVERLOAD boundaries, repeated SPIKING, cross-operation connection timing, operation cleanup, additive composition, zero-allocation timing, a step crossing five SPIKING boundaries, per-instance state, reset/replay, recorder provenance, and manual benign/hostile profile effects. `spawn spiking-threat`, `spawn spiking-benign`, and `spawn overload-demo` are manual-only on the default scene catalog. All use the existing profile → concrete spawn plan → registration path.

## Deferred

Connection-wide persistent WORK ownership, changes to operation sequence, multiplicative or conflicting modifiers, external reactions, more complex lifetime rules, and player-facing explanations remain open. The narrow typed keyword data is not a general scripting language.
