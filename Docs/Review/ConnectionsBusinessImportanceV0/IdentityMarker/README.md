# PR #139 — shared teal/cyan identity and IMPORTANT `!`

Current presentation direction. [Contract and exact Inspector location](../../../Connections/connections-identity-marker-v0.md). Earlier importance-color and three-preset evidence remains historical, superseded by this pass. PR #139 stays unmerged.

All requests use the exact original six teal/cyan lifetime-slot colors. IMPORTANT uses an inline `!` in its existing network/terminal pill, with bright pale-white ID text for both classes. IDs remain unadorned for commands and lookup. No marker objects, changed packing dimensions or altered WORK shader/fill are introduced. The `!` uses 75% label size to fit existing terminal-header padding while retaining ID font size.

In **Assets/Prefabs/Connections/Display/DisplayStyle.asset → Live performance color comparison → Performance Color Treatment**:

- **Blended** retains identity-to-warning interpolation with the restored narrow palette.
- **Uniform** retains each identity's nominal (zero-danger) rails; positive-danger colors use the existing common warm progression/pulse without identity interpolation. Warning onset enters the warm family directly. Uniform remains the authored default.

Orange/red/pink targets, danger calculation, heartbeat, LOAD density, operation movement/direction, route geometry, socket packing and all importance/gameplay mechanics remain. The former ordinary presets and separate importance palette/activity colors are retired. LOGS headers now join inspect/LOAD/WORK/resolved history in shared identity/importance binding; resolved log text can remain quiet while IDs stay bright.

## Actual comparison captures

Both canonical scenes, both modes and both supported resolutions use actual scripted Play Mode with real model-generated nominal AUTH traffic alongside slowed/endangered DB traffic. Pausing the comparison clock holds state fixed for palette comparison; a running-host check also verifies live mode switching without restart or view recreation. Temporary fixture/camera setup is discarded. The capture explicitly refreshes terminal layout/clipping when switching render sizes; it does not change production layout code or simulation state for readability checks.

| View | Blended | Uniform |
| --- | --- | --- |
| Small 1920×1080 | [Blended](Captures/Connections-1920-Blended.png) | [Uniform](Captures/Connections-1920-Uniform.png) |
| Small 1280×720 | [Blended](Captures/Connections-1280-Blended.png) | [Uniform](Captures/Connections-1280-Uniform.png) |
| Advanced 1920×1080 | [Blended](Captures/ConnectionsAdvanced-1920-Blended.png) | [Uniform](Captures/ConnectionsAdvanced-1920-Uniform.png) |
| Advanced 1280×720 | [Blended](Captures/ConnectionsAdvanced-1280-Blended.png) | [Uniform](Captures/ConnectionsAdvanced-1280-Uniform.png) |
| Lab 1920×1080 | [Blended](Captures/Lab-1920-Blended.png) | [Uniform](Captures/Lab-1920-Uniform.png) |
| Lab 1280×720 | [Blended](Captures/Lab-1280-Blended.png) | [Uniform](Captures/Lab-1280-Uniform.png) |

IMPORTANT 0–100% fill comparisons use the same authored views with temporary synthetic first-column overrides: [720p Blended](Captures/Lab-1280-Blended-fill.png), [720p Uniform](Captures/Lab-1280-Uniform-fill.png), [1080p Blended](Captures/Lab-1920-Blended-fill.png), [1080p Uniform](Captures/Lab-1920-Uniform-fill.png). Remaining ordinary lab rows retain their recipes for comparison. [Small resolved/history](Captures/Connections-1280-history.png) and [Advanced resolved/history](Captures/ConnectionsAdvanced-1280-history.png) retain IMPORTANT markers and canonical IDs.

## Checks actually run

Unity **6000.3.11f1**, isolated validation copy, original open Editor/user TMP edit preserved:

- **276 focused assertions pass:** exact historical palette for every class/slot; identity-colored healthy rails in both modes; identity-independent Uniform warning samples; preserved Blended RGB result; bright text; `!` supported by authored fonts; original pill size/raw IDs/aliases; marker fit and exact WORK progress at 0/25/50/75/100%.
- Supporting regressions pass: **Business Importance 314**, **Display Lab 512**, **Visual Integration 81**, **recorder-policy 74**, **REROUTING 108**, existing **RecorderWitness** and **LogsOutputWitness**. Visual Integration retains two unavailable batch diagnostic measurements. LOGS assertions now preserve identity color on resolution while continuing to mute earned log text.
- **1,524 actual Play Mode checks pass**, **18 screenshots**: raw IDs/marker text, visible glyph ink measured from unpadded glyph metrics (not font line-height/SDF bounds), consistently bright labels, network/terminal identity consistency, rotated pills, active and collapsed history, same model/view instances, unchanged comparison WORK/time/routes, and live switching while the host continues.
- Final exact-source Editor compile and focused witness pass. Core regenerated/check and whitespace checks pass. No Small/Advanced scene serialization or gameplay/runtime-mechanics change is included; the lab scene changes its diagnostic subtitle only.

[Focused checks](deterministic-checks.txt), [actual Play Mode checks](graphical-checks.txt), [Unity summary](validation-summary.txt). Source changes are shared style/pills, LOGS presentation binding and supporting fixtures/docs. Prior #139 broad failures remain documented: Resource inspect-header geometry/identity assertion, Advanced Overclock compact row, stale Generated Identity tuning. That broad matrix was not repeated here. Graphical startup again logged the inherited nonfatal Unity QuickSearch indexing exception. No human gameplay/keyboard acceptance or universal perceptual claim is made.

## Human review

1. Open either canonical scene, enter Play, optionally choose **Tools → Connections → Load Business Importance Comparison**. `i0`/`i1` are IMPORTANT and `o0`/`o1` ordinary in this disposable fixture. Stop/reload without saving to restore authored traffic/objectives.
2. Compare `inspect -v i0 o0`, `load DB`, `work DB`, `logs i0`. `i0!` is display formatting: type the plain `i0` ID. Check bright IDs and marker fit through progress, slowdown/danger, route changes and resolved/collapsed history. Importance still costs three failure units and grants no processing preference or extra success WORK.
3. Select the shared DisplayStyle asset and flip **Performance Color Treatment** live. Healthy rails should retain their individual teal/cyan slots in both modes. Compare warnings and check pills retain identity colors/markers.
4. Repeat at **1920×1080 / 1280×720** and in **ConnectionsDisplayLab**. The lab keeps its shared assets, existing 26-sample layout and progress rack. Restore Uniform if retaining the authored review default; shared asset edits in Play Mode can persist.

INSPECT route emphasis, TAG investigative memory, importance-trust escalation and the inspect WORK-bar deadline checkpoint remain deferred. No additional gameplay feature is implemented here.
