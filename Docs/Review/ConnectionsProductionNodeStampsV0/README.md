# Connections production node event stamps V0

This review covers `Connections.unity` and `ConnectionsAdvanced.unity`. Both scenes use the existing `NodeEventStamp` prefab, with one instance per node in a dedicated `NodeStampLayer` after `NodeStatusLayer`. The production status views suppress the persistent exterior bands while keeping their compact panels and timers.

The presenter starts stamps from completed, successful events: Blackout application, infected or clean scan result, infection-removing clean, Condition application or refresh, isolation activation, interceptor activation, and each actual Payload catch. A hidden infection alone produces no stamp. Reset cancels active stamps. A later event replaces an active stamp on the same node.

## Scripted Editor captures

These are **scripted Editor renders**, not Play Mode screenshots. The capture opened each production scene in an isolated Unity project copy, applied `ScanJammed` to DB, `PortScanned` to API, and Blackout to AUTH, then rendered the existing animation at approximately 0.3 seconds. These three nodes demonstrate concurrent stamps and persistent panels at both requested resolutions.

| Scene | 1920×1080 | 1280×720 |
| --- | --- | --- |
| Connections | [capture](Connections-1920x1080.png) | [capture](Connections-1280x720.png) |
| ConnectionsAdvanced | [capture](ConnectionsAdvanced-1920x1080.png) | [capture](ConnectionsAdvanced-1280x720.png) |

## Automated checks run

- Unity 6000.3.11f1 batch Editor compilation and `NodeStatusWitness.Run`: passed in an isolated copy. It checks both scene bindings, layer order, per-node independence, artwork/tints/animation, click-through, no exterior bands, event semantics, concurrent and replacement behavior, repeated broad and targeted catches, Condition persistence, reset, and existing panel placement/state behavior.
- `NodeStatusLabWitness.Run`: passed in the same isolated copy, confirming the authored lab and its existing status/stamp interactions remain intact.
- `git diff --check`: passed.

## Manual Play Mode review

Open each production scene at 16:9, including 1920×1080 and 1280×720. Use `infect AUTH blackout` for OFFLINE, `infect DB phantom-load` followed by `scan DB` for INFECTED, and `scan` on a healthy node for CLEAN. Use `worm <nodeId> scan-jammed|port-scanned` to exercise Conditions, `isolate <nodeId>` for ISOLATED, and `payload <nodeId>` with `intercept [payloadId] @ <nodeId>` for INTERCEPT and CAUGHT. Confirm failed actions do not stamp, and try multiple statuses and nearby events while traffic runs. Check panel overlap, label readability, node centering, command input focus, and the terminal/network balance.

The automated Editor checks do not replace a human Play Mode visual and input review. No genuine Play Mode video was captured for this PR.
