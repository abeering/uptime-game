# Execution and reproduction

All runs used the actual Unity **6000.3.11f1** Editor on macOS, with disposable project copies to avoid interfering with the user's open project. Runtime source matched this branch. No canonical scene or prefab was saved. The final witness additionally records prefix hashes and resumes completed measurement groups; earlier completed groups retain their original full schedule/plan hashes.

| Check | Expected | Observed |
|---|---|---|
| IndependentDemandWitness.Run | Both scene defaults unchanged; paired demand/recipes, exact replay, reset, accounting, pause, Authored mode, coarse scheduling, Blackout and finite-ID failures | 32 assertions passed |
| IndependentDemandWitness.Measure | 192 real runs, paired independent requests/plans match, no unaccounted admission loss | 192 passed; compare complete common prefix where real CORE endings censor one side |
| IndependentDemandWitness.Natural | 48 actual quota-enabled mixed shifts, unchanged mechanics | 48 completed; outcomes retained, including failures |
| WorkShiftWitness.Run | Existing accounting/ending assertions | 52 passed |
| WorkShiftWitness.VerifyTargets | Existing 18 actual quota runs preserve expected ending/time/credit prefix | All 18 exact checks passed; original calibration files left unchanged |
| DuelConcessionWitness.Regressions | Existing applicable suites, canonical bindings and terminal/concession checks | Gameplay/terminal/parity suites passed; aggregate exited 1 for two inherited UI assertions |
| IndependentDemandCapture.Run | Both actual Play Mode scenes at both 16:9 resolutions, real source/kill/expired boost, inspect focus and paired recipes | 42 assertions passed, 16 captures |
| Generated Core / whitespace | Derived source snapshot current; no whitespace errors | Checked before commit |

The two aggregate failures are unchanged from #124: `OverclockWitness: ConnectionsAdvanced compact three-tool row` and `ResourceWitness: inspect header pill keeps authored large geometry and identity color`. No assertion or authored layout was relaxed. [Per-suite results](parent-regression-checks.txt), [881 canonical parity checks](canonical-parity-checks.txt) and [terminal checks](duel-concession-checks.txt) are retained. Graphical Editor startup also emitted a Unity QuickSearch `SearchDatabase.EnumerateAll` index exception; the capture runner subsequently completed all checks and exited successfully. This is not claimed as a clean Editor console.

## Repeat

