# Legitimate WEB traffic — small follow-up to PR #118

Scene-only Connections content patch on `feat/connections-hacker-directives-v0`, November 20, 2026, compared with pre-patch commit `0cd6251`. The PR still targets Integrity #117 and remains unmerged. The original six-template directive experiment and its evidence are preserved in the parent folder.

## Content change

Only `ConnectionsAdvancedIntegrity.unity` adds a canonical-style template: `storefront-page` / **Storefront Page Requests**, target WEB, ambient weight **1.0**, normal READ → EXECUTE. READ retrieves page resources at LOAD 0.5–1.1 and mean WORK 24; EXECUTE renders/serves content at LOAD 0.8–1.6 and mean WORK 36. The existing ±25% WORK sampler gives 18–30 and 27–45 WORK. Twelve normal activity milestones describe page requests, resources, cache misses/retries, rendering and serving.

The six existing recipes and their destinations/weights are unchanged. Their combined weight remains 5.25; the added weight gives storefront a provisional 16% share. The serialized `purposeTemplate` flag puts it through the existing shared benign, LOADER and INFECTOR machinery. No runtime C# change was necessary. There is no separate benign-only class, new spawn process, guaranteed WEB occupancy or recipe replacement rule.

BREACH's existing “sample normal hostile recipe, then prefer target” seam can retain storefront cover when choosing WEB. It can also retarget storefront elsewhere or use another purpose at WEB, as before. Ordinary ambient and recon-follow-up storefront attacks both appear in these runs. Thus neither WEB destination nor the storefront claim alone establishes hostility. Other mismatched destination/claim combinations can still be useful evidence; this patch does not promise universal indistinguishability.

## Natural before/after comparison

Eight unattended runs use the same seeds, SPREAD/BREACH, `.02`-second host steps and 600-second-or-breach limit as the original experiment. No recipe, target, objective, anomaly or damage is forced. Seed 1701 repeats exactly in each mode. Counts below are queued connections whose **final target is WEB**, not transits, standalone Payloads or Worms.

| Mode / seed | Before window s | Before benign / hostile WEB | After window s | After benign / hostile WEB |
|---|---:|---:|---:|---:|
| SPREAD / 1701 | 403.517 | 0 / 2 | 209.068 | 2 / 6 |
| SPREAD / 1702 | 205.117 | 0 / 1 | 451.021 | 1 / 4 |
| SPREAD / 3141 | 331.157 | 0 / 0 | 327.819 | 7 / 0 |
| SPREAD / 9001 | 133.352 | 0 / 1 | 106.814 | 1 / 4 |
| BREACH / 1701 | 244.535 | 0 / 2 | 109.194 | 1 / 2 |
| BREACH / 1702 | 119.532 | 0 / 0 | 110.753 | 0 / 0 |
| BREACH / 3141 | 314.945 | 0 / 4 | 599.963 (no breach) | 7 / 4 |
| BREACH / 9001 | 122.351 | 0 / 6 | 106.814 | 1 / 7 |

Across the full after-windows, WEB receives **20 benign and 27 hostile connections**. The same storefront claim accounts for all 20 benign connections plus **4 LOADERs and 8 INFECTORs**; remaining WEB attacks retain other covers. Eight of those twelve storefront hostiles are ordinary `ambient_hostile` plans (four LOADERs, four INFECTORs); four are recon follow-ups. The new claim is therefore shared by genuine work and attacks, including ordinary BREACH traffic.

Loss times differ, so these full-window totals are not arrival-rate estimates. [comparison.csv](comparison.csv) also counts each pair only through its shorter common observation window: combined before **0 benign / 15 hostile**, after **17 benign / 23 hostile**. All eight comparisons retain identical hostile opportunity timing, category and category-roll common prefixes. Population/coverage settings and origin weights are exactly equal; realized coverage decisions/counts can differ through changed WORK lifetimes, contention and Blackout feedback. No additional frequency or coverage goal was introduced.

The content addition changes recipe RNG consumption and outcomes; it does not replay the six-template parent. One BREACH seed survives its new window, while other seeds end earlier. This is not a fresh balance conclusion, and the original mean-breach-time comparison should not be applied to this updated catalog. Visible-defense and kill-all were not rerun for this small patch.

