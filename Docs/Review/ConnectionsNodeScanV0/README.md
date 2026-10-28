# Connections Node Scan V0 review

`scan <nodeId>` starts one eight-second scan on the Connections simulation clock. The host samples active infections at completion, then releases the scan slot. The terminal keeps an authored card in history and appends a separate completion line. The player sees only `INFECTED` or `CLEAN`; the internal recorder retains timing and player-action linkage. Packet scanning is unchanged.

## Validation

- Unity 6000.3.11f1 compiled the isolated project copy. `ScanWitness` passed healthy and infected results, infection delivery and removal during scanning, multiple infection kinds, Blackout, contention, deadline splitting under a coarse step, reset, one host completion event, recorder JSON, and prefab/scene bindings. It also checked that the progress bar has 25% width after two of eight simulation seconds.
- Existing `InfectionWitness`, `RecorderWitness`, `ResourceWitness`, `NetworkHealthWitness`, and `InspectV2Witness` passed in the same isolated project copy.
- An automated Play Mode smoke check passed: the scan card appeared in terminal history, completion appended exactly one line, another scan retained the prior card, and completion during PID-Select left the overlay and selected input unchanged. This was a scripted check, not an interactive review.
- The [1920×1080 Editor preview](ScanOutputEditorPreview.png) shows CLEAN, INFECTED, and active SCANNING cards. It is a rendered Editor composition, not a capture of interactive Play Mode or the complete Connections scene. It caught and helped fix a progress bar that initially looked full during an active scan.
- `python scripts/build_uptime_project_core.py --check` passed after regenerating the derived Core.

## Interactive Play Mode review

At 16:9 in `Assets/Scenes/Connections.unity`, run `scan DB`; confirm the card fits the terminal and its bar advances with simulation time. Execute other commands and scroll away while it runs; the card must update in place without forcing scroll. Confirm the separate completion line, then start another scan and check that the prior card remains frozen. Test both result colors, a long history, and the 100-entry retention limit. Open PID-Select with `clean DB` while a scan runs and confirm scan completion does not close it or move keyboard focus. Check command input focus after ordinary scans.
