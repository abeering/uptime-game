# Payload Spawner Infection V0 review

`Tools > Connections > Capture Payload Spawner Play Mode Review` runs the Advanced scene at 1920×1080 and writes scripted frames here. Batch entry point: `Uptime.Connections.EditorTools.PayloadSpawnerCapture.Run`, optionally with `-payloadSpawnerCapturePath <folder>`. The script manually injects API, pauses ambient generation to isolate the sequence, advances simulation time, uses the terminal command path for interception, scan, and CLEAN, and submits the listed malicious PID. It checks that no new attempt occurs after cleaning.

The November 5, 2026 Play Mode run passed and produced these frames:

1. [Before first launch, at 12s](01-hidden-source-before-first-launch-1920x1080.png)
2. [First red Payload establishing, at 20.4s](02-first-red-establishing-1920x1080.png)
3. [Second launch, at 40.4s](03-second-launch-over-time-1920x1080.png)
4. [Targeted interceptor armed](04-interceptor-armed-1920x1080.png)
5. [Scan reports the hidden source infected](05-scan-discovered-source-1920x1080.png)
6. [PID selection for CLEAN](06-pid-select-source-1920x1080.png)
7. [CLEAN removes the source](07-clean-success-1920x1080.png)
8. [No further launches after 45 more simulation seconds](08-no-further-launches-1920x1080.png)

The final frame's scan card is historical output. The live API infection badge has cleared. Scripted frames are presentation evidence, not a substitute for human playtesting.

## Interactive review

In `ConnectionsAdvanced.unity`, play at 16:9 **with normal ambient traffic enabled**. Run `infect api payload-spawner 20` and observe whether repeated internal red strikes prompt investigation of API. Use `intercept <payloadId> @ <destination>` or node-wide `intercept @ <destination>` to catch one strike, then `scan api` and `clean api`; submit the suspicious PID. Check that new launches stop while any Payload already traveling still reaches its destination unless intercepted. Repeat with `infect <otherNodeId> payload-spawner 20` to check adjacent internal target selection. Allow repeated strikes against a node already carrying Phantom LOAD and inspect the run log for `infection_rejected` without stacking. Judge command focus, scan/CLEAN readability, and red path crowding with the physical keyboard.
