# Connections Run Recorder V0

**Current status (October 22, 2026):** Integrated run-recording contract from [PR #7](https://github.com/abeering/uptime-game/pull/7). The example and witness results below belong to that milestone; the listed interactive Play Mode checks were not completed during integration. See [Connections Current State](Current_State.md).

Connections records each Play Mode run automatically. The recording starts when `SimulationHost` resets at Play start, creates a JSON file immediately, saves it every 30 seconds of real elapsed time, and saves once more when Play Mode stops. Saving updates the same run file. A reset or new Play Mode session gets a new UTC timestamp and random run ID in its filename.

In the Unity Editor, find files at `<project-root>/Logs/UptimeRuns/`. The folder is created automatically and ignored by Git. Standalone builds use `Application.persistentDataPath/UptimeRuns/`. Filenames follow `uptime_YYYYMMDD_HHMMSS_seed-XXXXX_RUNID.json`, using the run's UTC start time. The output is ordinary UTF-8 JSON and can be attached directly to a ChatGPT conversation.

## Commands

| Command | Result |
|---|---|
| `mark <note>` | Record the exact note, link it to an immediate authoritative network snapshot, and save the file. |
| `log save` | Update the run file immediately. |
| `log dir` | Print the absolute output directory. |
| `log folder` | Open that directory in Finder or the platform file manager from the Unity Editor. |

Explicit saves print the absolute file path in the Unity Console. Save errors appear in the Console and terminal; they do not stop the simulation. The recorder writes a temporary file in the same directory, then atomically replaces the previous JSON. A crash can leave the temporary file, while the last successful run file remains readable.

## Format

`schemaVersion` is currently `1`. The top-level `run` object contains the unique ID, UTC start/end/save timestamps, Unity version, scene, run mode, seed, fixed step, copied node configuration, and copied ambient profile/cadence configuration. `events` has ordered sequence numbers and simulation timestamps for plan queueing, concrete connection spawns, operation starts and completions, resolution, player actions, and markers. A spawn event embeds the route and all generated operation LOAD/WORK values. Marker events carry a zero-based `snapshotIndex` into `snapshots`.

Threats V0 Slice 1 adds manual `plan_queued` events (`source: manual`), Killed resolutions and `effect` events. An infection-delivery event carries the connection ID, completed operation index, `effectType: InfectionDelivery`, `targetNodeId`, and simulation time. The operation-completed event precedes its effect, which precedes final connection resolution. Profile and concrete operation records include internal hostile classification and completion-effect settings. These are additive fields in schema version 1; the linked example predates this slice and retains its original Blocked action as a historical recording.

[`connections-run-recorder-example.json`](examples/connections-run-recorder-example.json) is the complete file saved by a Recorder Witness run. It includes a two-operation connection, a bookmark, autosave, and a blocked high-demand connection.

Snapshots are taken at about one-second **simulation** intervals after allocation, plus immediately for markers and blocks. Each contains authoritative requested and allocated LOAD for every node and current WORK, operation, demand, allocation, and performance for pending or active connections. A large manual simulation step yields one snapshot at the actual observed time; the recorder does not invent intermediate states. Autosaves use **real elapsed** time, including while simulation time is paused.

The recorder is observational. It does not consume the traffic generator's random stream, change spawn cadence, allocate LOAD, advance WORK, or affect completion. It has no log viewer, cloud upload, or gameplay analytics. Completed connection details remain in events and snapshots even after live simulation ownership retires them.

## Review

Run `Tools/Connections/Run Recorder Witness` in the Editor for schema, exact generated definitions, event order, snapshots, marker persistence, repeated save behavior, autosave timing seam, final save, distinct runs, and recorded/unrecorded gameplay parity. Keep the Resource, Geometry, and Traffic Witnesses passing.

For visual Play Mode review, play `Connections.unity` at 16:9. Confirm a file appears immediately, its modification time advances after 30 real seconds (including with `Time.timeScale` paused), `mark test observation` persists its note and linked snapshot, `log save` updates the same file, `log dir` and `log folder` lead to the same directory, stopping Play Mode records an end time, and a second Play Mode session creates a different file. Check terminal input focus after each command. Batch Editor witnesses cannot directly confirm the Finder window or interactive Play Mode stop.
