# Connections topology stress V0

**Scope:** opt-in Connections presentation experiment, October 24, 2026. Built on the head of geometry study PR #23 (`4b992c4`), while that PR was still open. This is not a topology progression system or a new Connections mechanics contract. `Connections.unity`, Packet, the node shape, the terminal split, identity packing, and routing geometry are unchanged.

## Method

`ConnectionsTopologyStress.unity` copies the authored workstation into a separate scene. At Play start, `TopologyStressHarness` generates a seeded connected tree with one to four perimeter external origins, internal services, branching, and exactly `nodes - 1` physical links. A separate depth-column/perimeter placement uses the existing 290 × 96 node cards at the serialized 1920 × 1080 reference size. It neither shrinks cards nor repairs bad placements. Generated nodes and links copy the scene's authored UI; logical paths, pills, markers, LOAD bars, `SimulationHost`, and `ConnectionTrafficDirector.QueueSpawnPlan` are production components. Traffic uses multiple origins and internal destinations, route searches, READ/WRITE/EXECUTE, and occasional two-operation arcs. Plans are benign, with 1–3 requested LOAD and 45–90 seconds of nominal WORK duration. Internal nodes have 10 LOAD capacity and 1 baseline LOAD.

The Inspector exposes node count (4, 8, 12, 16, 20, 24, 28), topology seed, traffic seed, and Light/Typical/Heavy density. Arrival intervals center on 12, 5, and 2.5 simulation seconds respectively, with ±20% jitter. The batch comparison used **Typical**, topology seeds **1701/3141/9001**, traffic seed **2401**, and 60 simulation seconds. It ran in Unity Play Mode with explicit one-second simulation steps, then advanced the existing path views for a settled 1920 × 1080 Editor capture. The 21 PNGs and [CSV](topology-stress-v0/measurements.csv) are in [topology-stress-v0/](topology-stress-v0/). Every capture contains active simulated connections. One-second stepping and a settled visual instant do not establish interactive pacing or exact 0.02-second Play Mode snapshots.

Diagnostics count card-rectangle overlaps and viewport overflow, sample physical centerlines against unrelated card rectangles, measure shortest card-edge-to-card-edge links, and query production identity packing for occupied links. Crossings are sampled diagnostics, not exhaustive collision proofs. `Unavailable` is the identity solver's result for a link with active users; below-minimum width is separate. The CSV also records origins, active connections, rotating/overlaid stages, first-phase visible IDs, and total active link uses. Three seeds are a small sample, not a distribution study.

## Result by tier

Ranges span the three seeds. The linked image is seed 1701; the directory also contains 3141 and 9001. Every snapshot has 11–12 active connections.

| Nodes | Classification | Diagnostic result and visible cause |
|---:|---|---|
| [4](topology-stress-v0/n04-s1701.png) | **Compromised but potentially usable** | No card overlap or unrelated-node centerline crossing. One occupied link is below the 2.5 px diagnostic width at 12 active connections; its dense amber bundle and rotating IDs are the issue. |
| [8](topology-stress-v0/n08-s1701.png) | **Compromised but potentially usable** | No card overlap or narrow link. One card overflows the viewport and one centerline passes an unrelated card in all seeds; one or two occupied links rotate IDs. Branches remain distinguishable in the static capture. |
| [12](topology-stress-v0/n12-s1701.png) | **Unreadable or geometrically unsuccessful** | No card overlap, but the shortest visible link is **7.7–7.8 px**; four occupied links return `Unavailable` for identity display in every seed. Two centerlines pass unrelated cards. Depth columns consume the space between full-size cards. |
| [16](topology-stress-v0/n16-s1701.png) | **Unreadable or geometrically unsuccessful** | One or two card overlaps, **4–6** unavailable occupied links, **6–8** centerline/card crossings, and a shortest link around **7.7 px**. IDs and card contents collide near branches. |
| [20](topology-stress-v0/n20-s1701.png) | **Unreadable or geometrically unsuccessful** | **16–18** card overlaps, **7–9** unavailable links, and **10–15** centerline/card crossings. Some links have under 7 px visible length. |
| [24](topology-stress-v0/n24-s1701.png) | **Unreadable or geometrically unsuccessful** | **22–33** card overlaps, **9–11** unavailable links, and **23–26** centerline/card crossings. Service headings and LOAD bars are obscured by neighboring cards. |
| [28](topology-stress-v0/n28-s1701.png) | **Unreadable or geometrically unsuccessful** | **39–45** card overlaps, **10–11** unavailable links, and **27–40** centerline/card crossings. Much of the right-side network is a stack of cards and paths. |

