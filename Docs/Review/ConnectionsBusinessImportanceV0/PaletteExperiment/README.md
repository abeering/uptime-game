# PR #139 — live palette comparison

Connections presentation refinement on `feature/connections-business-importance-v0`. [Contract and exact Inspector location](../../../Connections/connections-palette-experiment-v0.md). PR #139 stays unmerged.

Open **Assets/Prefabs/Connections/Display/DisplayStyle.asset → Live palette experiment**. Change **Ordinary Identity Palette** and **Performance Color Treatment** during Play Mode. Both canonical scenes and the lab reference this same asset. Authored defaults: **Muted Hues + Uniform Performance**.

- Ordinary **Uniform** uses one neutral identity; **Muted Hues** preserves the exact six current hues; **Grayscale** uses six distinct achromatic shades (0.53–0.93).
- IMPORTANT identity remains the original bright cyan/teal family. All pills retain identity through nominal, slowed and endangered states.
- **Uniform Performance** uses a common neutral base and the existing continuous warning targets/pulse, producing equal rail colors for equal danger/phase across families and slots. This includes nominal rails. **Blended** retains the previous identity-to-warning interpolation. Socket/body colors follow the applicable rail treatment; geometry, size and placement do not change.
- Active inspect, LOAD, WORK and frozen/collapsed history pills update live through the authoritative palette. Color refresh does not rebind identity, recreate connections or reset WORK/progress/alias emphasis.

## Six-way comparison

All images are actual **1280×720 Play Mode** captures. Each scene's six modes use the same logical connections, path instances, geometry, WORK and paused heartbeat/simulation time. DB has slowed/endangered ordinary and IMPORTANT jobs; AUTH has nominal ordinary and IMPORTANT jobs. Activity and warning severity remain independent. The clock pause permits matched comparisons; a separate running-host assertion verifies live switching also works without pause/restart.

| Ordinary palette | Performance | Display Lab | Small | Advanced |
| --- | --- | --- | --- | --- |
| Uniform | Blended | [Lab](Captures/Lab-Uniform-Blended.png) | [Small](Captures/Connections-Uniform-Blended.png) | [Advanced](Captures/ConnectionsAdvanced-Uniform-Blended.png) |
| Uniform | Uniform | [Lab](Captures/Lab-Uniform-Uniform.png) | [Small](Captures/Connections-Uniform-Uniform.png) | [Advanced](Captures/ConnectionsAdvanced-Uniform-Uniform.png) |
| Muted Hues | Blended | [Lab](Captures/Lab-MutedHues-Blended.png) | [Small](Captures/Connections-MutedHues-Blended.png) | [Advanced](Captures/ConnectionsAdvanced-MutedHues-Blended.png) |
| **Muted Hues** | **Uniform (default)** | [Lab](Captures/Lab-MutedHues-Uniform.png) | [Small](Captures/Connections-MutedHues-Uniform.png) | [Advanced](Captures/ConnectionsAdvanced-MutedHues-Uniform.png) |
| Grayscale | Blended | [Lab](Captures/Lab-Grayscale-Blended.png) | [Small](Captures/Connections-Grayscale-Blended.png) | [Advanced](Captures/ConnectionsAdvanced-Grayscale-Blended.png) |
| Grayscale | Uniform | [Lab](Captures/Lab-Grayscale-Uniform.png) | [Small](Captures/Connections-Grayscale-Uniform.png) | [Advanced](Captures/ConnectionsAdvanced-Grayscale-Uniform.png) |

The lab includes nominal, slowed, stalled and endangered rows alongside both identity families. Grayscale accents remain readable against the existing dark network/pill background in these captures; final perceptual preference remains for human review. No layout change was made; earlier #139 1080p/720p layout evidence remains historical, while the six-way comparison here is specifically 720p.

## Checks actually run

Unity **6000.3.11f1**, isolated project copy; original open Editor/user TMP edit preserved:

- **2,609 color assertions pass:** all six combinations, every identity slot/family, nominal/slow/stalled activity and independent danger levels; exact old Blended RGB samples, unchanged IMPORTANT family and departure targets, exact Muted Hues, achromatic distinct grayscale.
- Focused regressions pass: **Business Importance 314**, **Display Lab 512**, **Visual Integration 81**, **recorder-policy 74**, **rerouting 108**, existing **RecorderWitness**. Visual Integration retains two unavailable diagnostic measurements in batch mode; these are not claimed as measurements.
- **666 actual Play Mode checks pass**, with **18 screenshots**: live selector switching on existing views/models; matching network/inspect/LOAD/WORK identities, including disabled/frozen/collapsed inspect history; unchanged WORK/time/route during comparisons; normal host progression after live switching; same lab view instances across modes.
- Project Core regenerated from source, `--check` and whitespace checks pass.

[Exact color assertions](deterministic-checks.txt), [actual Play Mode assertions](graphical-checks.txt), [Unity summary](validation-summary.txt). The existing render witness's asset-edit assertion now accounts for the selected performance treatment rather than assuming every rail uses the identity palette.

The prior #139 broad suite's three inherited failures (Resource inspect-header geometry/identity assertion, Advanced Overclock compact row, stale Generated Identity tuning) remain documented in the [parent review](../README.md). That broad suite was not rerun for this presentation-only refinement; its historical results are not relabeled as new passes. The inherited nonfatal Unity QuickSearch startup indexing exception was again observed during the successful graphical run. No human keyboard/IME or universal readability acceptance is claimed.

## Manual experiment

1. Enter normal generated Play Mode in either canonical scene, then select the shared **DisplayStyle.asset** in the Project tab. Set each ordinary mode with each performance treatment. Existing connections should change color immediately, retaining IDs, routes, WORK fill and movement.
2. Compare a nominal route, congested/slow route and endangered route. In Uniform Performance, matched danger/pulse states should have identical rails across ordinary/IMPORTANT and slots; pills remain their assigned identities. In Blended, individual identity hues participate as before.
3. Open `inspect <id>`, `load <node>` and `work <node>`; change the ordinary palette again. Keep an inspect card through resolution/collapse and repeat: history pills also follow the new palette. Returning to Muted Hues restores existing lifetime-slot colors.
4. Repeat in **ConnectionsDisplayLab**, including its progress rack. Restore **Muted Hues + Uniform** for the authored review baseline. Shared asset edits in Play Mode can persist in Unity; no scene rebuild/save is needed for this experiment.

The existing **Load Business Importance Comparison** fixture is optional if ordinary generation is inconvenient. The automated capture additionally creates a nominal ordinary/IMPORTANT AUTH pair, but this is temporary evidence setup rather than a new production scenario or generator.

Deferred INSPECT route emphasis and TAG investigative memory are recorded in the living design and contract only. Neither adds a command, visual layer or gameplay behavior here.
