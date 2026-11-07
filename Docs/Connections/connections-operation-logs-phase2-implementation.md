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
- The authored `LogsOutput` and `LogsEntryRow` prefabs use INSPECT's verified
  identity pill and purpose title typography. `LOGS` sits below the pill;
  `TAIL N` sits at the header's right edge. Rows show timestamp, originating
  operation, activity, and optional result. They use a larger terminal-relative
  type size and truncate long activities in one line. At narrow widths each
  entry uses a deliberate two-line layout, with the activity below its time
  and operation; the optional result hides when space is too tight. There is
  no `LIVE` label.
- The pill is bound once to `state.ConnectionId`. Identity hops and Probe
  expiration cannot change the verified identity on an opened LOGS card.
- A card updates from retained `EmittedLogs` on an unscaled UI interval. It
  retains only N visible rows and lets terminal history provide scrolling.
  Height follows displayed row count. `Terminal.UpdateHistoryLayout` maintains
  the terminal's bottom-follow or scrolled-back position during height changes.

On resolution, the card immediately adopts INSPECT's muted colors and holds
its final earned tail for four unscaled seconds, then folds to a compact
outcome summary. This timing also applies to a card requested after resolution;
repeated requests can briefly show retained evidence again. A `RectMask2D`
on the resizing inner card clips rows during collapse. Reset invalidates old
cards and displays `RUN RESET`; an old card cannot rebind to a new connection
with the same ID. Destroying a card unsubscribes it from reset notifications.

## Manual demonstration

Both Connections scenes expose a `logs-demo` fixed profile for the existing
development `spawn` command. Its ambient weight is zero. The profile has a
READ then WRITE arc, ten synthetic milestones, close early bursts, and quiet
middle intervals. No ordinary ambient PURPOSE receives log content in this
phase. The default Connections scene gets the profile from built-in defaults;
ConnectionsAdvanced serializes the same zero-weight profile.

At 16:9 in each scene, start Play Mode and run `spawn logs-demo`, then use the
ID returned by the terminal with `logs <id>` and `logs -n 15 <id>`. Check narrow
terminal widths, two cards for the same ID, early burst and quiet periods,
READ-to-WRITE transition, `load`/`work`/`inspect` alongside logs, keyboard
focus and command recall, bottom-follow and manual scrollback, kill/completion,
historical retrieval and its collapse, summary clipping, and reset. The demo's roughly
two-minute uncontended operation duration allows time for command entry.

Interactive Play Mode layout and focus review remains necessary after batch
Editor witnesses. The `logs-demo` activity names are diagnostic placeholders,
not the Phase 3 PURPOSE corpus.