**First serious break: 12 nodes in this placement.** The graph remains connected and cards do not overlap, yet adjacent depth columns leave links too short to carry identities. This is evidence against this *placement strategy*, not proof that twelve nodes cannot be presented by another layout. At 16 and above, rectangular card packing itself fails in the available area. The 4-node narrow bundle and 8-node rotating IDs show that traffic concentration and identity packing can cause local problems before node count becomes large. PR #23's isolated-link and shared-border findings remain relevant; this study does not isolate the renderer's maximum capacity from the layout's failures.

## Next experiments, ranked

1. **Replace depth-column placement with a layout that reserves visible link length and avoids card overlap.** Keep current cards and viewport initially; test whether the 12-node failure moves.
2. **Measure shared-node attachment and label collisions on a better placed topology.** Current crossing/overflow counts and PR #23's local junction tests show that independent link success misses neighboring links.
3. **Compare identity readability under the same topology at Light/Typical/Heavy traffic.** The 4-node narrow bundle is driven by route concentration. Vary occupancy before changing packing or lane rules.

None is implemented in this slice. The experiment does not establish a final maximum node count.

## Validation actually run

- Unity 6000.3.11f1 compiled the isolated project and configured the separate scene. The final batch reported `TopologyStressBatch PASS`: 21 Play Mode configurations, 21 CSV rows, and 21 PNGs verified at 1920 × 1080. Representative captures at every tier were visually inspected.
- `TopologyStressWitness`, `GeometryWitness`, `ResourceWitness`, and `TrafficWitness` passed in the isolated Editor project. The graph witness checked all 21 count/seed pairs.
- `git diff --cached --check` and `python scripts/build_uptime_project_core.py --check` passed. Git shows no edits to `Connections.unity`, Packet scenes, or Connections prefabs.
- Interactive Play Mode with a human at the terminal was **not** run. Unity's Search indexer logged an `ArgumentOutOfRangeException` at batch startup; the stack was in `UnityEditor.Search`, and the requested batch and witnesses completed. This remains an Editor log caveat rather than a gameplay validation result.

## Reproduction and review

Open `Assets/Scenes/ConnectionsTopologyStress.unity` in Unity 6000.3.11f1. In the `Topology Stress Harness` Inspector, set a supported node count, topology seed, traffic seed, and density, then enter Play Mode at 16:9. Change fields and restart Play to regenerate without placing nodes. Wait through the short arrival grace; inspect generated IDs in compact and verbose modes, check input focus and scrolling, then watch shared branches, endpoint markers, LOAD bars, link labels, and identity cycling. Compare one seed across sizes and another seed at the same size. The batch PNGs are settled Editor Play Mode captures; they do not validate human reading speed or continuous animation.

For the automated snapshot pass, run `Uptime.Connections.EditorTools.TopologyStressBatch.Run` via `-executeMethod` in an isolated batch Editor project **without** `-quit`; the runner exits after 21 cases. `TopologyStressWitness.Run` checks graph connectivity, duplicate links, origins/services, and deterministic placement. The existing Geometry, Resource, and Traffic witnesses are relevant regressions. The original `Connections.unity` scene is not used as the generated test scene.
