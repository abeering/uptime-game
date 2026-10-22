# Uptime

Uptime is one game design currently explored through two independent gameplay prototypes. Packet is parked but retained; Connections is the active experiment. Neither is declared the final implementation or an automatic migration of the other. Start with the [documentation map](Docs/README.md), [current project state](Docs/Uptime_Current_State.md), and [shared game design](Docs/Design/Uptime_Design.md).

| Prototype | Open this scene in the Unity Editor | Runtime code | Current state |
| --- | --- | --- | --- |
| Packet | `Assets/Scenes/GameScene.unity` (with `ABScene.unity` as another Packet scene) | Principally `Assets/Scripts/` outside `Connections/` | [Packet](Docs/Packet/Current_State.md) |
| Connections | `Assets/Scenes/Connections.unity` | `Assets/Scripts/Connections/` | [Connections](Docs/Connections/Current_State.md) |

Open the relevant scene and enter Play Mode to run that prototype. Connections has its own prefabs in `Assets/Prefabs/Connections/`; its current four-node scenario is a playable slice of Uptime, not a separate project. The scenes and simulation paths are independent. Shared game-design goals can inform both prototypes, while code or assets should be shared deliberately for a concrete use.

The project uses Unity **6000.3.11f1**. `EditorBuildSettings` still lists `SampleScene`, so standalone player builds need a prototype-specific scene list or build profile before they can start in either scene. The [Connections V0 brief](Docs/historical/connections-v0.md) records the original experiment; current Connections implementation is summarized in its [Current State](Docs/Connections/Current_State.md).
