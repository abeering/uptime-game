# Connections — Text Communications V0 review

Baseline: `main` at `c7a857330a49386207c73a4d9e52a5934ad07b51`, after merged PR #73. This is Connections implementation/presentation work. [Implementation contract and API](../../Connections/connections-text-communications-v0.md).

## Captures

These are actual Unity 6000.3.11f1 scripted Play Mode Canvas renders from an isolated project. Each run fixes its render viewport before notification sizing or window placement. They are not OS screenshots or evidence of physical keyboard/IME review.

| Scene / viewport | Messages with CLEAN and a call | Mixed stack with generated traffic |
| --- | --- | --- |
| Connections / 1920×1080 | [windows and messages](Connections-windows-and-messages-1920x1080.png) | [traffic](Connections-generated-traffic-1920x1080.png) |
| Connections / 1280×720 | [windows and messages](Connections-windows-and-messages-1280x720.png) | [traffic](Connections-generated-traffic-1280x720.png) |
| Connections / 1577×887 | [windows and messages](Connections-windows-and-messages-1577x887.png) | [traffic](Connections-generated-traffic-1577x887.png) |
| Advanced / 1920×1080 | [windows and messages](ConnectionsAdvanced-windows-and-messages-1920x1080.png) | [traffic](ConnectionsAdvanced-generated-traffic-1920x1080.png) |
| Advanced / 1280×720 | [windows and messages](ConnectionsAdvanced-windows-and-messages-1280x720.png) | [traffic](ConnectionsAdvanced-generated-traffic-1280x720.png) |
| Advanced / 1577×887 | [windows and messages](ConnectionsAdvanced-windows-and-messages-1577x887.png) | [traffic](ConnectionsAdvanced-generated-traffic-1577x887.png) |

The [four-node long mixed stack](Connections-mixed-stack-1920x1080.png) and [Advanced long mixed stack](ConnectionsAdvanced-mixed-stack-1920x1080.png) demonstrate dynamic heights, a monitor icon with amber rail, person fallbacks with teal rails, and a supplied Sprite. The [single supplied-avatar example](Connections-im-avatar-1920x1080.png) and [Advanced example](ConnectionsAdvanced-im-avatar-1920x1080.png) use Unity's existing circular Sprite as a **synthetic binding fixture**, not a new character portrait. Ordinary messages show the neutral silhouette; a real character Sprite can be supplied through the same API. No character artwork or roster is added.

The window captures retain a selected PID draft during incoming notification delivery. The notification stack never moves CLEAN or the call on arrival. The generated-traffic captures include the scene's actual randomized traffic, so active IDs and LOAD values differ between runs. Some network cards can sit behind the fixed stack, especially in Advanced; topology is not repositioned to accommodate it. Overlap under constrained conditions remains permissible.

## Automated validation

All six focused Play Mode runs passed: both scenes independently at 1920×1080, 1280×720 and 1577×887. The witness checks:

- Optional supplied Sprite, person fallback, monitor fallback, authored teal/amber colors, explicit timestamp and workstation-clock snapshot.
- Immediate full body assignment, complete wrapping of a long paragraph, variable heights, viewport bounds and newest-first nonoverlapping settled live entries.
- Arrival fade, expiration and departure at `timeScale = 0`, smooth reflow, fifth-message eviction, incoming messages during departure, and a 100-message synchronous burst with bounded live/departing lists.
- Terminal focus/draft/selection preservation, delayed arrival during CLEAN PID selection, unchanged open window positions, incoming text during a call, ordinary text excluded from Escape, fresh/held/next-fresh dismissal behavior.
- Delayed developer duplicate rejection, malformed commands, reset cancellation, terminal disable cancellation, component/container disable and final entry cleanup.
- Actual ordinary generated traffic and unchanged network pane geometry, node positions and physical link count.

