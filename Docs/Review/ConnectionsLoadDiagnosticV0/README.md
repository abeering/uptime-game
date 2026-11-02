# Connections LOAD diagnostic V0 — PR #68 refinement evidence

This follow-up keeps `load <nodeId>` as an instant, live Connections terminal-history panel, now limited to connection requests. Its authored header contains only the node identity and `LOAD`. Rows show effective `DesiredLoad`; empty nodes say `NO ACTIVE CONNECTION REQUESTS`. The panel has no aggregate requested/capacity display or `OTHER` row. The normal network-view node bar still reports total requested LOAD, including infection and defensive demand. Both gameplay scenes now author zero baseline LOAD; the allocator's optional baseline mechanic remains supported.

Bar length compares visible requests using `max(3, largest request × 1.15)` rounded up in 0.5-LOAD steps. Growth is immediate. A 0.5-real-second grace period precedes an approximately 3-second contraction. Teal/amber/red color remains tied to absolute request size relative to the node's capacity, so a nearly full teal bar can still represent a small request. These scale and color settings are serialized on the authored panel.

## Scripted 16:9 Play Mode captures

**Bottom gutter follow-up:** LOAD now gives the final row 12 units inside the card and leaves another 12 units below the card, matching the inspect/scan composition. Its accent rail stretches with the card as rows appear and disappear. The earlier speculative width change was reverted. The focused witness checks empty, single, multiple, and shrinking row layouts; the captures below show the corrected spacing.

These 1920×1080 frames came from Unity 6000.3.11f1 Play Mode in `ConnectionsAdvanced.unity`. A temporary, ignored capture driver disabled automatic traffic, established explicit test connections, stepped the simulation, submitted normal terminal commands, refreshed panels, and snapped bar animations to their current targets for each still. It advanced the panel's UI clock for the recovery image. The images establish layout and values, **not interactive animation, scrolling, or keyboard-focus behavior**.

| Image | Evidence |
| --- | --- |
| [Quiet AUTH requests](load-quiet-small-play.png) | `q2` 2.2, `q3` 1.8, and `q1` 1.2 have readable relative teal bars; AUTH's network bar reports 5.2/10. |
| [DB, low SPIKING phase](load-db-low-play.png) | `s9` requests 4.0 alongside 8.0, 5.0, and 3.0; DB total is 20/10. |
| [Same DB panel, high SPIKING phase](load-db-high-play.png) | `s9` requests 18.0 and has a dominant red bar; DB total is 34/10. Row order stays fixed. |
| [Immediately after the spike](load-db-after-spike-play.png) | `s9` falls to 4.0 while the high scale is held briefly, making the drop obvious. |
| [Scale recovered](load-db-recovered-play.png) | The same low requests expand back to useful relative lengths after contraction. |
| [Empty panel with hidden node demand](load-empty-infected-node-play.png) | JOBS network bar reports 3/10 from Phantom LOAD while `load jobs` says `NO ACTIVE CONNECTION REQUESTS`; the panel does not reveal the infection. |
| [Several independent panels](load-multiple-panels-play.png) | The terminal retains a DB panel, the JOBS empty panel, and another DB panel in scrollable history. |

## Unity validation actually run

An isolated project copy under ignored `Temp/AdvancedSceneValidation` compiled the branch. `LoadOutputWitness` passed: both scenes have all baseline settings at zero; authored prefab bindings/material and removal of the aggregate header; valid/invalid commands; empty text; multiple panels; a 2.2-LOAD request filling over 70% of a teal bar; 18.0 requested versus lower allocated LOAD under contention; WAVING effective LOAD; SPIKING scale growth, graceful hold, and smooth contraction; stable order; infection exclusion while the normal node bar still displays the higher total; arrival, operation transition, kill, reset, and narrow layout. `ResourceWitness` also passed explicit nonzero baseline scenarios (DB baseline 2 and 10), so the underlying allocator behavior remains exercised.

The existing Connections regression batch passed: Worm, Payload, Geometry, Condition, Recon Event Lifecycle, Node Status, Scan, Infection, Traffic, Recorder, Network Health, Resource, Behavior, Lifecycle, Advanced Scene, and Inspect V2 witnesses. The bottom-gutter follow-up reran this batch and the focused LOAD witness, including geometry checks for empty, one-row, multi-row, and shrinking LOAD cards. Its refreshed scripted Play Mode captures show the new spacing. The Advanced scene witness's 600-second seeds still passed. Its plan/infection counts were 1701: 23 benign, 13 ordinary hostile, 5 Reconnaissance, 5 infections; 3141: 27, 4, 1, 3; 9001: 15, 12, 7, 9. The older baseline-one run produced 23/13/5/5, 26/4/1/4, and 18/12/7/6 respectively. The differing downstream counts are expected from changed available capacity; generation settings were not retuned.

`AmbientHostileActivityWitness` passed four unattended 600-second Advanced runs with no player intervention. Its prior hard-coded expectation that seeds 1702–1704 all fail was removed because that outcome depended on the old scene baseline. The witness still verifies that `Failed` is latched consistently with the first failed sample and that benign Health-impact samples do not exceed all-connection slow/overdue samples. All four selected-activity counts remained 22, 15, 20, and 21 opportunities, with no selection failures.

| Seed | Benign cancels | All slow/overdue sampled s | Benign Health-impact sampled s | Peak live connections | Final / minimum Health | Failed? | First failed sample s |
| ---: | ---: | ---: | ---: | ---: | ---: | :---: | ---: |
| 1701 | 14 | 335 | 31 | 14 | 94.48 / 89.91 | No | — |
| 1702 | 13 | 405 | 184 | 13 | 64.34 / 28.54 | No | — |
| 1703 | 8 | 518 | 518 | 9 | 0 / 0 | Yes | 292 |
| 1704 | 0 | 500 | 467 | 12 | 0 / 0 | Yes | 448 |

These are end-of-step samples, not exact cumulative slowdown durations. All runs continued to 600 simulated seconds after any latched failure. The [original PR #66 review](../ConnectionsAmbientHostileActivityV2/README.md) retains its baseline-one results as historical evidence: three seeds failed there, while two fail under the zero-baseline scenes. No Health, traffic, or threat tuning was changed for this follow-up.

The scripted capture completed; its batch Editor log emitted one nonfatal Unity Search indexing exception at startup, then produced all seven frames and reported `LOAD REFINEMENT PLAY CAPTURE PASS`. Interactive Play Mode review remains necessary.

## Manual review

Open `ConnectionsAdvanced.unity` at 16:9 and enter Play Mode. Use `load db` and `load auth` while traffic is active; compare small requests, watch a SPIKING or WAVING row over time, and check bar growth and later recovery. Issue two panels for the same node, scroll through history, and confirm command focus stays in the prompt. Create an infection on a node with no connection requests and compare its network bar with the panel's empty message. Repeat `load db` in `Connections.unity`.
