# Connections — geometry capacity study V0

**Scope:** Connections presentation research, October 23, 2026. Baseline: current `main`, `522605d212695a4420ee9326694eac352d7975ad`. No production scene, prefab, runtime, simulation, allocation, spawning, or Packet changes. These measurements do not approve new mechanics or establish a node-count limit.

**Finding:** identity packing becomes compromised before lane width in the tested isolated links. At 120 px of visible link, four lanes already select rotating IDs; at 180 px, four IDs fit together horizontally/vertically but the 45° diagonal still rotates. Separately, nearby links can collide at a shared node even when both pass their independent budgets. Smaller node roots alone fail the existing content layout. **Test width-only node compaction next, retaining the 96 px height and existing font sizes.**

## 1. Current baseline

All dimensions below are reference UI pixels at native 1920×1080. The scene uses Scale With Screen Size, reference 1920×1080, width/height match 0.5; these are not invariant device pixels at smaller resolutions. Values come from the loaded scene and authored prefabs, not field defaults. [Measured data](geometry-capacity-v0/measurements.md) records the resolved layout.

| Item | Actual value / implication |
|---|---|
| Node cards, all four | **290 × 96**; area 27,840 px² |
| Content envelope | Heading and LOAD containers occupy a combined **250 × 78** bounding box, x ±125, y −43…35 relative to card center. This includes empty space, not just glyph ink. |
| Heading | 234 × 39 at left inset 28; horizontal layout, 8 px gap; ID **27 px**, role **14 px**. Preferred combined text widths: DB 124.42, API 165.82, AUTH 190.42, EXT1 **224.01**. |
| LOAD | 250 × 34, centered at y −26; label 234 × 15 at **13 px**; track **234 × 10**. Existing layout leaves 5 px below the LOAD container. |
| Eight attachment budgets | N **96**, NE **72**, E **72**, SE **72**, S **96**, SW **72**, W **72**, NW **72**, identically serialized on all nodes |
| Logical lines | Maximum **12**, minimum gap **6**, diagnostic minimum **2.5**. The C# default gap of 3 is **not** the scene setting. No maximum gap setting; the solver widens gaps for pills. |
| Identity pills | **54 × 28**, ID font **22**; pill gap **6**, end inset **12**, line clearance **1**; stagger step **20**, with a **4 px** fine search. Pills rotate with their link; vertical IDs are sideways. |
| Cycling | Enabled; authored automatic interval 2.6 s, minimum dwell 1.2 s; actual dwell = max(1.2, 2.6 / √(total / visible)) |
| Physical links | 2 px UI lines, drawn center-to-center behind cards; their labels are **116 × 24**, font 11.5, offset 100 on E1 and 80 on E2/E3 |
| Target markers | Beam/angle-derived border span, plus prefab padding **8 × 8**; no separate marker collision solver |
| Screen split | Terminal rectangle **648.8 × 1080** (33.79% width), nominal anchor split **34%**; 4 px divider. Network panel **1267.2 × 1080** (66%). |
| Usable authored network area | `PhysicalTopology`: **1203.2 × 911**, after 64 px horizontal and 169 px vertical deductions from the network panel; header and margins already excluded. This is a layout rectangle, not a guarantee every rendered object stays inside it. |

| Physical link (A→B) | Heading | Center distance | Visible boundary-to-boundary center lane | Bundle budget |
|---|---:|---:|---:|---:|
| E1, EXT1→API | −90° | 255.08 | **159.08** | 96 |
| E2, API→DB | −136.81° | 412.60 | **272.34** | 72 |
| E3, API→AUTH | −43.19° | 412.60 | **272.34** | 72 |

**Card size and bundle budget are different quantities.** `NodeView` rounds a center-to-center heading to one of eight 45° sectors. `PhysicalLinkView` takes the smaller endpoint budget. Neither derives its budget from the rectangle, divides a shared side between links, nor reserves attachment space. A 45° ray from a 290 × 96 rectangle meets its **top/bottom**, not its right/left edge; a 72 px perpendicular bundle spans about **101.82 px** along that horizontal border. North's 96 px budget is therefore not an independent physical port beside NE's 72 px budget. The distinction matters especially near the ±22.5° sector boundaries, which can change a budget without a continuous change to the card boundary.

