# PR #73 — Focused call presentation refinement

This follow-up starts from `ae3515942f89d84ba52bece158702bafafc7ab0e` on the existing `feat/connections-call-presentation-placement` branch. It changes presentation and two call-placement candidates, preserving the prior PR's call APIs, typewriter/portrait behavior, CLEAN mechanics, keyboard focus, LIFO/protection/held-key routing and delayed `calltest`. The PR remains unmerged.

## Selected presentation

- Shell: **520×400**, with the same shared 40-unit titlebar and frame.
- Feed: **488×274.5**, exactly **16:9**. It retains approximately the initial feed's pixel area while giving the window a narrower, taller silhouette. Fallback noise and aspect-preserving supplied portraits share the same region.
- Subtitle scrim: **0.60 alpha**, between the requested 0.55–0.65 range and softened from 0.80. It is inset eight units from the feed edges, with 16-unit horizontal and ten-unit vertical text padding. White text, approximately three lines per page, unscaled disclosure, completion and animation hooks are preserved.
- Titlebar: **`[ESC] HANG UP`** is restored alongside the bottom handset and `[ESC]`. Both labels share the same keycap tint from `SetEscapeTarget(bool)`: active pale red when the call owns Escape, subdued gray otherwise. The circle/handset retain their red/gray state. No new handler, click target or focus operation is added. Long caller names ellipsize in the reserved title area.

## Before and after

The parent folder retains the initial PR's full-size, 720p and fractional captures. For example: [before call](../Connections-call-alone-1920x1080.png), [before gray call behind CLEAN](../Connections-clean-over-call-1280x720.png). The new images below are scripted Unity 6000.3.11f1 Play Mode Canvas captures, with each viewport fixed before any placement.

| Scene | 1920×1080 after | 1280×720 after | 1577×887 after |
| --- | --- | --- | --- |
| Four-node | [call](Connections-call-alone-1920x1080.png) | [call](Connections-call-alone-1280x720.png) | [call](Connections-call-alone-1577x887.png) |
| Advanced | [call](ConnectionsAdvanced-call-alone-1920x1080.png) | [call](ConnectionsAdvanced-call-alone-1280x720.png) | [call](ConnectionsAdvanced-call-alone-1577x887.png) |

Both indicators subdued while CLEAN owns Escape: [four-node 720p](Connections-clean-over-call-1280x720.png), [Advanced 720p](ConnectionsAdvanced-clean-over-call-1280x720.png). Long titles and fully paginated dialogue: [four-node fractional](Connections-long-dialogue-1577x887.png), [Advanced fractional](ConnectionsAdvanced-long-dialogue-1577x887.png). Portrait/animation with subdued controls: [Advanced](ConnectionsAdvanced-animated-portrait-inactive-1920x1080.png); this uses the existing synthetic sprite and in-memory animation test, without adding character artwork.

## CLEAN title and PID/CPU headings

CLEAN's existing title rectangle is now included in its serialized protected content, alongside the process body (which contains the heading row), PID input and Escape hint. The same small candidate list gains two positions adjacent to an existing CLEAN window: immediately below it, or just to its left with eight units of permitted frame overlap. Score weights and CLEAN's own candidates are unchanged. Placement still happens only on opening and never moves the older window.

The focused witness stages a CLEAN window so the preferred upper-right call home would cover both its title and PID/CPU headings. It verifies that setup, then opens the actual sample call. The selected adjacent candidate must have **zero intersection with the title, heading row, process listing and PID input**, preserve CLEAN's exact position, and retain the PID draft/selection/focus. See [four-node](Connections-title-heading-collision-1920x1080.png) and [Advanced](ConnectionsAdvanced-title-heading-collision-1920x1080.png). This controlled fixture exercises the reported type of obstruction; it does not claim to reproduce an unspecified physical Game view size from the user's interactive review.

Natural node cases are also measured and captured. [CLEAN/API before](Before-Connections-call-over-clean-node-API-1920x1080.png) is a fresh baseline capture from the initial PR; [four-node after](Connections-call-over-clean-node-API-1920x1080.png) and [Advanced after](ConnectionsAdvanced-call-over-clean-node-API-1920x1080.png) use the refinement. Process samples differ between runs. Under tight space, natural windows may still overlap margins or the far-right portion of the PID border; the camera is taller and this is not a zero-overlap guarantee. Review the visible listing, prompt and heading text, rather than treating all intersecting window pixels as useful content.

## Validation actually run

Unity 6000.3.11f1 compiled the refinement. All six full Play Mode witness runs passed: both scenes at 1920×1080, 1280×720 and 1577×887. Every owner assertion checks both indicators; all 36 natural node cases had zero title/heading rectangle intersection. The controlled collision case passed at every scene/size while retaining position, PID focus/draft/selection and zero overlap with all protected CLEAN content. Existing subtitle/animation, fresh/held/protected Escape, delayed call, correct/innocent PID, generated-traffic and topology checks also passed.

The final Editor batch passed call/placement/chrome bindings in both scenes, CLEAN Window, Infection, Network Health, Inspect V2, Resolved Inspect and Advanced Scene witnesses. All 170 raw before/after PNGs passed integrity/dimension checks; 16 selected images are retained here. Preserved-source comparisons, Core generation/freshness, review links and diff checks passed. [Validation excerpts and caveats](validation.txt); [files changed in this follow-up commit](changed-files.txt).

## Manual review

Repeat in both scenes at 1920×1080, 1280×720 and 1577×887, setting the size before opening windows:

1. `calltest`: check the 16:9 feed, softer inset subtitle overlay, caller identity and both active Escape indicators. Continue typing/scrolling/inspecting in the terminal.
2. Open CLEAN after a call. Both call labels and handset control become subdued together; video/subtitles/animation continue. First fresh Escape cancels CLEAN and reactivates both call indicators. Held Escape must not also hang up.
3. `calltest delay=5`, immediately `clean DB` (and repeat on `API` or another node), then type/select a PID. The arriving call must preserve focus, draft, selection and CLEAN position. A valid fresh Escape hangs up; the next fresh press can cancel CLEAN. Keep the existing short target-change guard in mind.
4. Check the title, PID/CPU headings, full process listing and typed PID under both opening orders. Some frame/margin overlap is intentional. Correct and innocent PID results, noncancelable result hold and prompt restoration should remain normal.
5. Supply a long caller name/dialogue and a real portrait/controller through the existing API, or run the focused witness's synthetic portrait/animation case. Confirm text paging and animation during subdued Escape ownership.

The full scripted witness remains available through `Tools/Connections/Run Escape and Video Call Witness (Play Mode)` or the [parent reproduction commands](../README.md#reproduce-the-checks). Physical keyboard/IME and native Game view interaction remain manual. The isolated Editor's existing SearchDatabase startup exception is reported separately from gameplay assertions.
