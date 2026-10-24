# Connections topology placement V1: bounded study

## Question and controlled setup

Can placement alone make the current full-size 12-node Connections stress topologies geometrically viable? This is an isolated Connections implementation study, not a rule for the shared Uptime design or the playable `Connections.unity` scene. The source posture is in [Connections Current State](../Connections/Current_State.md); the fixed baseline is [Topology Stress V0](connections-topology-stress-v0.md) and its [measurements](topology-stress-v0/measurements.csv). The [geometry capacity study](connections-geometry-capacity-v0.md) explains why visible length alone cannot guarantee readable identities.

V0 and V1 use the same generated trees, node roles, origin counts, routes, traffic seed 2401, Typical density, 60 simulated seconds, 290 × 96 cards, 1920 × 1080 capture, and existing network viewport and presentation. The authored stress scene selects V1; `TopologyStressHarness.SetPlacementMode` and `TopologyStressBatch.Run` retain an isolated V0 option. The production Connections and Packet scenes were not edited. Original V0 captures and CSV were not regenerated.

## Method

V1 assigns the generated tree to evenly spaced candidate slots within the existing viewport. It tries fitting two through four columns, places external origins near different perimeter regions, assigns each internal node near its tree parent with a seeded tie break, and searches pairwise swaps. For 8 and 12 nodes it uses three starts per fitting grid and at most three best-swap passes per start; for 16–28 nodes it uses two starts and one pass. The 4-node control retains the V0 coordinates after the candidate search reduced a busy link's attachment room. The search runs once during topology creation and uses no frame timing or presentation solver calls.

The lexicographic score first rejects card overlap, viewport overflow, link passage through another card, links below 60 px, and links below 120 px. It then considers unrelated-link crossings, links below 180 px and their length deficit, crowded approaches at junctions, origin separation, total edge length, and spatial balance. Link length is measured between rectangular card boundaries along the center-to-center direction. The 120 and 180 px thresholds are diagnostics and search preferences, not identity-packing guarantees. V1 uses the authored node template's dimensions. It does not resize cards or change graph generation.

The new CSV retains the 17 V0 columns and adds crossing and short-link counts for all links and occupied links. Its `legacy_*` columns recompute those new diagnostics from the same graph and V0 placement; the other V0 values below come from the original V0 CSV. `links_through_other_nodes` retains the V0 sampled measurement for comparability, while the optimizer uses exact segment/rectangle intersection. Crossings exclude pairs of edges that share a node.

## Results

Every cell reads **V0 → V1**. `Cards` is overlap/overflow; `short` and `occupied` are counts below 120/below 180 px; `IDs` is Unavailable/Rotating/Overlaid occupied links. The [V1 CSV](topology-placement-v1/measurements.csv) also has the V0 fields for active connections, visible IDs, total link uses, and below-minimum-width links.