Enforced geometry: maximum width, minimum gap, card intersections/2 px underlap, pill bounds/insets, pill–pill/node/own-link-label clearance, and (in stages A/B) clearance from other lanes **on the same link**. The solver uses covered endpoints, slightly longer than visible link lengths. The **2.5 px minimum is diagnostic**, not a clamp: fallback can draw thinner or zero-width paths. Budget fit can also fail after fixed gaps consume the whole budget. No enforced rule checks node-content fit, other links' pills/lanes/labels, other nodes, shared attachment regions, marker collisions, or viewport containment. “Readable” and “comfortable” below are review classifications, not solver guarantees.

## 2. Bounded comparisons

The [Editor-only study](../../Assets/Editor/GeometryCapacityStudy.cs) calls the existing `EdgeGeometry`, `IdentityPacking`, production `IdentityPresenter`, and marker geometry. It follows `GeometryPreview`'s disposable path-rendering pattern; its single-edge update/cleanup loop is not used to render three links. `GeometryWitness` remains the production regression check, including route-aware ordering. Source context: [route-aware lanes](../Connections/route-aware-lanes-v1.md), [identity presentation](../Connections/identity-presentation-v1.md), [markers](../Connections/terminal-markers-v1.md), and the separate, still-parked [spacing research](connections-lane-spacing-continuity.md). Shared [topology](../Design/toplogy-system-v1.md), [roles](../Design/topology-nodes.md), and [grammar](../Design/topology-grammar.md) describe broader proposals, not assumptions of this experiment.

**108 cases**, plus one deliberately unavailable control: baseline H/V/45° × four lengths × five densities (60); 80% and 65% roots at 180 px, all three orientations/densities with unchanged budgets (30); proportional-budget sensitivity at 180 px and 4/8/12 lanes (18). This isolates footprint from budget instead of sweeping every combination. Synthetic lengths are measured between **center-lane card boundaries**; individual diagonal lanes have different visible lengths. Node cards and their own 116 × 24 label obstacle remain in each solve; labels retain 80 px offset (100 vertical). Other topology obstacles are deliberately absent. Complete widths, gaps, phases, coverage and diagnostics: [CSV](geometry-capacity-v0/link-cases.csv).

### Identity display at the baseline footprint

Cells are **solver stage + simultaneously visible pill count**; ranges cover all phases. A = Centered; B = Staggered; C = Overlaid (pills may hide neighboring beams); D = Rotating. In this sample A/B are **geometrically comfortable candidates**, C/D **compromised**; all need human review, especially sideways IDs. D reports the selected fallback, not proof that rotation is mathematically necessary: this bounded heuristic is not an optimal packing solver.

| Orientation | Visible px | 2 lanes | 4 | 6 | 8 | 12 |
|---|---:|---|---|---|---|---|
| Horizontal | 120 | A2 | D2 | D2 | D2 | D2† |
| Horizontal | 180 | A2 | B4 | D4 | D4 | D4† |
| Horizontal | 240 | A2 | B4 | C6 | D6 | D6† |
| Horizontal | 320 | A2 | B4 | C6 | C8 | D10† |
| Vertical | 120 | A2 | D2 | D3 | D2–3 | D3 |
| Vertical | 180 | A2 | B4 | D6* | D5 | D5–6 |
| Vertical | 240 | A2 | B4 | B6 | D6–7 | D8 |
| Vertical | 320 | A2 | B4 | B6 | C8 | C12 |
| 45° diagonal | 120 | A2 | D2 | D2 | D2 | D2† |
| 45° diagonal | 180 | A2 | D3 | D4 | D4 | D4† |
| 45° diagonal | 240 | A2 | B4 | C6 | D5 | D6† |
| 45° diagonal | 320 | A2 | B4 | C6 | C8 | D8–9† |

† 0.5 px lines, below the diagnostic minimum: identities still pack, but this is **unavailable as a readable lane presentation**. This differs from the solver's `Unavailable` enum. *V/180/6 returns two rotating phases that each contain all six IDs; the later rotation packer finds a fuller arrangement than the earlier attempt. Treat the returned stage as implementation evidence, not an inherent capacity limit. All 108 cases eventually schedule every ID, including cases with zero-width lanes; identity success alone is insufficient.

