# Connections Operation Logs V0, Phase 2: terminal viewer

This is a Connections implementation note. The Phase 1 simulation stream in
`LogicalConnectionState.EmittedLogs` remains the sole source for displayed rows.
The viewer neither calculates WORK milestones nor reconstructs entries from the
run recorder.

## Command and presentation

- `logs <id>` opens a live history card with the latest five earned entries.
- `logs -n N <id>` opens an independent card with the latest N entries, where N
  is an integer from 1 through 30. The limit is provisional and is held in
  `Terminal.MaxLogTailCount`.
- ID lookup uses `SimulationHost.FindConnection`, the same canonical lookup as
  `inspect`. Unknown IDs use the existing failed-address notification path.
- The authored `LogsOutput` and `LogsEntryRow` prefabs show the verified ID,
  target/PURPOSE where width allows, status, tail size, timestamp, originating
  operation, activity, and result. Rows truncate long activities in one line.
- A card updates from retained `EmittedLogs` on an unscaled UI interval. It
  retains only N visible rows and lets terminal history provide scrolling.
  Height follows displayed row count. `Terminal.UpdateHistoryLayout` maintains
  the terminal's bottom-follow or scrolled-back position during height changes.

A card opened while a connection is active holds its last earned tail for four
unscaled seconds after resolution, then folds to a compact outcome summary.
A card requested for an already resolved connection remains a full, readable
snapshot. Reset invalidates both kinds of card and displays `RUN RESET`; an old
card cannot rebind to a new connection with the same ID. Destroying a card
unsubscribes it from reset notifications.

## Manual demonstration

Both Connections scenes expose a `logs-demo` fixed profile for the existing
development `spawn` command. Its ambient weight is zero. The profile has a
READ then WRITE arc, ten synthetic milestones, close early bursts, and quiet
middle intervals. No ordinary ambient PURPOSE receives log content in this
phase. The default Connections scene gets the profile from built-in defaults;
ConnectionsAdvanced serializes the same zero-weight profile.

At 16:9 in each scene, start Play Mode and run `spawn logs-demo`, then use the
ID returned by the terminal with `logs <id>` and `logs -n 20 <id>`. Check narrow
terminal widths, two cards for the same ID, early burst and quiet periods,
READ-to-WRITE transition, `load`/`work`/`inspect` alongside logs, keyboard
focus and command recall, bottom-follow and manual scrollback, kill/completion,
historical retrieval, summary collapse, and reset. The demo's roughly
two-minute uncontended operation duration allows time for command entry.

Interactive Play Mode layout and focus review remains necessary after batch
Editor witnesses. The `logs-demo` activity names are diagnostic placeholders,
not the Phase 3 PURPOSE corpus.
