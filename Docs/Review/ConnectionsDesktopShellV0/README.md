# Connections Desktop Shell V0 review

These are static Unity 6000.3.11f1 Editor renders of the two playable Connections scenes. The before images use `main` at `b861a25` (PR #68 merged); the previous-shell images use `6c33314`; the after images include the independent window frames. The temporary capture setup rendered the authored Canvas through a camera at the named resolution. They are presentation previews, not interactive Play Mode evidence; live traffic, logical paths, terminal history, and keyboard focus need the checks below.

| Scene | Before, 1920×1080 | After, 1920×1080 | After, 1280×720 | Expanded Health | PID-Select overlay |
| --- | --- | --- | --- | --- | --- |
| Connections | [Before](before-connections-1920x1080.png) | [After](after-connections-normal-1920x1080.png) | [Smaller](after-connections-normal-1280x720.png) | [Expanded](after-connections-health-expanded-1920x1080.png) | [Overlay](after-connections-pid-select-1920x1080.png) |
| ConnectionsAdvanced | [Before](before-advanced-1920x1080.png) | [After](after-advanced-normal-1920x1080.png) | [Smaller](after-advanced-normal-1280x720.png) | [Expanded](after-advanced-health-expanded-1920x1080.png) | [Overlay](after-advanced-pid-select-1920x1080.png) |

For a direct comparison with the initial shell, see [previous Connections](previous-shell-connections-1920x1080.png) and [previous ConnectionsAdvanced](previous-shell-advanced-1920x1080.png).

## Visual comparison

- The clock/Health strip is flush with the full display width and top edge. The two permanent windows retain the 34/66 split, with 8 px desktop gutters around and between them at the reference resolution. Its expanding Health tray remains over the terminal column, leaving the topology in place.
- Each pane has a shared authored perimeter-frame prefab with four thin borders. The shared 40 px titlebar has a more distinct background, a bottom rule, and a small passive pushpin formed from noninteractive Images.
- Both explanatory subtitle rows remain removed. Inner margins absorb the surrounding gutters: terminal history, prompt, and topology retain exactly the same screen bounds as the initial shell. At 1920×1080 the history begins at screen y=106 rather than the original y=184, preserving the 78 px recovery.
- The topology starts 108 px below the full-screen top instead of 137 px, and its usable height grows by about 29 px at 1920×1080. Advanced node anchors, cards, and logical-path authoring were not edited.
- The expanded detail tray now reserves at least 148 px below the compact strip so its existing heading, trend, service, causes, and recent-event rows remain visible at smaller 16:9 sizes.

## Manual Play Mode review

1. Open each scene at 1920×1080 and 1280×720. Confirm the strip, titles, prompt, node cards, physical links, logical paths, status bands, and identity labels remain legible under generated traffic.
2. Type a partial command, place the caret in its middle, press Tab twice, and confirm the command text, caret, and input focus survive. Scroll back in a long `inspect` history, press Tab twice, and confirm the visible history position stays put. Return to the bottom and confirm new output follows it.
3. Run `inspect` on several simultaneous IDs, including compact and verbose output. Confirm structured cards and command results do not clip the narrower terminal.
4. In the Editor, use `infect DB phantom-load` followed by `clean DB` to open PID-Select. Confirm it overlays the network, receives typing, and returns focus to the terminal after dismissal. Inspect crowded labels and paths around the overlay in the advanced scene.

Unity 6000.3.11f1 compiled the isolated project and passed Health, Inspect V2, Load Output, and Advanced Scene witnesses after the frame changes. Temporary shell checks covered both scenes' gutters, full-width strip, four-edge frame prefab instances, titlebar rules, passive pushpins, and absence of interactive chrome. A serialized rectangle comparison against `6c33314` confirmed identical terminal history, prompt, and topology bounds in both scenes. Interactive Play Mode behavior and Packet scenes were not tested by these captures.