| Nodes/seed | Cards | Through cards | Crossings | Shortest px | Short all | Short occupied | IDs |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 4/1701 | 0/0 → 0/0 | 0 → 0 | 0 → 0 | 206.68 → 206.68 | 0/0 → 0/0 | 0/0 → 0/0 | 0/1/1 → 0/1/1 |
| 8/1701 | 0/1 → 0/0 | 1 → 0 | 1 → 0 | 157.84 → 303.5 | 0/2 → 0/0 | 0/2 → 0/0 | 0/2/0 → 0/0/2 |
| 12/1701 | 0/1 → 0/0 | 2 → 0 | 1 → 0 | 7.82 → 158.6 | 5/5 → 0/6 | 4/4 → 0/5 | 4/1/0 → 0/1/2 |
| 16/1701 | 2/1 → 0/0 | 8 → 3 | 3 → 1 | 7.73 → 9.07 | 5/7 → 2/6 | 4/6 → 2/6 | 4/3/0 → 2/2/0 |
| 20/1701 | 18/1 → 0/0 | 15 → 7 | 6 → 15 | 6.57 → 103.75 | 10/15 → 3/3 | 7/11 → 2/2 | 7/2/0 → 0/2/1 |
| 24/1701 | 33/1 → 0/0 | 25 → 24 | 8 → 24 | 9.62 → 63.8 | 14/17 → 1/6 | 11/14 → 1/4 | 11/2/0 → 4/1/0 |
| 28/1701 | 42/1 → 0/0 | 40 → 43 | 8 → 17 | 31.22 → 9.07 | 16/19 → 3/8 | 10/13 → 3/5 | 10/2/0 → 3/1/0 |
| 4/3141 | 0/0 → 0/0 | 0 → 0 | 0 → 0 | 206.68 → 206.68 | 0/0 → 0/0 | 0/0 → 0/0 | 0/1/1 → 0/1/1 |
| 8/3141 | 0/1 → 0/0 | 1 → 0 | 1 → 0 | 158.32 → 303.5 | 0/2 → 0/0 | 0/2 → 0/0 | 0/2/0 → 0/0/2 |
| 12/3141 | 0/1 → 0/0 | 2 → 0 | 1 → 0 | 7.81 → 158.6 | 5/6 → 0/3 | 4/5 → 0/2 | 4/1/0 → 0/0/2 |
| 16/3141 | 1/1 → 0/0 | 6 → 1 | 2 → 7 | 7.76 → 63.8 | 7/9 → 3/5 | 6/8 → 3/4 | 6/2/0 → 3/1/1 |
| 20/3141 | 17/1 → 0/0 | 11 → 8 | 5 → 14 | 0.9 → 103.75 | 13/15 → 1/1 | 9/11 → 1/1 | 9/2/0 → 0/4/0 |
| 24/3141 | 27/1 → 0/0 | 26 → 25 | 5 → 15 | 0.9 → 18.14 | 13/16 → 2/10 | 11/13 → 2/8 | 11/2/0 → 2/1/0 |
| 28/3141 | 39/1 → 0/0 | 35 → 43 | 6 → 22 | 0.9 → 9.07 | 17/19 → 5/8 | 10/12 → 2/3 | 10/2/0 → 2/1/0 |
| 4/9001 | 0/0 → 0/0 | 0 → 0 | 0 → 0 | 206.68 → 206.68 | 0/0 → 0/0 | 0/0 → 0/0 | 0/1/1 → 0/1/1 |
| 8/9001 | 0/1 → 0/0 | 1 → 0 | 1 → 0 | 156.64 → 303.5 | 0/2 → 0/0 | 0/2 → 0/0 | 0/1/1 → 0/0/1 |
| 12/9001 | 0/1 → 0/0 | 2 → 0 | 1 → 0 | 7.74 → 158.6 | 4/6 → 0/6 | 4/4 → 0/4 | 4/0/0 → 0/0/1 |
| 16/9001 | 1/1 → 0/0 | 6 → 2 | 1 → 4 | 7.74 → 9.07 | 8/9 → 2/6 | 7/8 → 2/5 | 6/1/0 → 2/2/0 |
| 20/9001 | 16/1 → 0/0 | 10 → 8 | 1 → 11 | 5.66 → 103.75 | 10/12 → 2/2 | 7/9 → 1/1 | 7/0/0 → 0/3/0 |
| 24/9001 | 22/1 → 0/0 | 23 → 25 | 3 → 15 | 14.23 → 18.14 | 13/15 → 1/12 | 10/12 → 1/9 | 9/0/0 → 1/1/0 |
| 28/9001 | 45/1 → 0/0 | 27 → 43 | 4 → 21 | 14.66 → 37.17 | 18/20 → 3/9 | 12/13 → 3/5 | 11/1/0 → 3/1/0 |

### Representative captures

The V1 directory contains [all 21 native captures](topology-placement-v1/) and the baseline directory contains [the 21 V0 captures](topology-stress-v0/). The three primary comparisons are:

