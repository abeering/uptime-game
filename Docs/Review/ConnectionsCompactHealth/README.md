# Permanent compact Network Health review

Connections implementation only, based on `main` at `1a52c8b` (merged PR #69), October 31, 2026. The full-width 52-unit status strip retains its clock, authoritative Health value, animated fill, signed rate, and existing colors. `ExpandedDetails` and all five presentation children are removed. The panel no longer reads Tab, restores focus, calculates tray-only labels, or writes pane geometry. `Terminal.SetTopInset` had only the Health caller and is removed; history layout, scrolling, bottom-follow, and prompt-focus code are retained. Both scenes remove only the three obsolete Health overrides for terminal/input/clean-overlay references; their authored rectangles are unchanged. The earlier [Desktop Shell V0 review](../ConnectionsDesktopShellV0/README.md) preserves historical expanded-tray evidence.

## Captures

These are scripted Unity 6000.3.11f1 **Play Mode** renders of each authored scene Canvas through a temporary camera at the stated resolution. They show three real established, contending connections, compact and verbose inspection output, a selected partial command after scripted Tab checks, and authoritative Health drain. They are controlled scenarios with ambient generation and automatic stepping disabled for the check, not interactive player-session screenshots. No temporary review code or camera is committed to the game.

| Scene | 1920×1080 | 1280×720 |
| --- | --- | --- |
| Connections | [Capture](Connections-play-1920x1080.png) | [Capture](Connections-play-1280x720.png) |
| ConnectionsAdvanced | [Capture](ConnectionsAdvanced-play-1920x1080.png) | [Capture](ConnectionsAdvanced-play-1280x720.png) |

The strip stays full-width and both pane tops remain at their authored 60-unit inset, leaving the existing 8-unit gutter. Clock, Health, bar, signed rate, prompt, and frames remain legible at both sizes. The scene-specific topology and 34/66 pane split are retained.

## Checks actually run

Unity 6000.3.11f1 compiled an isolated project copy and passed:

- **Network Health Witness:** both scenes' prefab bindings and permanent compact geometry; Health render/re-enable leaves pane rectangles, history browsing/bottom position, draft, selection, and existing focus/selection state unchanged. Authoritative value, signed rate, rate color, and visible bar fraction are checked against the simulation. Existing bar spacing, animation convergence, repeated loss, critical priority, configurable threshold, frame subdivision, and reset checks pass.
- **Inspect V2 Witness:** commands and aliases, multi-ID output, compact/verbose cards, responsive timeline, LOAD display, canceled outcome, and history.
- **Resolved Inspect Witness:** four outcomes, timed collapse, scroll anchoring, bottom-follow, and input preservation.
- **LOAD Output Witness:** both scenes, command output, contention, dynamic requests, responsive layout, multiple panels, and reset.
- **Advanced Scene Witness:** authored bindings/geometry, three deterministic traffic runs, Blackout cancellation, and Spawner routing.

The Health witness retains its deterministic expected-versus-observed cases:

| Case | Expected | Observed |
| --- | --- | --- |
| Three 10-LOAD demands on DB | 3.333 LOAD each, −0.50 Health/s | Matched |
| Independent AUTH demand | 5 LOAD | Matched |
| First and next overdue steps | −1.53/s, then −1.59/s | Matched |
| Legitimate kill / recovery after release | −10 once / +1.75 over five seconds | Matched |
| Ordinary completion / reset / failure | No completion damage / 100 / zero latches failure | Matched |

Additional temporary **scripted Play Mode** checks passed separately in both scenes. An Input System virtual keyboard drove Tab and Page Up across real frames; a TMP key event also exercised its separate native-event processing path. There was no per-Tab focus restoration in the test. Assertions checked actual TMP focus and EventSystem selection, draft and selection preservation, browsed/bottom history positions, compact height and fixed pane offsets, Page Up scrolling, output bottom-follow, retained browsing scroll offset on append, authoritative numeric Health/rate, orange sustained drain, red critical recovery, and teal settled noncritical recovery. These checks do not establish OS delivery of a physical keyboard event.

Four additional static Editor renders were inspected at the same scene/resolution combinations. A serialized comparison confirmed identical authored geometry in both scenes and no changes to surviving prefab objects beyond removing the tray child reference and obsolete script fields. All prefab-local references resolve. `python3 scripts/build_uptime_project_core.py --check` and `git diff --check` passed. Packet was not modified or playtested. Unity also reports the existing unrelated `InfectionWitness.cs` CS0252 warning. The refreshed Play capture runs logged a Unity Editor `SearchDatabase.EnumerateAll` startup `ArgumentOutOfRangeException` in the isolated copy; all gameplay assertions still ran and passed. This is recorded as an Editor indexing caveat, with no change to indexing or gameplay code.

## Manual interactive Play Mode review still required

1. In Unity 6000.3.11f1, open each scene at 1920×1080 and 1280×720. Enter Play Mode with ordinary generated traffic. Check the strip, pane borders, prompt, node cards, physical links, logical paths, and crowded labels while resizing between those sizes.
2. Type a partial command and put the caret or selection in its middle. Press physical Tab repeatedly. Confirm Health stays compact, neither pane moves, and command text, selection/caret, and focus survive. Type another character and submit a valid command.
3. Inspect multiple active IDs with `inspect <ids>` and `inspect -v <ids>`. Browse a long history using Page Up/Down, Ctrl/Cmd + arrows, and the mouse wheel. Press Tab while browsing. Confirm the visible position remains stable and that returning to the bottom resumes following new output. Confirm command recall and prompt focus still work.
4. Run `demandtest` or ordinary contention. Confirm immediate numeric Health and signed drain, easing orange fill, and recovery after congestion clears. Legitimate kills cost Health; hostile kills do not. With Health below 25, verify red presentation during both drain and recovery; at or above 25, settled recovery returns to teal.

Physical-keyboard and human visual judgment under an interactive player session were not performed by this automated review.
