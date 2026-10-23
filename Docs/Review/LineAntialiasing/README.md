# Connections line rendering review

Scope: Connections presentation only. The shader, material, and graphics below do not change Packet, the allocator, widths and lane packing, operation timing, or node/terminal typography.

## Audit findings

- Logical paths and their moving operation pulses are custom `MaskableGraphic` meshes (`PathGraphic`), not Unity `LineRenderer` components. Physical links were rotated, sprite-free UI Images. They all had hard polygon edges and the default UI material.
- `Connections.unity` uses a Screen Space Overlay canvas (`m_RenderMode: 0`). Camera post-processing is not a smoothing pass over these graphics. Increasing camera AA is therefore not the targeted fix for these lines.
- The current High quality level and graphics default reference `Assets/UniversalRenderPipelineAsset.asset`: MSAA is disabled (`m_MSAA: 1`), render scale is 1. Other quality levels reference a separate 4x-MSAA asset. The Quality Settings AA fields alone do not establish the effective URP configuration. Shared pipeline settings were left unchanged.
- The Canvas targets 1920×1080, scales with screen size, and has Pixel Perfect disabled. Fractional placement is appropriate for animated diagonal geometry; rounding every position would add visible stepping. A scaled Game View preview can introduce additional resampling beyond the native framebuffer.
- Identity pill outlines are separate rounded polygon meshes (`PillGraphic`); they are not covered by this line shader. The target markers are axis-aligned Images. Interactive review should distinguish these elements from the path beams and pulses.

The audit used the serialized scene, both pipeline assets, `QualitySettings.asset`, `GraphicsSettings.asset`, the installed URP 17.3.0 and uGUI sources, and the Connections runtime/prefab code. Unity's [URP antialiasing documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/anti-aliasing.html) explains the different camera/pipeline AA techniques and their costs.

## Implemented treatment

`AntialiasedLineGraphic` emits one quad per rectangle, with line-local coordinates and nominal half extents in its four-component UV0. The mesh extends 1.5 screen pixels beyond the nominal rectangle at the current overlay Canvas scale. `ConnectionsLine.shader` uses fragment derivatives to calculate pixel coverage on the long edges and end caps, including fractional-width lines. This smooths the edge itself without a full-screen filter, a supersampled render target, extra fringe triangles, or a unique material per connection. Coverage retains the nominal line dimensions; raster padding does not change lane packing or target geometry.

The three physical links and `LogicalPath.prefab` explicitly reference one shared `ConnectionsLine.mat`. `PathGraphic` uses the same quad treatment for its beams and pulses. The shader is unlit, does not sample textures, and supports UI stencil masks and rectangular clipping. It does not implement soft RectMask2D clipping; the existing network does not require it.

`PathGraphic.SetPath` now reuses its three snapshot arrays when the segment count stays constant, copying new values into those buffers instead of cloning all three arrays during establishment/reflow.

The extra edge fragments and derivative arithmetic have a cost. Single-quad topology, shared material, and buffer reuse establish bounded implementation costs; they do not establish a measured frame-time improvement. No CPU/GPU performance profile or draw-call count was collected.

## Validation actually run

Unity 6000.3.11f1 ran against an isolated copy of Assets, Packages, ProjectSettings, and Library in `/private/tmp/uptime-line-validation`, with the original project left open.

| Check | Expected | Observed |
| --- | --- | --- |
| Editor C# compilation | No compilation errors | Passed; existing Packet obsolete-API warnings remain |
| Existing Geometry Witness, including added shared-material bindings | Existing geometry, packing, lifecycle, marker checks and all four line bindings pass | Passed |
| Shader support/compilation | Supported Metal shader without compiler errors | Passed |
| Path mesh | Four vertices and six indices per beam; four vertices per pulse | Passed; tested eight pulses |
| UV0 payload | Half extents survive mesh creation | Passed |
| Changed path snapshot | Same array instance reused for unchanged segment count | Passed |
| GPU edge coverage, 2 px diagonal in a single-sampled render texture | Hard rectangle has binary edges; shader rectangle has partial coverage | 0 partially covered pixels on old rectangle; 1,606 on new rectangle |
| `git diff --check` | No whitespace errors | Passed |

[Comparison at native resolution](line-comparison.png): old hard rectangle above, new antialiased rectangle below. This is a controlled GPU render of the production shader/mesh, not a full scene or Play Mode capture. The temporary review method was used only in the isolated project; it is not a new repository test harness.

## Manual Unity review

1. Reload `Connections.unity` and review the Game View at 16:9, ideally native 1920×1080 at 100% display scale. Also check a smaller window and Retina scaling.
2. In Play Mode, review shallow/steep physical links, roughly 4–6 concurrent paths, narrow crowded lanes, moving READ/WRITE pulses, establishment/reflow/departure, and warning colors. Look for smoother edges without apparent gap loss or flickering end caps. Pill outlines still use their existing renderer.
3. Confirm the node headings and LOAD bars remain legible, identity labels do not crowd them, `inspect` output renders normally, and command input retains focus.
4. For a performance claim, capture CPU UI rebuild/batching, batches/draw calls, and GPU time at typical and dense traffic in the Unity Profiler/Frame Debugger. Nested canvases or further pulse/shader changes should follow that evidence.
