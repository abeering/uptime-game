# Connections — Independent Benign Demand Experiment V0

Experimental implementation, November 22, 2026, based on PR [#124](https://github.com/abeering/uptime-game/pull/124) at `7cec0ae7e2c0b363fa07613c99cc4b3e2898dcf3`. This is an unmerged comparison, not a canonical traffic-model decision. PR #124, PR #115 and main are not modified by this branch.

## One director, one downstream pipeline

`ConnectionTrafficDirector` adds an Inspector `Benign Arrival Policy`: `PopulationResponsive` (zero/default, preserving existing scenes) or `IndependentDemand`. Restart after changing policy or rate. The independent policy uses `Independent Benign Mean Seconds` (default 8) and a separate seed-derived random stream. It samples exponential waits at a constant mean after the existing opening grace. A one-microsecond numerical guard keeps each wait positive. No bursts, company scheduling or new commands are added.

The independent clock has no occupancy, LOAD, cancellation, infection or outage inputs. Every due request samples an ordinary benign PURPOSE through existing `GenerateBenignPlan`, using its existing isolated recipe stream. Plans call the same `QueueSpawnPlan`, validation, registration, Connecting/Active lifecycle, allocator and WORK credit paths. Due requests are retained across coarse scheduler calls instead of reducing demand to one opportunity per call. Explicitly disabled ambient generation creates no demand; re-enabling starts a fresh seeded wait, following the existing hostile-clock convention.

The population policy keeps its original probability, occupancy feedback, ramp, cancellation memory and coverage scheduling. No old draws are inserted or removed. Hostile clocks, generators, enemy mechanics, resources, CORE rules, Overclock, shift endings and 900/1,200 quotas are unchanged. Additional traffic can still change their outcomes, consume shared IDs and change allocation/identity consequences; unchanged hostile rules do not imply identical runs under a changed workload.

## Demand, admission and backlog

The existing queue admits due plans immediately without a busy-network cap. This experiment introduces **no separate admission queue**. An admission means a connection was registered, including a Connecting attempt that can later cancel on an offline route; it does not mean successful establishment or completed WORK. Operational backlog is the ordinary active/connecting workload, measured with active counts and outstanding WORK.

The new recorder events distinguish `benign_demand_arrived` (ideal scheduled time plus observation time, sequence, next wait, ID or failure reason) from `benign_demand_admitted` (registration time and sequence). Full definitions remain on `plan_queued`, whose source is `independent_benign`. Metadata records policy and mean; snapshots contain independent demand totals, waiting count, unadmitted failures and next scheduled time. Recording is observational and does not determine requests or admission.

The existing 260 generated-ID pool remains a limit. Requests after exhaustion continue to be counted and recorded as `ids_exhausted`; they are **unadmitted failures**, not a waiting queue or promised future WORK. Missing weighted PURPOSEs likewise produce explicit failures. No measured comparison is accepted if demand was lost to this limit. The focused witness exercises exhaustion separately. This is sufficient for the bounded experiment, not a claim of unlimited-duration traffic support.

## Controlled experiment

The focused Editor witness runs the actual Unity host. Both scenes retain their real PURPOSE catalogs, natural LOAD noise and Integrity rules. Diagnostic runs use the unchanged shift-ending machinery with a 360-second deadline and unreachable quota, so early quota success cannot conceal a recovery tail. CORE endings remain possible and censor observation.

A fixture registers one real steady LOADER at about 90 seconds, using the existing physical-route finder, a 12-LOAD operation, zero productive WORK rate and the existing 30-active-second withdrawal lifecycle. No infection or lingering penalty is installed. It either withdraws after 30 active seconds, is killed after ten active seconds, or is killed then followed by ordinary Overclock. Its explicit ID does not consume the generator's ID/RNG streams. Paired independent runs must retain the same demand schedule and complete benign plans through their common duration. If a real CORE ending stops one run earlier, every request in that shorter prefix must match; arrivals after an ending are not invented.

Healthy population-policy runs establish an empirical arrival-rate reference. The independent grid includes that matched rate and means of 4, 6, 8 and 12 seconds. Healthy, withdrawal, kill and kill-plus-Overclock cases use four fixed seeds in each scene. A smaller natural mixed-threat sample retains the actual quotas, comparing unattended and bounded visible defense without privileged threat information.

Boundary samples integrate active counts, requested/available LOAD and legitimate allocation shortfall. Per-second traces retain outstanding workload and confirmed WORK. Operation delay is measured against authored WORK / authored LOAD, excluding travel; dynamic outliers and incomplete operations are identified, so this is not presented as an exact counterfactual simulation for every behavior. Recovery includes first ten-second no-shortfall windows, recovery after ordinary capacity returns, and total post-removal contention; paired healthy traces distinguish background bursts from a causal recovery tail.

See [review evidence and recommendation](../Review/ConnectionsIndependentDemandV0/README.md) for actual results, limitations, captures and manual playtests. No positive result is assumed and no unrelated rule is changed to produce one.

## Removing the experiment seam

After human testing, adopting independent demand would remove the enum switch, population coverage fields/functions, cancellation-pressure bookkeeping and legacy scheduling branch from this same director. Rejecting it would remove the independent clock/settings/counters and recorder additions. Neither decision requires replacing the downstream pipeline. Historical recorder formats and evidence should remain readable. This branch does not make either decision.
