# Connections SFX V0

**Scope:** Connections implementation in `Connections.unity` and `ConnectionsAdvanced.unity`. The scene-owned `ConnectionsSoundController` uses the existing `AudioCue` data shape and `SoundEffect` source prefab. It does not use Packet's global `AudioManager` or change Packet playback.

## Routing

| Cue | Successful event |
| --- | --- |
| `worm_status` | `ConditionApplied` with a Worm branch ID, including PortScanned and ScanJammed refreshes |
| `status_portscan` | `ConditionApplied` with a connection source ID and PortScanned, excluding Worms |
| `intercept_success` | PayloadDirector's caught Payload event |
| `chat_notification` | A message delivered into the visible TextCommunicationDirector stack |
| `command_error` | A rejected submitted terminal command, once per submission even when several targets fail |
| `node_infected_2` | An infected diagnostic scan result |
| `packet_spawn` | A successful logical connection registration, regardless of intent |
| `scan_complete_benign` | A clean diagnostic scan result |
| `probe_sweeping_loop` | While the authoritative host has an active Probe link and remaining simulation time |
| `video_call_open` | A call that successfully opens and becomes visible, including `calltest` |

The terminal reports command failure explicitly while dispatching submitted commands. Later scan results and CLEAN feedback can use the same error color without emitting command-error audio. One-shot sources are pooled under the scene object, while Probe has a separate loop source. Playback uses 2D sources with play-on-awake off and stops on controller disable; Probe also stops on host reset, expiry, or disable. The controller has a master volume, per-cue volumes, and separate configurable cooldowns for arrivals and message bursts. Clip/pitch variation uses a local audio random stream, leaving simulation and traffic randomness untouched. Missing references or clips are silent.

## Review

Run `Tools/Connections/Check Sound Bindings`, then `Tools/Connections/Run Sound Witness (Play Mode)` in each scene with `-witnessScene Connections` or `ConnectionsAdvanced` for batch runs. The witness checks scene clip/source references, exclusive Worm/recon routing, scan outcomes, once-per-command errors, message cooldown, call opening, and Probe expiry/reset/disable. Listen in Play Mode at 16:9: `worm`, `portscan`, `scan`, `payload` plus `intercept`, `probe`, `notifytest burst`, and `calltest`. Also try an invalid multi-target command, a successful command, ambient arrivals, Probe reset and expiry, and leaving each scene. Adjust serialized cue volumes after listening; the initial values are provisional.
