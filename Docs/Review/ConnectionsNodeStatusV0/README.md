# Node Status Presentation V0 review

These 1920×1080 images came from scripted Unity Play Mode capture in an
isolated project using the authored scenes and prefab. They are Editor-driven
examples, not human validation of animation or keyboard interaction.

| Example | Capture |
|---|---|
| Normal advanced node | [status-normal.png](status-normal.png) |
| Blackout / OFFLINE | [status-offline.png](status-offline.png) |
| Scan-confirmed INFECTED after Blackout removal | [status-infected.png](status-infected.png) |
| Brief CLEAN scan acknowledgment | [status-clean.png](status-clean.png) |
| OFFLINE + INFECTED in one Pain panel | [status-grouped.png](status-grouped.png) |
| CLEAN plus Editor-only Awareness preview | [status-families.png](status-families.png) |
| Live crowded advanced topology, seed 1701 | [status-crowded.png](status-crowded.png) |
| Four-node Connections scene, grouped Pain | [status-four-grouped.png](status-four-grouped.png) |

The advanced crowded capture shows 13 active or Connecting connections at
simulation time 343 seconds. The target API was scanned after infection; a
separate naturally offline node is also visible. The screenshot checks visual
stacking near beams, identity pills, and terminal sockets. The normal and
single-status captures use generated mode with ambient generation disabled.

## Manual review in Unity

1. Open `Assets/Scenes/ConnectionsAdvanced.unity`, set Game View to 16:9, and
   enter Play Mode. Use `infect API blackout`, then `scan API`; wait for the
   eight-second scan to complete. Check that OFFLINE appears immediately,
   INFECTED joins it in one panel only after scanning, and the panel animates.
2. Use `uninfect API blackout`. The OFFLINE row should disappear while the
   scan-acquired INFECTED row remains. Run `scan API` again; the CLEAN
   acknowledgment should appear briefly without a countdown.
3. Try `clean API` with a scan-confirmed infection, select the malicious PID,
   and verify that the known-infected indicator clears without showing CLEAN.
   Confirm the terminal prompt and scan card still work.
4. Repeat an OFFLINE and scan case in `Assets/Scenes/Connections.unity`. Watch
   callout placement and band animation at 16:9 as traffic changes. Inspect a
   busy endpoint for terminal socket and identity pill readability.

The remaining artistic judgment is whether the band width, activation pop,
panel spacing, and occasional beam overlap feel right during interactive
Play Mode. No PORT SCANNED gameplay is included.
