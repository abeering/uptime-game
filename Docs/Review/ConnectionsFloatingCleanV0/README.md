# Floating CLEAN Window V0 review

Connections implementation, based on `main` at `1c90aa1` (merged PR #70). PID-Select now nests actual instances of `DockedTitlebar.prefab` and `DockedWindowFrame.prefab`. Both shared source prefabs, both scene files, and `Terminal.cs` are byte-identical to the baseline. CLEAN retains its centered 710×465 reference rectangle in NetworkPanel.

The titlebar is 40 units high, with the shared bottom rule and dynamic `PROCESS KILLER / <node>` title. A CLEAN-only, right-aligned, noninteractive `[ESC] CANCEL` label replaces its hidden pinned group. The label appears on opening and stays through selection, then hides during the existing noncancelable result display. The bespoke title/titlebar, external Outline, and accent rail are removed; the input border is retained. Body text starts 66 units below the top rather than 82, recovering the height gained from the smaller titlebar while preserving its font, palette, horizontal padding, and spacing above the input. `ENTER PID TO TERMINATE` remains; the duplicate body Escape instruction is removed.

The implementation reads the working workspace design in [docs-only PR #67](https://github.com/abeering/uptime-game/pull/67), which was open at inspection. This PR adds no window manager, Escape stack, global focus manager, pause/menu, notifications, or communication surfaces. Existing local Escape handling and session logic remain authoritative.

## Visual evidence

These are scripted Unity 6000.3.11f1 **Play Mode** captures of the authored scene Canvas rendered through a temporary camera. The actual `infect DB phantom-load` and `clean DB` command listeners open real sessions; native simulation stepping continues. Ambient generation is disabled in these controlled checks. They show the CLEAN window together with both permanent panes, including their original filled pinned indicators. They are not screenshots from a human-operated physical-keyboard playtest.

| Scene | Phase | 1920×1080 | 1280×720 |
| --- | --- | --- | --- |
| Connections | SSH splash | [Capture](Connections-splash-1920x1080.png) | [Capture](Connections-splash-1280x720.png) |
| Connections | PID selection | [Capture](Connections-selection-1920x1080.png) | [Capture](Connections-selection-1280x720.png) |
| Connections | Success result | [Capture](Connections-success-1920x1080.png) | [Capture](Connections-success-1280x720.png) |
| Connections | Failure result | [Capture](Connections-failure-1920x1080.png) | [Capture](Connections-failure-1280x720.png) |
| ConnectionsAdvanced | SSH splash | [Capture](ConnectionsAdvanced-splash-1920x1080.png) | [Capture](ConnectionsAdvanced-splash-1280x720.png) |
| ConnectionsAdvanced | PID selection | [Capture](ConnectionsAdvanced-selection-1920x1080.png) | [Capture](ConnectionsAdvanced-selection-1280x720.png) |
| ConnectionsAdvanced | Success result | [Capture](ConnectionsAdvanced-success-1920x1080.png) | [Capture](ConnectionsAdvanced-success-1280x720.png) |
| ConnectionsAdvanced | Failure result | [Capture](ConnectionsAdvanced-failure-1920x1080.png) | [Capture](ConnectionsAdvanced-failure-1280x720.png) |

Fractional Canvas scaling at 1577×887: [Connections selection](Connections-selection-1577x887.png), [Advanced selection](ConnectionsAdvanced-selection-1577x887.png). All four phases were also rendered at that size in each scene. Raw pixel checks across every edge of all 24 captures found continuous frame-colored pixels on all four CLEAN edges, including fractional scaling. The 18 selected images above are retained; [frame check excerpts](frame-pixels.txt) record the complete matrix. This checks raster visibility as well as the authored 2-unit frame thickness.

## Checks actually run

Unity 6000.3.11f1 compiled an isolated copy of the branch. These witnesses passed:

- **CLEAN Window Witness:** both scenes; nested shared prefab provenance, 710×465 centered scene placement, 40-unit titlebar/rule, title and cancel-label bindings, inactive CLEAN pin, noninteractive adornment, four 2-unit frame edges, removal of bespoke chrome, retained body/input styling and timing, and unchanged permanent filled pinned indicators. Available via `Tools/Connections/Run CLEAN Window Witness`.
- **Infection Witness:** existing infection and CLEAN model coverage, including healthy and infected sessions, five processes, invalid and innocent PID outcomes, exact five-Health penalty, captured-instance removal, later arrivals, penalty-free cancellation, LOAD refresh, Spawner independence, recorder provenance/JSON, and seeded replay. Its old Health focus reference was removed because PR #70 already removed that coupling; its obsolete bespoke-chrome checks now verify the actual shared instances.
- **Network Health Witness:** both compact layouts, authoritative presentation, deterministic contention/drain, recovery, penalties, operation transitions, failure, and reset.
- **Advanced Scene Witness:** authored bindings/geometry and the CLEAN presentation checks, three deterministic traffic runs, Blackout cancellation, and Spawner routing.

Temporary **scripted Play Mode** checks passed separately in both scenes, using actual command/PID submission listeners and Input System virtual Escape keypresses across real frames. Checks confirmed:

1. SSH opens with the dynamic title and Escape hint without disclosing infection status.
2. Escape during SSH and selection closes the session without a Health penalty or removing infection.
3. Five-process selection receives PID typing focus; the body keeps only the Enter instruction.
4. Invalid PID input preserves the session, hint, Health, and usable input focus.
5. Correct selection removes the captured infection, hides input and Escape hint, ignores Escape during results, and retains the 1.3-second result hold.
6. An innocent selection retains infection and immediately costs exactly five Health; its result also hides and ignores Escape.
7. Success, failure, and cancellation all restore actual TMP terminal focus/EventSystem selection and retain their terminal feedback.
8. Simulation time advances while CLEAN is open; NetworkPanel geometry and every node's local position remain fixed.

[Validation excerpts](validation.txt) retain the witness and scripted check outputs. Temporary authoring, capture, Play Mode, and pixel-inspection scripts remain outside the production repository. `python3 scripts/build_uptime_project_core.py --check` and `git diff --check` passed. Packet was not modified or playtested.

The isolated Play Mode launches logged a Unity Editor `SearchDatabase.EnumerateAll` startup `ArgumentOutOfRangeException`, as in the prior compact Health review. Gameplay checks continued and passed. Physical-keyboard delivery, interactive Game view resizing, and human judgment under ordinary generated traffic remain manual checks.

## Manual Unity Play Mode review

1. Open each Connections scene in Unity 6000.3.11f1 at 1920×1080 and 1280×720, then try a fractional Game view size such as 1577×887. Keep ordinary generated traffic running. Confirm both permanent panes retain their original titlebars, filled pins, borders, and topology layout.
2. Enter `infect DB phantom-load`, then `clean DB`. Confirm the floating title, complete perimeter, shared titlebar/rule, and `[ESC] CANCEL`. Cancel with physical Escape during the SSH splash. Reopen and cancel during PID selection; confirm typing returns to the command prompt and cancellation is free.
3. Reopen, submit an invalid PID, and confirm input remains usable. Select the conspicuous high-CPU PID; confirm success removes the captured infection and restores command focus after the existing result hold. The Escape label must be absent throughout results, and physical Escape must not cancel them.
4. Reinfect and reopen, then select an innocent process. Confirm the existing five-Health penalty, retained infection, hidden result-phase Escape label, normal hold/feedback, and restored command focus.
5. In every phase check text and input fit, all frame edges at fractional scaling, readable titles, normal terminal history, live traffic/WORK, and fixed topology geometry. CLEAN may obscure the network within its existing centered footprint; it must not move or resize the network to make room.