Width/gap pairs below explain the stages (px, rounded). The fallback width is independent of length; A/B use pill-aware gaps instead.

| Lanes | H / diagonal fallback | Vertical fallback | Successful A/B layouts |
|---|---|---|---|
| 2 | 12 / 6 | 12 / 6 | A: **12 / 22**, all orientations |
| 4 | 12 / 6 | 12 / 6 | B: H/diagonal **10.2 / 10.4**; vertical **12 / 9.5** |
| 6 | 7 / 6 | 11 / 6 | Vertical B at 240/320: **5.29 / 12.86** |
| 8 | 3.75 / 6 | 6.75 / 6 | None tested |
| 12 | 0.5 / 6 | 2.5 / 6 | None tested |

Thus spacing observed in the sweep is 6–22 px; this is not a configured maximum. Lane-width capacity alone, from `(budget + gap) / (minimum width + gap)`, is **9** on 72 px and **12** on 96 px at ≥2.5 px/6 px. At full 12 px width it is 4 and 5 respectively. Those are derived isolated-link ceilings, not comfortable identity or topology capacities. The **70 px horizontal, two-lane control** returns solver `Unavailable`: no full-size pill fits with the authored insets/cards.

### Compact roots and attachment budgets

| Exploratory size | Root area saving | Existing node information | 180 px link results with original budgets |
|---|---:|---|---|
| 290 × 96 | — | All measured containers inside card | Baseline table |
| **232 × 76.8** (80%) | 36% | Heading, LOAD container and track each exceed card bounds on every node | Same stages/widths/visible counts as baseline; shrinking cards alone does not improve a link whose **visible length is held fixed** |
| **188.5 × 62.4** (65%) | 57.75% | Same three failures; AUTH/EXT1 preferred heading text already exceeds the entire card width | Similar stages/counts; horizontal bundles at 4/6/8/12 have uncovered endpoints because the retained bundle exceeds the 62.4 px card height; diagonal rotation phase counts also increase |

Scaling the **entire content subtree** would avoid some overflow by reducing the ID/role/LOAD fonts to 21.6/11.2/10.4 px at 80%, or 17.55/9.1/8.45 at 65%; tracks become 8 or 6.5 px tall. These are arithmetic projections, **not** validated readable alternatives. A useful compact card needs explicit content layout work.

Proportional budgets are an exploratory sensitivity assumption, not a proposed default:

| Scale / orientation | Budget | 4 lanes width/gap; stage | 8 lanes width; stage | 12 lanes |
|---|---:|---|---|---|
| 80% H | 57.6 | 4.44/13.28; B4 | **1.95**; D2–4 | **0 width**, fixed gaps exceed budget |
| 80% V | 76.8 | 12/9.5; B4 | 4.35; D4 | **0.9**; D4–5 |
| 80% diagonal | 57.6 | 9.9/6; D2 | **1.95**; D2–4 | **0 width**, fixed gaps exceed budget |
| 65% H | 46.8 | 7.2/6; D3 | **0.6**; D2–3 | **0 width**, fixed gaps exceed budget |
| 65% V | 62.4 | 6.36/12.32; B4 | 2.55; D4 | **0 width**, fixed gaps exceed budget |
| 65% diagonal | 46.8 | 7.2/6; D3 | **0.6**; D2–4 | **0 width**, fixed gaps exceed budget |

Eight-lane gaps in these rows are 6 px. Bold sub-minimum/zero widths are compromised/unavailable even though pills rotate successfully. Retaining all budgets is unsafe on small horizontal sides; scaling all budgets trades that problem for earlier lane failure. Neither is automatically the right attachment policy.

### Multi-link junction

Twelve local tests use two independently solved links approaching one card at **45°/90°** (different compass budgets, same top border) or **0°/15°** (nearby east region), four/eight lanes each. The test compares actual strips in the first 40 visible pixels and production marker rectangles with 8 px padding. It does not create nodes or a gameplay topology.