## Actual WORK and hostile examples

Full sampled/final operations, logs, source, anomalies, observed WORK/request maxima and lifecycle counts are retained in [natural-storefront-examples.json](natural-storefront-examples.json), backed by complete compressed recorder logs in the mode folders.

- **Benign SPREAD 1701 / m7:** ordinary coverage from EXT1 to WEB, READ LOAD 0.555 / WORK 20.636 then EXECUTE 1.473 / 42.871. Both operations complete and all twelve earned activity entries emit. No infection effect.
- **Ordinary LOADER, SPREAD 1701 / z2:** storefront cover from EXT2 to WEB, existing single-operation SPIKING composition, normal EXECUTE LOAD 1.429 with sampled elevated attack request 8.869, normal timed activation and paused productive WORK. Activation and approximately 8.842 requested LOAD are observed in snapshots. No new LOADER recipe or potency rule.
- **Ordinary INFECTOR, BREACH 9001 / t7:** storefront cover retains WEB as its selected target. Existing DELAYED low-LOAD anomaly retains READ/EXECUTE and appends a WRITE carrying Blackout; its delivery request is 0.452 with WORK 38.640. It emits activity/progress before the run ends; this particular carrier does not complete delivery in the window. Other ordinary storefront INFECTORs retain high-LOAD and WORK-deviation anomalies.
- **Completed INFECTOR, BREACH 3141 / r9:** the existing recon follow-up selects matching storefront cover to WEB. Its intentionally subtle STANDARD variant retains normal READ/EXECUTE values (1.073 / 28.618 then 1.578 / 38.948), completes both operations and all twelve logs, and applies Blackout through the existing final-operation infection effect. Subtle, anomaly-free generation is an existing rule, not omitted hostile behavior.

## Checks and review

Unity **6000.3.11f1** ran against the isolated `/private/tmp/uptime-integrity-validation` project:

- [focused-checks.txt](focused-checks.txt): **177 checks** for the scene template, unforced weighted benign/LOADER/INFECTOR construction, independent normal sampling, preserved hostile effects, BREACH-compatible WEB selection and other-scene catalog isolation.
- [directive-checks.txt](directive-checks.txt): all **375** existing directive assertions pass after the catalog change.
- [integrity-checks.txt](integrity-checks.txt): all **327** existing Integrity assertions pass.
- [regression-checks.txt](regression-checks.txt): Traffic and Ambient Hostile Activity witnesses pass. The older aggregate suite's two inherited UI failures were documented in the original experiment; that broader suite was not rerun for this content-only follow-up.
- [replay-checks.txt](replay-checks.txt): two exact full event/snapshot/graph replays. [preservation-checks.txt](preservation-checks.txt): existing profile definitions/destinations, all traffic settings excluding the added profile, node/Integrity/travel settings and hostile common-prefix schedules/categories match the preserved baseline.
- [play-mode-checks.txt](play-mode-checks.txt): actual graphical Play Mode generation, inspect-text privacy and terminal restart/seeded intent replay pass with the new catalog. The known Unity Search startup indexing exception recurs; the witness exits 0. Human keyboard/layout review remains outstanding.

Reproduce with these Editor execute methods on a disposable copy: `StorefrontTrafficWitness.Natural`, `StorefrontTrafficWitness.Regressions`, and `HackerDirectivePlayWitness.Run`. Use batch mode, `-nographics -quit` for the first two, and graphics with no `-quit` for the Play Mode method. Run `python3 Docs/Review/ConnectionsHackerDirectivesV0/StorefrontPatch/compare.py` to regenerate the comparison/examples/preservation report. Run `python3 scripts/build_uptime_project_core.py --check` for documentation freshness. Natural output stays in this patch folder; parent evidence is not overwritten.

For manual Unity review, open the Integrity scene, disable Randomize Seed and try SPREAD 1701 or BREACH 3141. Inspect storefront WORK/LOAD and `logs <id>` as it progresses, then compare its claim with internally recorded hostile storefront plans. Confirm other scenes still have only their existing catalog. At 16:9 check the longer claim's pill/inspect layout, normal command input focus and terminal `restart`. No new presentation elements were introduced. Do not regenerate this experimental scene from an older phase's authoring helper, which still describes that older phase.
