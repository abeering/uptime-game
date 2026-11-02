# Connections — Video Call Presentation & Soft Placement V1 review

**Initial `ae35159` evidence:** The captures and validation below retain the first PR #73 presentation. See the [focused review refinement](Refinement/README.md) for the current 520×400 window, 16:9 feed, softer scrim, synchronized titlebar/bottom Escape indicators and CLEAN label-protection case.

Baseline: merged PR #72, `main` at `ddb955ba3bd9b37aea3c311c9c192c4a50e4dc49`. Connections only. See the [implementation contract](../../Connections/connections-video-call-presentation-v1.md).

## Before and after

The existing [V0 call at 1920×1080](../ConnectionsVideoCallEscapeV0/Connections-call-alone-1920x1080.png) and [V0 call behind CLEAN at 1280×720](../ConnectionsVideoCallEscapeV0/Connections-clean-over-call-1280x720.png) are the before captures from PR #72. They show the narrow portrait, separate subtitle column, titlebar hang-up label and centered CLEAN.

V1 keeps the same window size/chrome and enlarges the feed, overlays subtitles, and uses one supplied phone Sprite in the red/gray bottom control. The captures below are actual scripted Play Mode Canvas renders from Unity 6000.3.11f1 in an isolated project copy. Each full witness run holds its Canvas at its requested render size **before opening or placing any window**. Images are not OS screenshots or a claim of human keyboard/Game view review.

| Scene / size | Call alone, red | CLEAN above call, gray | Delayed call above CLEAN, red |
| --- | --- | --- | --- |
| Four-node / 1920×1080 | [call](Connections-call-alone-1920x1080.png) | [CLEAN](Connections-clean-over-call-1920x1080.png) | [arrival](Connections-delayed-call-over-clean-1920x1080.png) |
| Four-node / 1280×720 | [call](Connections-call-alone-1280x720.png) | [CLEAN](Connections-clean-over-call-1280x720.png) | [arrival](Connections-delayed-call-over-clean-1280x720.png) |
| Four-node / 1577×887 | [call](Connections-call-alone-1577x887.png) | [CLEAN](Connections-clean-over-call-1577x887.png) | [arrival](Connections-delayed-call-over-clean-1577x887.png) |
| Advanced / 1920×1080 | [call](ConnectionsAdvanced-call-alone-1920x1080.png) | [CLEAN](ConnectionsAdvanced-clean-over-call-1920x1080.png) | [arrival](ConnectionsAdvanced-delayed-call-over-clean-1920x1080.png) |
| Advanced / 1280×720 | [call](ConnectionsAdvanced-call-alone-1280x720.png) | [CLEAN](ConnectionsAdvanced-clean-over-call-1280x720.png) | [arrival](ConnectionsAdvanced-delayed-call-over-clean-1280x720.png) |
| Advanced / 1577×887 | [call](ConnectionsAdvanced-call-alone-1577x887.png) | [CLEAN](ConnectionsAdvanced-clean-over-call-1577x887.png) | [arrival](ConnectionsAdvanced-delayed-call-over-clean-1577x887.png) |

Different node homes: four-node [EXT1](Connections-clean-node-EXT1-1920x1080.png), [AUTH](Connections-clean-node-AUTH-1920x1080.png); Advanced [EXT1](ConnectionsAdvanced-clean-node-EXT1-1920x1080.png), [RPT](ConnectionsAdvanced-clean-node-RPT-1920x1080.png). CLEAN alone at small/fractional sizes: [four-node 720](Connections-clean-alone-1280x720.png), [four-node fractional](Connections-clean-alone-1577x887.png), [Advanced 720](ConnectionsAdvanced-clean-alone-1280x720.png), [Advanced fractional](ConnectionsAdvanced-clean-alone-1577x887.png).

Content avoidance: [four-node](Connections-avoided-content-obstruction-1920x1080.png), [Advanced](ConnectionsAdvanced-avoided-content-obstruction-1920x1080.png). These capture the first frame of an arriving call at an alternative candidate, preserving the older process/PID area. The witness compares protected-content intersection against the preferred upper-right home and logs the measured reduction. Constrained cases can retain some obstruction; the heuristic does not promise zero occlusion.

Minor overlap: [four-node](Connections-minor-frame-overlap-1920x1080.png), [Advanced](ConnectionsAdvanced-minor-frame-overlap-1920x1080.png). This explicitly controlled fixture stages an older call so its unused bottom frame overlaps CLEAN's normal target-node home by four reference units. It verifies positive window intersection, zero protected-content intersection, no movement of the older call, and no displacement of CLEAN from that home. The staged call position is test setup, not an extra runtime repositioning rule. Network nodes remain untouched.

Ordinary generated traffic with both surfaces: [four-node](Connections-generated-traffic-overlap-1280x720.png), [Advanced](ConnectionsAdvanced-generated-traffic-overlap-1280x720.png). The witness re-enables the scene's ordinary director and resets it, waits for live connections, then schedules a sample and opens CLEAN. It checks continued simulation and PID draft/selection/focus. This is a scripted traffic smoke review, not a human assessment of arrival rhythm or gameplay difficulty.

Long content: [four-node fractional](Connections-long-dialogue-1577x887.png), [Advanced fractional](ConnectionsAdvanced-long-dialogue-1577x887.png). The witness verifies title ellipsis, multiple subtitle pages, the final visible page and at most three lines on that page. Portrait/animation: [four-node](Connections-animated-portrait-inactive-1920x1080.png), [Advanced](ConnectionsAdvanced-animated-portrait-inactive-1920x1080.png). The test supplies an existing gradient sprite plus an in-memory Animator controller/clip and verifies actual Image animation, the `IsTalking` bool and subtitle advancement with gray controls. It is a synthetic content-binding test; no character artwork or animation asset is added.