At the **current** card size with four lanes per link, 45°/90° has **3 intersecting cross-link strip pairs and 4 marker pairs**; 0°/15° has **2 strip pairs and 4 marker pairs**, despite both layouts individually returning B at 0°/15°. At eight lanes these become 9/9 and 7/12. Compact-card cases also collide; full counts are in the [measurements](geometry-capacity-v0/measurements.md). Markers are relevant only when those lanes terminate at the shared node; strip competition also applies to transit. These diagnostics establish local interference, not perceived severity or a global collision-free layout.

### Visual references

Four native 16:9 **Editor renders**, inspected as part of this study:

- [Current footprint, four lanes per link](geometry-capacity-v0/baseline-4.png): B on E1/E2/E3; 12 scheduled and rendered pills across three independent link previews.
- [80% roots, unchanged content](geometry-capacity-v0/compact-80-4.png): same 12 pills; obvious LOAD/heading spill. This candidate is rejected as a direct root resize.
- [Promising modest width-only candidate, 260 × 96](geometry-capacity-v0/width-only-260-4.png): saves **10.34% node width/area**, retains font sizes and all 12 pills. The 250 px LOAD container leaves only 5 px side margins; the 234 px heading rectangle extends 2 px beyond the right card edge, though the measured glyph widths fit. Human review/content-inset adjustment is required; this is not a ready default.
- [Crowded current footprint, twelve lanes per link](geometry-capacity-v0/crowded-12.png): rotating IDs, **20/36 pills** rendered in the initial phases (6/7/7); diagonal beams are 0.5 px. The density is visibly compromised.

The images retain authored node positions, terminal, static headings and LOAD values. `NO ACTIVE CONNECTIONS` is the authored caption: preview beams are not simulated traffic. IDs/colors are illustrative per-link assignments, not route-consistent connections or a palette validation. No operation pulses, target markers, lifecycle animation, user commands or Play Mode are captured. The compact-root image increases actual visible distances by retaining node centers; numerical compact comparisons instead hold visible distance fixed. These are intentionally different comparisons.

## 3. First limiting factor and topology spacing

| Capacity | First meaningful constraint shown |
|---|---|
| Connection lanes | Four full-width fallback lanes fit either budget; eight remain ≥2.5 px at original budgets. Twelve on 72 px fail the width diagnostic regardless of length. |
| Identities | Four IDs at 120 px already rotate while beams remain 12 px. Six at 240 px on 72 px budgets fit together only by overlaying beams. This is the first isolated-link compromise in the sampled density sequence. |
| Attachments | Close approaches can overlap at only four lanes/link, independently of pill fit. Eight direction budgets are not eight independent rectangular ports. |
| Node information | Both 80%/65% root reductions break the current fixed child layout immediately. Font shrinking is not validated as an acceptable substitute. |
| Topology spacing | Node width plus useful annotation length consumes viewport space. A short link that holds two IDs is not an adequate general-purpose four/eight-lane link. |

For a simple axis-aligned adjacent pair, required center pitch is node width + visible length horizontally, or height + length vertically. At current size, 180 px visible implies **470 px H / 276 px V**; at 80%, **412 / 256.8**; at 65%, **368.5 / 242.4**. The 45° boundary distance differs: at baseline the two cards consume **135.76 px** along the center ray before any visible link is added; orientation therefore matters beyond compass budgets.

As a deliberately narrow spacing illustration, a **4 × 4 rectangular lattice** with 120 px gaps needs **1520 × 744 px** at baseline, **1288 × 667.2** at 80%, or **1114 × 609.6** at 65%, against 1203.2 × 911 available. Only the smallest footprint fits those bare bounds, and it fails node-content checks while 120 px links rotate four IDs. At 180 px gaps even its bare width is **1294 px**. These are arithmetic bounding boxes with no crossing, junction, annotation, margin or usability allowance. They neither rule out other arrangements nor establish that any complete sixteen-node network works. There is **no measured full-topology node limit**.

## 4. Highest-value opportunities (not implemented)

