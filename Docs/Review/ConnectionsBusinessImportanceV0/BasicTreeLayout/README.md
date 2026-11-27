# PR #139 — Small Connections tree layout

Authored positions only in `Assets/Scenes/Connections.unity`. EXT1 and API share the center spine; DB, CACHE and AUTH are evenly spaced left-to-right on one bottom row. Vertical levels are equally spaced. Existing card dimensions (290×96), topology/link IDs, capacities, node properties, presentation assets and gameplay code remain unchanged. Advanced is byte-identical.

| Node | Normalized anchor (x, y) | Position offset |
| --- | --- | --- |
| EXT1 | (0.50, 0.82) | (0, 0) |
| API | (0.50, 0.51), retained | (0, 0), retained |
| DB | (0.17, 0.20) | (0, 0), retained |
| CACHE | (0.50, 0.20) | (0, 0) |
| AUTH | (0.83, 0.20) | (0, 0), retained |

The existing physical/logical presenters derive geometry, ports, socket placement and label poses from these authored transforms. No layout manager, label override, pill-packing change or runtime code is introduced.

## Actual Unity validation

Unity **6000.3.11f1**, isolated project copy, scripted actual Play Mode. **361 checks pass**, covering both native supported viewports, symmetry/level spacing, node bounds/nonoverlap, original E1–E4 topology, six real established connections (two per bottom destination), and clearance between every visible identity pill, edge label and node card. Pill/pill and label/pill comparisons use the existing oriented-bounds intersection helper. Existing packing/cycling behavior under crowding remains unchanged.

| Viewport | Empty authored topology | Six parallel connections |
| --- | --- | --- |
| 1920×1080 | [Topology](1920-empty.png) | [Parallel rails/pills](1920-parallel.png) |
| 1280×720 | [Topology](1280-empty.png) | [Parallel rails/pills](1280-parallel.png) |

[Exact checks](checks.txt). The temporary capture script existed only in the isolated validation copy and is not added as a production tool. It disabled ambient generation for the screenshots and created ordinary/IMPORTANT WRITE requests to each bottom node; normal host stepping established them before the comparison clock was paused. No canonical tuning was saved. The successful run exited 0; the inherited nonfatal Unity QuickSearch startup indexing exception was observed again.

A source comparison proves that removing only anchor/anchored-position lines makes Small byte-identical to its prior scene. Advanced is byte-identical to the previous feature head (SHA-256 `0cb929f350a7121cb6aaee1d771038c85895c949afcc3c70fb71dd5578b02d15`). No simulation regression matrix was repeated for this transform-only change; prior PR validation and inherited caveats remain in the parent review. Core was regenerated from the current-state source and checked.

Manual review: open **Connections**, use Game view **1920×1080** and **1280×720**, and enter normal Play Mode. Check the tree, fan-out links, edge labels and parallel traffic around API. Existing authored **Load Business Importance Comparison** remains available for a controlled mixed-route example; stop/reload without saving its temporary fixture tuning. Advanced has not been altered.
