# Connections Worm & Isolation V0 — review

This branch adds manually spawned Worm outbreaks and temporary node isolation to the **Connections** prototype. Packet gameplay and ambient generation remain unchanged. The [implementation contract](../../Connections/connections-worm-isolation-v0.md) describes the V0 decisions, including the separate Payload/interception rule.

## 16:9 scripted Play Mode captures

These images were rendered at 1920 × 1080 from an isolated Unity 6000.3.11f1 project in **Play Mode**. A scripted Editor driver stepped the simulation and invoked the host directly to make each state reproducible. They are not evidence of interactive command focus or human visual timing review.

| State | Capture |
|---|---|
| One branch crawling from EXT1 toward WEB; persistent thread and short thick body | [Single crawl](worm-single-crawl-play.png) |
| Guaranteed API fork with three simultaneous outgoing branches | [Fork](worm-fork-play.png) |
| DB isolated while sibling branches continue; OFFLINE and timed ISOLATED are visible | [Isolation](worm-isolation-play.png) |
| Worm thread alongside seeded ambient connections and pills | [Crowded](worm-crowded-play.png) |

The first headless capture attempt produced uniformly gray files; those were discarded. The checked-in files came from a second run with graphics rendering enabled. Unity emitted an Editor Search indexing `ArgumentOutOfRangeException` on startup, outside the game stack; the scripted capture completed and all four rendered frames were inspected.

## Automated validation

- `WormWitness` passed in Unity batch Editor mode. It checks both scene bindings; growing thread and head/tail phase boundaries; effect timing and provenance; a three-way fork; full eight-node coverage with a coarse step; node-budget and zero-probability partial outbreaks; a temporary cyclic link without duplicate visits; seeded replay; resident and incoming isolation; sibling survival; status coexistence; benign and hostile cancellation with one existing −5 Health cost; Blackout/isolation independence; Payload coexistence; cooldown, expiration, and reset.
- The existing `ConditionsBatch` passed in Unity batch Editor mode. It runs Payload, Geometry, Condition, Recon Event Lifecycle, Node Status, Scan, Infection, Traffic, Recorder, Network Health, Resource, Behavior, Lifecycle, and Advanced Scene witnesses.
- `python scripts/build_uptime_project_core.py` and `python scripts/build_uptime_project_core.py --check` were run after updating `Docs/Connections/Current_State.md`.

Interactive Play Mode review is still needed in both scenes, especially the perceived wriggle and branch timing, crowded path/pill readability, status placement during live traffic, and terminal command focus. Reopen each scene from disk after switching to this branch so the Editor uses its authored Worm component bindings. The [manual steps](../../Connections/connections-worm-isolation-v0.md#play-mode-review) list the commands and observations.