Run Editor methods in a disposable checkout of this branch, from the project root. Unity needs its normal graphical/license permissions. The executable path below is this machine's installation; substitute yours.

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -executeMethod Uptime.Connections.EditorTools.IndependentDemandWitness.Run -logFile /tmp/demand-checks.log
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -executeMethod Uptime.Connections.EditorTools.IndependentDemandWitness.Measure -logFile /tmp/demand-controlled.log
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -executeMethod Uptime.Connections.EditorTools.IndependentDemandWitness.Natural -logFile /tmp/demand-natural.log
"$UNITY" -batchmode -projectPath "$PWD" -executeMethod Uptime.Connections.EditorTools.IndependentDemandCapture.Run -logFile /tmp/demand-graphics.log
python3 Docs/Review/ConnectionsIndependentDemandV0/summarize.py
```

The graphical method enters/exits actual Play Mode and exits batch mode itself: do not pass `-quit` or `-nographics`. Optional arguments `-demandSmallMean 8 -demandAdvancedMean 6` select rates. `Run Independent Demand Witness` is also under the existing Tools/Connections Editor menu. The focused witness opens both scenes without saving and leaves the last scene loaded.

`Measure` resumes fully completed groups from `controlled-runs.json.gz`. Move that file aside in the disposable checkout to rerun the entire matrix. Do not overwrite the committed historical evidence with a different build and call it the same measurement. `summarize.py --plots` regenerates plots when matplotlib is installed (the delivered plots used matplotlib 3.9.4). This is data reduction, not a second gameplay implementation.

Existing regression entrypoints used: `Uptime.Connections.EditorTools.DuelConcessionWitness.Regressions`, `Uptime.Connections.EditorTools.WorkShiftWitness.Run`, and `Uptime.Connections.EditorTools.WorkShiftWitness.VerifyTargets`. The latter reads the original #124 calibration already present on this branch; its observed runs/checks were copied into this new folder rather than replacing old evidence. The aggregate's nonzero exit is expected until the two inherited UI assertions are addressed separately.

## Measurement definitions and limits

- Controlled seeds: 1701, 1702, 3141, 9001. Natural adds 42 and 99. Host simulation steps are 0.02 seconds; samples are approximately one simulation second apart, including time zero/final boundary. Timestamps are Unity floats.
- Per scene: 16 population baseline runs; 80 independent runs across the measured baseline-rate match and means 4/6/8/12 seconds. Paired healthy/source cases keep exact ideal due times and complete benign definitions (including noise seeds). A CORE ending freezes generation; prefix hashes compare every request in the shorter run, without fabricating subsequent demand. Mixed natural runs do not promise identical benign IDs/plans because real hostiles share the existing ID pool.
- The real LOADER is registered around 90 seconds, activates after normal Connecting travel, requests 12 DB LOAD and has zero productive rate. Existing authored withdrawal lasts 30 active seconds; kill cases remove it after ten active seconds. No persistent infection or penalty is added. Advanced uses its real EXT1→WEB→API→DB route. The explicit fixture ID avoids consuming random draws; graphical evidence uses the shorter ID `zz` for label readability.
- Active average integrates registered legitimate Active states over the actual observed duration; Connecting is separate. Peak visible counts active/connecting logical connections, not payload sprites or every possible presentation object. All nodes retain ordinary baseline LOAD; transit/link geometry does not become an allocation constraint.
- Global contention means summed legitimate desired-minus-allocated LOAD exceeds 0.01. Severity integrates that shortfall in LOAD·seconds. Per-node integrals and samples retain requested LOAD, capacity, and productive availability after baseline/infection/defensive reservations. Node excess and global contention are related but distinct measures.
- Operation measurements cover completed legitimate operations. Actual execution time excludes Connecting and travel; nominal is authored WORK/authored LOAD. Natural LOAD noise and dynamic behavior can make delays negative or large without proving scheduler error. Each row retains generation decision; incomplete operations are censored, not assigned zero delay.
- `recoveryDelay` is the start of the first ten-second no-legitimate-shortfall window after source removal. Zero does not mean every delayed job is finished or later bursts never contend. `normalCapacityRecoveryDelay` additionally requires Overclock to have expired. `postRemovalContentionSeconds` counts all later contention, including ordinary background bursts. Always compare the paired healthy run.
- Derived CSV fields compare only independent pairs at common per-second timestamps: extra outstanding WORK versus healthy; first five-second window with <=1 extra WORK; and signed extra DB contention samples. They are approximately one-second analyses, not the 0.02-second integral. `-1` means not observed before common censoring. Population pairs receive blank derived fields because their arrival schedules change with occupancy. Extra delayed WORK alone is not sustained LOAD overload.
- Diagnostic `InsufficientWork` is the expected unreachable-quota ending, not an ordinary quota failure. CORE losses remain real. Natural runs retain actual quotas, so earlier success censors subsequent hostile exposure. The bounded visible policy makes at most one decision per six seconds: intercept a visible final-hop Payload or Overclock the most overloaded node. It does not read hidden enemies, solve Duels, scan or CLEAN. The real seeded Duel race advances after its authored splash; the unattended operator can lose. This is a reproducible limited policy, not a model of a skilled human.

No real-time human play session, maximum-density graphical capture, keyboard/IME exercise or Packet Play Mode check is claimed. Historical scenes/prefabs, old measurements and the original parent assertions remain intact.