1. **Reclaim node width while retaining height and typography.** The 260 × 96 render is a modest starting point; any deeper reduction needs a more efficient heading/LOAD layout, especially EXT1's long role. Avoid coupling width savings to shorter side-attachment space through uniform scaling.
2. **Account for shared border regions when placing/budgeting neighboring links.** The four-lane junction collisions are a stronger reason than isolated solver success to review arrival angles and shared corners. This need not imply an octagonal renderer or identity-packing refactor.
3. **Consider a narrower terminal only after reviewing inspect content.** Moving the anchor split from 34% to 30% would return **76.8 px**, or **6.38%** of current topology width. This is an untested space tradeoff: structured inspect cards, scrolling and command entry could become worse.

## 5. What remains unknown

Measured: the serialized baseline, fixed-size content bounds/text widths, 108 solver cases plus unavailable control, local junction intersections, and four static production-component renders. The solver's heuristic stages are evidence about this implementation, not optimal packing proofs. A mathematically successful 2.5 px lane or overlaid pill does not establish comfortable reading.

Unvalidated: human reading speed, smaller/Retina Game Views, sustained rotating-ID tracking, live marker crowding, establishment/reflow/departure with route-aware ordering, cross-link pill/label obstacles, larger node names/roles or LOAD strings, dense graph crossings, and full sixteen-or-more-node feasibility. No gameplay balance, frame-time or performance capacity claim is made. No full-topology placement was attempted.

## 6. One recommended next experiment

In a disposable Editor preview, test **width-only compaction starting at 260 × 96**, with the existing font sizes, height and attachment budgets. Adjust only heading/LOAD insets to fit inside that card; compare it against 290 × 96 on the existing four-node positions at four and six lanes/link. Accept only if all content bounds fit, EXT1/LOAD remain easy to read at native 16:9, and markers/identities stay clear. Reject or revise this one hypothesis before attempting a larger topology. Do not change the production default as part of this study.

## Reproduction and validation

Run in a disposable project copy of `Assets`, `Packages`, `ProjectSettings` (a copy-on-write `Library` copy can shorten import). The method deliberately refuses interactive Editor execution so it cannot replace an open working scene. It changes only in-memory objects, writes this study's data/images, and verifies SHA-256 equality for the Connections prefabs and all three playable scene files. Example from the copied project:

```sh
/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /private/tmp/uptime-geometry-capacity-study \
  -executeMethod Uptime.Connections.EditorTools.GeometryCapacityStudy.Run \
  -quit -logFile /private/tmp/uptime-geometry-capacity-study.log
```

Run the existing witness similarly with `-executeMethod Uptime.Connections.EditorTools.GeometryWitness.Run`. Do not use `-nographics` for the capture run. Outputs are relative to the project root under `Docs/research/geometry-capacity-v0/`.

| Check actually run | Expected | Observed |
|---|---|---|
| Unity 6000.3.11f1 batch Editor, isolated copy | Compile without errors | PASS |
| Study solver cases | Record all requested dimensions/orientations/densities, reject intersecting scheduled pills | 108 cases + one control; pill-pair separation assertions pass; every ID covered in the 108 cases |
| Deliberate short control | No full pill fits | `Unavailable`, zero scheduled pills |
| Four rendered comparisons | Native 1920 × 1080, actual planned pills shown | 12 / 12 / 12 / 20 total pills; captures reviewed |
| Existing Geometry Witness | Existing budgets, card coverage, packing, route ordering, identity/color/lifecycle and target-marker checks | PASS |
| Protected scene/prefab byte hashes | Unchanged after study | PASS |
| CSV/artifact audit and baseline comparison | Expected case counts/diagnostics; untouched production | 109 rows, all four PNGs 1920 × 1080; five uncovered-end and five saturated-gap cases recorded; 234 production scene/prefab/script files match baseline bytes |
| `git diff --check`; Project Core `--check` | No whitespace errors; no stale Core | PASS |

Interactive Unity review remains outstanding. Open production `Connections.unity` at 16:9, check typical four–six live connections, node LOAD/markers and crowded identity labels, run `inspect <id>` / `inspect -v <id>`, and confirm scrolling and command focus. Compare the static candidates from the linked PNGs; never save study state into the production scene. The unchanged scene is the review baseline, not a compact implementation to approve.