Results: [correct PID](Connections-clean-result-with-call-1920x1080.png), [innocent PID](Connections-innocent-pid-result-1920x1080.png), [Advanced correct](ConnectionsAdvanced-clean-result-with-call-1920x1080.png), [Advanced innocent](ConnectionsAdvanced-innocent-pid-result-1920x1080.png). Correct removal, exact five-Health innocent penalty, noncancelable results and normal focus restoration remain covered.

## Checks actually run

Unity 6000.3.11f1 compiled the final isolated copy. All six full Play Mode runs passed: both scenes independently at 1920×1080, 1280×720 and 1577×887. Each included controlled placement/focus cases and ordinary generated traffic. The final Editor batch passed both-scene call/placement/chrome bindings, CLEAN Window, Infection, Network Health, Inspect V2, Resolved Inspect and Advanced Scene witnesses. Native TMP event editing, Input System UI cancellation, delayed-call duplicate/disable cleanup, both Escape orders, fresh/held/protected presses, correct/innocent PID results, live portrait animation, long content and topology invariance were exercised.

All 108 raw PNG captures passed chunk/CRC/dimension checks; 40 selected images are retained here. Byte comparisons confirmed unchanged existing scene rectangles, Packet scenes, shared source chrome, simulation/Health/node positioning, Escape authority and input adapter. The supplied PNG hash is unchanged. Project Core generation/freshness, diff whitespace and review/implementation links passed. See [validation excerpts](validation.txt) and the [changed-file manifest](changed-files.txt).

## Reproduce the checks

In Unity, the menu `Tools/Connections/Run Escape and Video Call Witness (Play Mode)` runs the four-node scene with a 1920×1080 Canvas viewport and stops Play Mode when finished. Batch runs can select scene and size:

```sh
"/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath /path/to/isolated/project \
  -executeMethod Uptime.Connections.EditorTools.EscapeVideoCallWitness.Run \
  -witnessScene ConnectionsAdvanced -witnessWidth 1577 -witnessHeight 887 \
  -capturePath /tmp/call-captures -logFile /tmp/call-witness.log
```

Run independently at 1920×1080, 1280×720 and 1577×887 for each scene. The witness temporarily routes the authored Canvas to a fixed-size camera RenderTexture for the entire run, then restores its previous camera/render mode. Window placement and capture therefore use identical geometry. It does not modify the saved scene or resize the OS/Game view. The original capture-only resize approach was rejected during review because it could show a window placed for a different viewport.

Adjacent menu checks: CLEAN Window, Infection, Network Health, Inspect V2, Resolved Inspect, and Advanced Scene. Run the static Escape/Video Call bindings check in Editor mode, where nested prefab provenance is available.

## Manual Play Mode review

1. Open each playable scene in Unity 6000.3.11f1. Use actual fixed Game view sizes 1920×1080, 1280×720 and 1577×887, setting each size before opening windows; reopen them after a size change. Leave ordinary generated traffic enabled. Confirm the full-width status strip, pane boundaries, node/path geometry and prompt stay stable.
2. Enter `calltest`. Confirm a large static feed, translucent subtitles confined to about three lines, caller title and red circular handset beside `[ESC]`. Continue typing/editing terminal commands, recall history, scroll away from the bottom and back, and submit `inspect <liveId>` / `inspect -v <liveId>`.
3. Hang up, then enter `clean DB` and repeat on another node. Confirm distinct nearby homes and a fully usable process listing/PID field. Escape should cancel without penalty and restore the prompt.
4. Enter `calltest`, then `clean DB`. The call stays where it opened and its handset/keycap turn gray. Subtitles continue. First fresh Escape cancels CLEAN; the call turns red. Hold Escape: it must remain open. Release and press again to hang up.
5. Enter `calltest delay=5`, immediately `clean DB`, then type/select a partial PID before arrival. The new call should choose a candidate without moving CLEAN, turn red, and preserve PID focus, draft and selection. First valid Escape hangs up, CLEAN's `[ESC] CANCEL` returns, and a held key must not cancel it. Release and press again to cancel CLEAN. A press inside the short arrival guard is consumed; release and retry after the guard.
6. With a call open, use `infect DB phantom-load`, then `clean DB`. Choose the anomalously high-CPU PID: infection removal and the existing result hold should work, with CLEAN no longer eligible for Escape. Repeat and choose an innocent PID: retain the failure result and five-Health penalty. The call remains available for hang-up.
7. For content review, supply `ConnectionsVideoCallData` through the documented API with a long name/dialogue, a real portrait Sprite and optional controller with `IsTalking`. Verify aspect ratio, visible animation while CLEAN owns Escape, subtitle paging/completion, and interruption during typing. The witness's synthetic sprite/clip is also available by running the menu check.
8. Confirm red and gray states use the same phone Sprite, remain legible at 720p, and expose no mouse action. Check native physical Escape, held keys, rapid arrival/dismissal, English editing and IME. Stop Play while a delayed call is pending, then restart: no old test should appear.

Caveats: candidate weights are provisional and can allow graceful overlap when space is tight; arbitrary aspect ratios, very small viewports, human traffic rhythm, physical keyboard/IME and actual Game view resizing require manual review. No character art is included. Packet gameplay was not exercised because Packet files are unchanged. The isolated Editor's pre-existing SearchDatabase startup indexing exception is recorded separately from passing gameplay assertions in validation evidence.