Unity compiled the code, and the adjacent Editor batch passed Text Communication bindings, both-scene Escape/Call bindings, Resource, Geometry, Traffic, CLEAN Window, Infection, Network Health, Inspect V2, Resolved Inspect and Advanced Scene witnesses. The existing full Escape/Video Call Play Mode witness also passed in both scenes at 1280×720. A final adjacent Editor batch passed against the final implementation. Scene block preservation, all 16 retained PNG integrity/dimension checks, documentation links and Project Core freshness passed. See [validation excerpts](validation.txt) for observed outputs and final regression results.

The initial focused run failed because a test assumed randomized traffic would already be active after a fixed wait. The corrected witness waits for the scene's real first active connection, with its overall timeout still enforced. All six final runs passed with that correction. This changed the witness, not traffic generation.

A pre-existing Unity SearchDatabase startup indexing exception appears in isolated Play Mode logs. It did not prevent the focused assertions or captures. It is recorded separately from gameplay results. Packet files are unchanged and Packet Play Mode was not exercised. Physical keyboard/IME, actual native Game view and subjective reading comfort remain manual checks.

## Reproduction

Run `Tools/Connections/Run Text Communication Witness (Play Mode)` in Unity 6000.3.11f1. It defaults to the four-node scene and a persistent 1920×1080 render viewport. For batch review:

```sh
"/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath /path/to/isolated/project \
  -executeMethod Uptime.Connections.EditorTools.TextCommunicationWitness.Run \
  -witnessScene ConnectionsAdvanced -witnessWidth 1577 -witnessHeight 887 \
  -capturePath /tmp/text-communication-captures -logFile /tmp/text-communication-witness.log
```

Repeat for both scenes and each listed size. `Tools/Connections/Check Text Communication Bindings` checks scene/prefab references in Editor mode. The existing `Tools/Connections/Run Escape and Video Call Witness (Play Mode)` remains independently available.

## Manual Unity review

In **both scenes**, set the Game view to **1920×1080, 1280×720 and 1577×887**, before opening windows:

1. Leave ordinary generated traffic on. Enter `notifytest im`, `notifytest system`, then `notifytest burst`. Check the compact messenger shape, prominent person/monitor slot, teal/amber rails, sender/time row, full text without typing, quick fade/slide, expiry and stable reflow. The cap is four live messages; an evicted message may briefly fade beside replacements.
2. Use the documented API with an actual portrait Sprite, short text, explicit newlines and a long paragraph. Check aspect ratio, readable wrapping and timestamp snapshot. Wait for natural expiration; repeat a burst while older messages are fading. Very large content exceeding the entire viewport uses ellipsis and should be shortened by its author.
3. Enter `notifytest im delay=5`, then type and select part of a terminal command without submitting it. Arrival must preserve focus, text, caret/selection and history scrolling. Submit `inspect <liveId>` and continue editing normally.
4. Enter `notifytest system delay=5`, immediately `clean DB` (repeat with `API` or another scene node), then type/select a PID draft. Arrival must preserve the PID, selection and input focus. The context menus on TextCommunications also deliver while CLEAN owns typing.
5. Open `calltest`, then messages, then CLEAN. Repeat CLEAN → delayed messages → call. Existing windows must stay fixed when text arrives. Newly opened windows should prefer avoiding actual message content, while small frame overlap is acceptable. Check call subtitles/controls, CLEAN title/PID/CPU headings, process rows and PID field for readability.
6. With CLEAN then a call open, a fresh Escape hangs up the call; holding it must leave CLEAN open. Release and press again to cancel CLEAN. Reverse the opening order and verify CLEAN dismisses first. Notifications should survive either Escape until their own expiry.
7. Keep live traffic running. Check topology, multi-connection labels, `inspect` output, command prompt, CLEAN's normal correct/innocent PID results and terminal focus restoration. Text must not pause or alter the simulation, expose hidden intent, or imply a real incident.
8. Disable/re-enable TextCommunications while messages are arriving/departing; reset the simulation; stop Play with a delayed test pending and restart. No old message should return. Check native physical keyboard editing, held Escape and IME as applicable.
