# Connections — live node WORK panel V0

**Scope:** Connections terminal presentation in the four-node and Advanced scenes. Packet and the simulation model are unchanged.

`work <nodeId>` appends a live panel to terminal history for one node, using the same authored card header and node pill as `load <nodeId>`. The command accepts exactly one case-insensitive node ID, returns immediately, and does not consume a scan slot or alter simulation time. Panels are independent, including multiple panels for the same node. The terminal prompt remains available while panels refresh.

Each row represents an established, active logical connection targeting that node. New active connections join; completed, killed, canceled, and withdrawn connections leave. A row already present stays in place through the one-step operation transition, avoiding a brief disappearance when demand pauses between stages. Existing rows retain their order. An empty panel says `NO ACTIVE CONNECTION WORK`.

The first line contains the player-visible identity pill, claimed purpose, and inspect-style operation timeline. The timeline reuses the authored operation token prefab and inspect's width-aware packing: current operation first, then completed and future counts as space permits. The second line contains a current-operation progress bar and ETA. The bar uses `CompletedWork / CurrentTotalWork` from the simulation state. On an operation change it shows the new operation's progress; there is no whole-connection progress or forecast.

ETA uses the same calculation and text as `inspect -v`: `RemainingWork / ActualWorkRate`, rounded up to `mm:ss`, displayed as `ETA  00:35` for 35 seconds. Zero productive rate displays `ETA  --:--`. If WORK is explicitly paused while LOAD is allocated, it displays `ETA  PAUSED`. A paused simulation clock leaves the estimate unchanged. The panel does not run an independent countdown. It also does not display LOAD values, activity descriptions, performance explanations, or internal benign/hostile classification.

The panel follows LOAD's periodic refresh, dynamic row creation, width fitting, height reflow, scroll anchoring, and reset handling. Visible hierarchy is authored in `WorkOutput.prefab` and `WorkRequestRow.prefab`; the row uses the existing identity and operation pill prefabs. The small shared display methods in `InspectEntry` keep verbose inspect and WORK semantics aligned.

`Assets/Editor/WorkOutputWitness.cs` checks parsing, scene references, empty state, target membership, multiple panels, contention and ETA parity, operation transition, allocation recovery, resolution, reset, and narrow width. Interactive Unity Play Mode review remains necessary at 16:9 for long purposes, busy nodes, pill packing, scrolling, and command focus.

The [Editor preview and validation notes](../Review/ConnectionsNodeWorkPanelV0/README.md) record the authored prefab composition and checks actually run.