| Seed | V0 | V1 |
| --- | --- | --- |
| 1701 | [capture](topology-stress-v0/n12-s1701.png) | [capture](topology-placement-v1/n12-s1701.png) |
| 3141 | [capture](topology-stress-v0/n12-s3141.png) | [capture](topology-placement-v1/n12-s3141.png) |
| 9001 | [capture](topology-stress-v0/n12-s9001.png) | [capture](topology-placement-v1/n12-s9001.png) |

At 12 nodes, placement removes all measured card collisions, overflow, unrelated-card obstruction, crossings, links below 120 px, and `Unavailable` occupied links across the three seeds. Shortest visible links rise from roughly 8 px to 159 px. The trees occupy distinct rows and broad regions, with opposing ingress in the visible examples. This establishes **geometric feasibility for these fixed 12-node cases** with full-size cards.

The images do not establish comfortable reading. The 1701 and 3141 captures retain long, dense diagonal bundles with labels close to one another and several IDs overlaid by the existing identity presenter. Seed 9001 has a clearer broad shape but still concentrates routes at junctions. Five of the 11 links in 1701 remain occupied and below 180 px; the other seeds have two and four. The 8-node placements clear V0 overflow and obstruction, although two 8-node seeds shift from rotation to overlay. The 4-node control is unchanged.

At 16 nodes and above, the slot arrangement still keeps cards inside the viewport without overlap, but many links pass through unrelated cards and crossings often increase. At 16, the shortest V1 links range from 9 to 64 px and two to three occupied links remain `Unavailable`. At 24 and 28, 15–43 unrelated-card obstructions and up to 24 crossings make the layouts poor despite fewer sub-120-px links. `Unavailable` improves in most high-count cases but persists. These counts are evidence of the limits of this placement and full-size viewport, not a usable scaling result.

## Remaining limitations and next experiment

| Source | Observed limitation |
| --- | --- |
| Placement | At 16+, bounded swaps cannot satisfy short-link, crossing, and obstruction demands together. The lexicographic choice favors clearing card collisions and short links before crossings. |
| Graph structure | Fixed trees can place several highly used branches at one junction; routes converge even when the cards are separated. This study did not alter the graph or routes. |
| Connection density and identity packing | Busy links still rotate or overlay IDs at 8–12 nodes. A 159 px shortest link does not imply enough capacity for all simultaneous IDs. |
| Node footprint and finite viewport | Full-size 290 × 96 cards fit in slots at 28 nodes, but free space for unobstructed and useful links vanishes. |
| Shared attachment and rendering | Multiple physical links and many logical lanes can approach the same card edge or cross near labels. There is no cross-link attachment allocation; static captures cannot judge animation or command use. |

**Recommended next experiment:** at the fixed 12-node V1 placements, measure and visually inspect shared-card attachment and label interference under the existing Typical workload. Keep topology, placement, card size, and connection density fixed. This isolates the remaining junction readability problem before choosing a rendering or attachment change.

## Validation and caveats

Unity 6000.3.11f1 compiled the isolated project and passed `TopologyStressWitness` (21 deterministic cases, reversed edge enumeration, finite cards, diagnostic geometry), Geometry, Resource, and Traffic witnesses, plus authored scene binding. `TopologyStressBatch.RunPlacementV1` completed 21 60-second cases with 21 PNGs and 21 CSV rows. The isolated batch Editor logged Search index `ArgumentOutOfRangeException` messages and a `ConnectionsRunRecorder` file-replace `IOException`; it still completed all captures and exited 0. These warnings were not treated as geometry or simulation measurements.

The 24-node workload exposed a preexisting `IdentityPacking.BuildRotation` no-progress loop when the fallback cycle contained only lanes already scheduled. A small guard now returns `Unavailable` for that impossible coverage, and a witness covers that case. Successful packing behavior and presentation geometry were not otherwise changed. This guard is a production-code correctness fix required for the batch, separate from the placement hypothesis.

Automated screenshots have not been checked in a live interactive Unity session. Review the stress scene at 16:9, especially 12-node label crowding and animated lanes, then verify `inspect` output and keyboard command focus in the playable Connections scene.
