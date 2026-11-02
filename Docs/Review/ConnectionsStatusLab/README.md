# Connections status lab

Open `Assets/Scenes/ConnectionsStatusLab.unity`, set the Game view to **16:9**, and
enter Play Mode. The scene starts completely clean. Click the grouped buttons to
record the existing AUTH node status presentation; no simulation needs to run.

| Family | Control | Lifetime |
| --- | --- | --- |
| Pain | OFFLINE, INFECTED | Toggle; persist until toggled off or cleared |
| Pain | SCAN JAMMED | 8 unscaled seconds |
| Awareness | PORT SCANNED | 20 unscaled seconds |
| Beneficial | CLEAN | 2.5 unscaled seconds; existing presentation has no radial timer |
| Beneficial | ISOLATED | 8 unscaled seconds; also shows OFFLINE |
| Beneficial | INTERCEPT x1, CAUGHT x1 | 5 unscaled seconds |

Reapplying a timed status restarts its full lifetime. **CLEAR ALL** clears state,
timers and visible families immediately, bypassing the normal exit fade. All
other expirations use the existing presentation's exit animation. Timers and
animations continue at `Time.timeScale = 0`; pausing the Editor itself stops
frames and therefore stops drawing updates.

Combine families and rows freely, including synthetic INFECTED + CLEAN. OFFLINE
has an independent persistent toggle: isolation expiry removes its own OFFLINE
contribution without clearing that toggle. INTERCEPT and CAUGHT retain production's
single interceptor slot, so either button replaces the other outcome and restarts
the five-second window. They are not simultaneous independent statuses.

The scene copies the AUTH `NodeView` hierarchy from `Connections.unity` at main
`8affb03` and uses a **linked instance** of
`Assets/Prefabs/Connections/NodeStatus.prefab`. The showcase scales the node and
status layer together by 1.35 for recording, retaining their relative dimensions.
It uses the production bands, colors, panels, rows, animations and radial timers.
The unobstructed callout uses the production north-anchor geometry. This lab does
not evaluate traffic-aware anchor selection or multi-node crowding.

`ConnectionsStatusLab` is the only lab runtime driver. It feeds the production
`NodeStatusState` and `NodeStatusView` directly, with detached presentation values
for Conditions, isolation and interception. A small internal preview setter allows
INFECTED and CLEAN to overlap without changing gameplay's diagnosis transitions.
There is no `SimulationHost`, production `NodeStatusPresenter`, infection list,
traffic, Health, LOAD allocation, or gameplay action in this scene. The ordinary
Connections, ConnectionsAdvanced and Packet scenes and production status prefab
are unchanged. The lab is intentionally absent from build settings.

## Review and checks

1. At 1920×1080 and 1280×720, click every button individually. Check family color,
   label, entry pop, band expansion/pulse, and radial countdown where applicable.
2. Enable OFFLINE, INFECTED, SCAN JAMMED, PORT SCANNED, CLEAN, ISOLATED and
   INTERCEPT together. Watch the three families and three-row Pain/Beneficial
   stacks; replace INTERCEPT with CAUGHT.
3. Reapply timers midway through. Confirm full radial reset, independent 2.5/5/8/20
   second expiry, and normal exit fades. Let isolation expire with and without a
   separately applied OFFLINE toggle. Toggle INFECTED during CLEAN and vice versa.
4. Press CLEAR ALL during entry, while timers run, and during an exit fade. Confirm
   immediate removal; stop/restart Play to confirm a clean start.
5. For adjacent gameplay smoke review, reopen Connections and ConnectionsAdvanced,
   exercise normal scan/clean/Condition feedback and command focus, then open a
   Packet scene. No gameplay scene changes are part of this PR.

**Tools → Connections → Run Status Lab Witness** runs the deterministic authored
bindings, prefab identity, timer fractions/expiry/refresh, toggle, animation,
synthetic overlap, isolation ownership and clear checks. It opens the lab scene.
**Run Status Lab Play Mode Witness** opens the lab, enters Play Mode, exercises every
button through UI raycasts/pointer-click handlers, and waits for real expiry at
`timeScale = 0`. It exits Play Mode after about 27 seconds of checks. These menu
commands replace the currently open scene; save your work first.

For scripted capture in an isolated project, run Unity 6000.3.11f1 with
`-batchmode -projectPath <project> -executeMethod Uptime.Connections.EditorTools.NodeStatusLabWitness.RunPlayMode`
and optional `-statusLabCapturePath <directory> -witnessWidth 1920 -witnessHeight 1080`.
Omit `-quit`: the Play Mode witness exits the batch Editor itself. The witness
uses a temporary render camera for the requested image dimensions; normal lab
operation uses the authored overlay Canvas.

Validation results and captures below are scripted Editor/Play Mode evidence,
not a human interactive playtest.

- Unity 6000.3.11f1 compiled the isolated project and authored scene successfully.
- `NodeStatusLabWitness.Run`: PASS. Timed statuses observed 0.5 at half lifetime,
  1.0 after refresh, and 0/inactive at expiry; CLEAR ALL immediately hid all families.
- Existing `NodeStatusWitness.Run` and `ConditionWitness.Run`: PASS against both
  ordinary Connections scenes, including production diagnosis/clean/reset,
  Condition delivery, expiry, scan/clean coexistence and seeded replay checks.
- `NodeStatusLabWitness.RunPlayMode`: PASS at 1920×1080 and 1280×720. All nine authored controls
  passed unobstructed UI raycasts; real frames verified entry, three-family
  coexistence, radial countdown, refresh, 2.5/5/8/20-second expiry, persistent
  toggles and clear with the scaled clock paused.
- Captures at both resolutions were visually inspected for layout and readability.
  This is scripted UI pointer-event validation, not physical mouse input or a
  human interactive recording session. Packet gameplay was not run.
- Both Play Mode batch startups logged a Unity Editor Search indexing
  `ArgumentOutOfRangeException` before the checks began. The witnesses subsequently
  completed and exited successfully; no lab runtime exception was observed.
- `git diff --check` and the staged diff whitespace check passed. The gameplay
  scenes, production prefab and presenter, simulation code, and build settings
  have no diff from the inspected main baseline.

[Clean start, 1920×1080](clean-1920x1080.png) ·
[All three families, 1920×1080](all-families-1920x1080.png) ·
[All three families, 1280×720](all-families-1280x720.png)
