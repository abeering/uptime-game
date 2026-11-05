# Ambient Payload Threats V0 review

The `AmbientPayloadThreatWitness` passed in Unity 6000.3.11f1 against the isolated Advanced scene. The 12-witness Connections regression batch also passed: Ambient Payload Threat, Ambient Hostile Activity, INFECTOR, Payload Launcher, Payload Spawner, Payload, Infection, Network Health, Scan, Traffic, Recorder, and Condition. Four 600-simulation-second runs used the authored 4:2:2:1:1 activity weights and 1:1:1:1 Advanced infection weights. The counts below are observed run evidence, not a balance target. No player intervened in these runs.

| Seed | Hostile opportunities | Launcher selections | Launcher Payloads | Payload Spawner carrier plans | Installed Payload Spawners | Periodic launches | All infections applied | Infection rejections |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1701 | 22 | 0 | 0 | 6 | 2 | 26 | 6 | 26 |
| 1702 | 14 | 1 | 1 | 1 | 1 | 23 | 8 | 24 |
| 1703 | 20 | 1 | 1 | 0 | 0 | 0 | 9 | 4 |
| 1704 | 21 | 3 | 2 | 1 | 0 | 0 | 7 | 2 |

Selection counts differ from completed effects because a Launcher must complete WORK and an INFECTOR must deliver successfully. A carrier can also be canceled or remain active at the end of a sample. These runs did not include player interceptions; the existing witnesses exercise that path. Resetting seed 1701 reproduced the same selection, installation, launch, and infection-event signature.

## Scripted Play Mode frames

`Tools > Connections > Capture Ambient Payload Threats Play Mode Review` opens Advanced at 1920×1080. The batch entry point is `Uptime.Connections.EditorTools.AmbientPayloadThreatCapture.Run`, optionally with `-ambientPayloadCapturePath <folder>`. The script forces one eligible activity at a time, advances the **existing hostile opportunity clock** to select it, then pauses further ambient selections to isolate its lifecycle for capture. The INFECTOR capture similarly makes Payload Spawner the only eligible infection. These are representative mechanics and presentation frames; the normal distribution is in the seeded runs above.

1. [Ambient Launcher connection active](01-ambient-launcher-active-1920x1080.png)
2. [Launcher completes WORK and emits a red API-to-DB Payload](02-ambient-launcher-red-payload-1920x1080.png)
3. [Ambient INFECTOR connection active](03-ambient-infector-active-1920x1080.png)
4. [Payload Spawner installed, before its first cadence](04-payload-spawner-installed-1920x1080.png)
5. [First one-hop red Payload from the installed source](05-payload-spawner-first-red-1920x1080.png)

## Interactive review with normal ambient generation

Open `ConnectionsAdvanced.unity` at 16:9 in Play Mode and leave ambient generation enabled. Play several shifts without development commands, then inspect a saved run log. Check whether a Launcher connection to API provides a useful first chance to kill it, and whether its red API-to-DB strike gives a satisfying second chance to intercept. When repeated internal red Payloads occur, scan the suspected source and use PID-Select CLEAN; check that future launches stop while any already launched Payload remains in flight. Judge the frequency of both outcomes, terminal focus, path/status crowding, and whether the expanded hostile mix feels varied. Compare with `Connections.unity`, where both new ambient weights are zero. Human pacing and readability conclusions remain open.
