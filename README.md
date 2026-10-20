# Uptime

This Unity project contains two independent games:

| Game | Open this scene in the Unity Editor | Runtime code |
| --- | --- | --- |
| Packet | `Assets/Scenes/GameScene.unity` (with `ABScene.unity` as another Packet scene) | `Assets/Scripts/` |
| Connections | `Assets/Scenes/Connections.unity` | `Assets/Scripts/Connections/` |

Open either scene and enter Play Mode to run that game. Connections has its own prefabs in `Assets/Prefabs/Connections/`; its current four-node scenario is the starting playable slice, not a separate project. Neither game loads or depends on the other's scene. Share code or assets deliberately when a concrete use calls for it.

The project uses Unity **6000.3.11f1**. `EditorBuildSettings` still lists `SampleScene`, so standalone player builds need a game-specific scene list or build profile before they can start in either game. The [Connections V0 brief](Docs/Design/connections-v0.md) records the first scenario and its original phased design; later ideas in it are not implemented commitments.
