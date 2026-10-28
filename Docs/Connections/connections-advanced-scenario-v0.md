# Connections — Advanced Scenario V0 (approved contract)

**Status:** Scenario and small implementation boundary approved October 28, 2026.
Connections only; the original four-node `Connections.unity` remains the playable baseline,
and Packet is untouched. This is an experimental playground, not a LevelDirector,
progression system, generated topology system, or balance claim.

## Topology for the later advanced scene

Create a separate `Assets/Scenes/ConnectionsAdvanced.unity` with eight nodes and
seven physical links:

| Node | Initial role in the scenario |
|---|---|
| EXT1 | Customer-facing external ingress |
| EXT2 | Partner/background external ingress |
| WEB | Customer ingress / transit |
| API | Shared application service |
| AUTH | Authentication service |
| DB | Database |
| JOBS | Background/integration service |
| RPT | Reporting service |

Edges (one connected tree, no rerouting or cycles):
`EXT1–WEB`, `WEB–API`, `WEB–AUTH`, `API–DB`,
`DB–RPT`, `API–JOBS`, `JOBS–EXT2`.
This describes logical adjacency, **not** final screen coordinates. Author a readable
16:9 placement using the prior stress/placement diagnostics as guidance, then playtest
live lanes, pills, markers, and command focus. Do not treat the stress generator as a
playable scenario.

## Traffic V0

Use the six existing canonical ecommerce PURPOSE recipes, their normal operation
definitions, existing LOAD/WORK mechanics, and existing threat generation.

| PURPOSE | Advanced target |
|---|---|
| `product-lookup` | DB |
| `customer-login` | AUTH |
| `cart-pricing` | API |
| `order-submit` | API |
| `inventory-sync` | JOBS |
| `sales-export` | RPT |

Configure weighted ambient origins **EXT1: 60%, EXT2: 40%**. Sample an origin
independently for each ordinary benign or hostile plan, using the same seeded planner
stream as that plan. Do not create a guaranteed connection per region. Neither origin
has PURPOSE-specific target bias in V0. The graph is connected and all six targets
are internal, so every sampled origin/target pairing has a valid route.

**Starting tuning hypothesis, not a balancing contract:** ramp benign soft coverage
from 1 to 4 established connections over 75 simulated seconds (versus 1 to 3 in
the baseline). Keep the existing 54-second mean hostile arrival clock, hostile
objective/infection mix, operation definitions, and infection/Health tuning.
This is not twice the traffic. Quiet regions are desirable.

## Small reusable generation seam (PR 1)

`ConnectionTrafficDirector` may optionally serialize:

- `ambientOrigins`: weighted origin node IDs. Empty means the existing single
  `ambientOriginId` path, without an additional RNG draw.
- `purposeTargetOverrides`: canonical PURPOSE ID to one or more eligible target
  node IDs. Empty means the existing catalog targets.

These are **scene-level generation settings**, not changes to canonical PURPOSE
operations. Validate duplicate/invalid IDs, finite origin weights, a positive
total, internal destinations, and reachable origin/target routes at reset.
All weighted origins, including zero-weight entries, count as configured ingress
nodes for Spawner target exclusion. Spawner children still originate at the
infected node, choose eligible *internal* targets from the same effective PURPOSE
destinations, and use the existing concrete-plan and lifecycle machinery.

The recorder retains the legacy `ambientOriginId` metadata, adds the effective
origin weights, and records each PURPOSE's effective target list alongside its
original authored target list. Concrete queued connection plans already contain
their actual origin, target, and route. Metadata is additive; no gameplay schema,
infections, allocator, or terminal vocabulary changes.

## PR boundaries

**PR 1:** optional routing/target overrides, Spawner target correctness, recorder
metadata, deterministic witness, and this contract. No scene/prefab edits.

**PR 2:** a separate authored advanced scene with eight nodes and the settings
above; new scene-binding and geometry witnesses, several seeded recorded runs,
and live 16:9 inspection. Keep the original four-node scene and Packet unchanged.

Only fix verified multi-node correctness or readability problems. Do not add
Probe, Priority, Throttle, new infection types, duels, upgrades, topology
progression, or a generalized topology-generation framework.

## Acceptance

- Both external origins produce benign and hostile traffic; target selection uses
  all configured destinations and valid existing paths.
- Existing LOADER/INFECTOR, Phantom LOAD/Blackout/Spawner, scan/clean/inspect/kill,
  LOAD/WORK, Health, lifecycle, and run recording operate in the advanced scene.
- Fixed seeds replay; logs explain origin, route, target, actions, infections,
  and outcomes. The original scene retains its behavior.
- At 16:9, the scene has readable ordinary play, regions that can be quiet,
  and geographically distinct opportunities to investigate/intervene.
  Synthetic saturated stress runs are **not** the acceptance workload.

References: [Topology Stress V0](../research/connections-topology-stress-v0.md),
[Topology Placement V1](../research/connections-topology-placement-v1.md),
[Traffic V0](connections-arcade-traffic-v0.md),
[Infections V0](connections-infections-v0.md),
[Lifecycle V1](connections-lifecycle-v1.md).
