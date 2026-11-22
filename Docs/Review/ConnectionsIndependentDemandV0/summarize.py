"""Summarize recorded Unity measurements; this script does not simulate gameplay.

Run: python3 Docs/Review/ConnectionsIndependentDemandV0/summarize.py [--plots]
Only --plots needs matplotlib. Existing Unity witnesses produce the input gzip files.
"""
import csv
import gzip
import json
import math
from pathlib import Path
import statistics as stats
import sys

ROOT = Path(__file__).resolve().parent
controlled = json.load(gzip.open(ROOT / "controlled-runs.json.gz"))["runs"]
natural = json.load(gzip.open(ROOT / "natural-runs.json.gz"))["runs"]
assert len(controlled) == 192 and len(natural) == 48
keys = ("scene", "policy", "meanInterval", "seed")

for run in controlled:
    healthy = next(r for r in controlled if r["scenario"] == "healthy" and all(r[k] == run[k] for k in keys))
    independent = run["policy"] == "IndependentDemand"
    if independent:
        assert run["arrived"] == run["admitted"] and run["waiting"] == run["unadmitted"] == 0
        same = all(run[k] == healthy[k] for k in ("demandSignature", "planSignature"))
        if not same:
            n = min(run["arrived"], healthy["arrived"])
            assert run["coreBreachAt"] >= 0 or healthy["coreBreachAt"] >= 0
            assert all(run[k][n - 1] == healthy[k][n - 1] for k in ("demandPrefixSignatures", "planPrefixSignatures"))
    run["extraOutstandingAtRemoval"] = None
    run["extraOutstandingPeakAfterRemoval"] = None
    run["workDebtRecoveryDelay"] = None
    run["extraDbContentionSecondsSampled"] = None
    run["extraGlobalContentionSecondsSampled"] = None
    run["healthyQuietDelaySampled"] = None
    run["quietDelaySampled"] = None
    if not independent or run["removedAt"] < 0:
        continue  # Population-responsive pairs deliberately have different arrival schedules.
    healthy_seconds = {math.floor(s["time"]): s for s in healthy["samples"]}
    db = next(i for i, node in enumerate(run["nodes"]) if node["id"] == "DB")
    rows = []
    extra_db = 0
    extra_global = 0
    quiet_samples = {"healthy": [], "paired": []}
    def contended(sample):
        return sum(max(0, a-b) for a,b in zip(sample["benignRequested"], sample["benignAllocated"])) > .01
    for sample in run["samples"]:
        base = healthy_seconds.get(math.floor(sample["time"]))
        if sample["time"] < run["removedAt"] or base is None:
            continue
        rows.append((sample["time"], sample["outstandingWork"] - base["outstandingWork"]))
        extra_db += int(sample["requested"][db] > sample["capacity"][db] + .01)
        extra_db -= int(base["requested"][db] > base["capacity"][db] + .01)
        extra_global += int(contended(sample)) - int(contended(base))
        quiet_samples["healthy"].append((base["time"], contended(base)))
        quiet_samples["paired"].append((sample["time"], contended(sample)))
    run["extraDbContentionSecondsSampled"] = extra_db
    run["extraGlobalContentionSecondsSampled"] = extra_global
    for label, field in (("healthy", "healthyQuietDelaySampled"), ("paired", "quietDelaySampled")):
        run[field] = -1
        start = None
        for time, busy in quiet_samples[label]:
            if busy:
                start = None
            else:
                start = time if start is None else start
                if time-start >= 10:
                    run[field] = max(0, start-run["removedAt"])
                    break
    if not rows:
        continue
    run["extraOutstandingAtRemoval"] = max(0, rows[0][1])
    run["extraOutstandingPeakAfterRemoval"] = max(0, max(debt for _, debt in rows))
    run["workDebtRecoveryDelay"] = -1  # No five-second <=1 WORK-debt window before common censoring.
    quiet = None
    for time, debt in rows:
        if debt <= 1:
            quiet = time if quiet is None else quiet
            if time - quiet >= 5:
                run["workDebtRecoveryDelay"] = max(0, quiet - run["removedAt"])
                break
        else:
            quiet = None

fields = "scene policy meanInterval seed scenario seconds arrived admitted waiting unadmitted averageActive peakActive peakVisible contentionSeconds benignShortfallLoadSeconds completedOperations completedMeanDelay loaderActivatedAt removedAt recoveryDelay normalCapacityRecoveryDelay recoveryCensored postRemovalContentionSeconds extraOutstandingAtRemoval extraOutstandingPeakAfterRemoval workDebtRecoveryDelay extraDbContentionSecondsSampled extraGlobalContentionSecondsSampled healthyQuietDelaySampled quietDelaySampled integrityStrikes compromisedNodes coreBreachAt ending".split()
with (ROOT / "controlled-summary.csv").open("w", newline="") as file:
    writer = csv.DictWriter(file, fieldnames=fields + ["confirmedWork", "eligible", "lost", "unfinished"])
    writer.writeheader()
    for run in controlled:
        writer.writerow({**{k: run[k] for k in fields}, "confirmedWork": run["work"]["credited"],
                         **{k: run["work"][k] for k in ("eligible", "lost", "unfinished")}})

lines = ["# Generated measurement tables", "", "Derived from actual Unity traces by `summarize.py`. Four seeds per healthy row; contention is mean fraction of observed time (CORE endpoints censor runs).", "",
         "| Scene | Policy / mean wait | Mean arrivals | Mean active | Peak active | Contention | Mean shortfall LOAD·s | Mean completed-op delay | CORE |",
         "|---|---|---:|---:|---:|---:|---:|---:|---:|"]
for scene in sorted({r["scene"] for r in controlled}):
    for policy in ("PopulationResponsive", "IndependentDemand"):
        for mean in sorted({r["meanInterval"] for r in controlled if r["scene"] == scene and r["policy"] == policy}, reverse=True):
            group = [r for r in controlled if r["scene"] == scene and r["policy"] == policy and r["meanInterval"] == mean and r["scenario"] == "healthy"]
            delays = [o["delaySeconds"] for r in group for o in r["operations"]]
            label = "Population (rate unused)" if policy == "PopulationResponsive" else f"Independent / {mean:.2f}s"
            lines.append(f"| {scene} | {label} | {stats.mean(r['arrived'] for r in group):.1f} | {stats.mean(r['averageActive'] for r in group):.2f} | {max(r['peakActive'] for r in group)} | {stats.mean(r['contentionSeconds']/r['seconds'] for r in group):.1%} | {stats.mean(r['benignShortfallLoadSeconds'] for r in group):.1f} | {stats.mean(delays):.2f}s | {sum(r['coreBreachAt']>=0 for r in group)}/4 |")
lines += ["", "Operation means pool completed operations, including dynamic outliers; unfinished operations are censored. Negative delay is possible under natural LOAD noise. Baseline rate matching uses observed requests/360 seconds, not calibrated Python throughput.", "",
          "| Scene | Policy | Operator | Success | CORE | Median successful seconds | Mean active | Peak visible |",
          "|---|---|---|---:|---:|---:|---:|---:|"]
for scene in sorted({r["scene"] for r in natural}):
    for policy in ("PopulationResponsive", "IndependentDemand"):
        for scenario in ("unattended", "visible-defense"):
            group = [r for r in natural if (r["scene"], r["policy"], r["scenario"]) == (scene, policy, scenario)]
            successes = [r["seconds"] for r in group if r["ending"] == "WorkComplete"]
            lines.append(f"| {scene} | {policy} | {scenario} | {len(successes)}/6 | {sum(r['coreBreachAt']>=0 for r in group)}/6 | {stats.median(successes):.2f} | {stats.mean(r['averageActive'] for r in group):.2f} | {max(r['peakVisible'] for r in group)} |")
(ROOT / "measurement-tables.md").write_text("\n".join(lines) + "\n")
print("PASS: 192 controlled runs, all independent paired schedules/plans match through common CORE-censored prefixes; all independent demand admitted. 48 natural shifts summarized.")

if "--plots" in sys.argv:
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    colors = {"healthy": "#557387", "withdraw30": "#ba7140", "kill10": "#447e52", "kill10-overclock": "#7256a0"}
    fig, axes = plt.subplots(4, 2, figsize=(13, 12), sharex="col", layout="constrained")
    for col, mean in enumerate((6, 8)):
        group = [r for r in controlled if (r["scene"], r["policy"], r["meanInterval"], r["seed"]) == ("Connections", "IndependentDemand", mean, 1701)]
        for run in group:
            samples = run["samples"]; times = [s["time"] for s in samples]; db = next(i for i,n in enumerate(run["nodes"]) if n["id"] == "DB")
            values = ([s["requested"][db] for s in samples], [s["active"] for s in samples],
                      [s["outstandingWork"] for s in samples], [s["credited"] for s in samples])
            for row in range(4):
                axes[row, col].plot(times, values[row], color=colors[run["scenario"]], label=run["scenario"], linewidth=1.4)
                if run["removedAt"] >= 0:
                    axes[row, col].axvline(run["removedAt"], color=colors[run["scenario"]], alpha=.35, linestyle=":")
        axes[0, col].axhline(10, color="black", linestyle="--", linewidth=.8, label="ordinary DB capacity")
        axes[0, col].set_title(f"Connections / mean {mean}s / seed 1701")
        for row, label in enumerate(("DB requested LOAD", "Active legitimate", "Outstanding WORK", "Confirmed WORK")):
            axes[row, col].set_ylabel(label); axes[row, col].grid(alpha=.2)
        axes[3, col].set_xlabel("Actual simulation seconds")
    axes[0, 0].legend(fontsize=8)
    fig.suptitle("Paired real Unity runs: persistent pressure near capacity, prompt LOAD recovery at the safer rate\nDotted lines: source removal. Overclock briefly raises capacity; black line is ordinary capacity.", fontsize=12)
    fig.savefig(ROOT / "paired-recovery.png", dpi=150); plt.close(fig)

    fig, axes = plt.subplots(2, 2, figsize=(11, 7), layout="constrained")
    for col, scene in enumerate(("Connections", "ConnectionsAdvanced")):
        for seed in (1701, 1702, 3141, 9001):
            group = sorted((r for r in controlled if r["scene"] == scene and r["policy"] == "IndependentDemand" and r["scenario"] == "healthy" and r["seed"] == seed), key=lambda r:r["meanInterval"])
            means = [r["meanInterval"] for r in group]
            axes[0,col].plot(means, [100*r["contentionSeconds"]/r["seconds"] for r in group], "o-", label=str(seed))
            axes[1,col].plot(means, [r["peakActive"] for r in group], "o-")
            for r in group:
                if r["coreBreachAt"] >= 0:
                    axes[0,col].scatter(r["meanInterval"], 100*r["contentionSeconds"]/r["seconds"], marker="x", s=100, color="red", zorder=5)
        axes[0,col].set_title(scene); axes[0,col].set_ylabel("Healthy contention (% observed)")
        axes[1,col].set_ylabel("Healthy peak active legitimate"); axes[1,col].set_xlabel("Mean interarrival seconds (larger = less demand)")
        for ax in axes[:,col]: ax.grid(alpha=.2)
    axes[0,0].legend(title="Seed", fontsize=8)
    fig.suptitle("Actual Unity rate sweep, no hostiles: red crosses mark CORE-censored runs", fontsize=12)
    fig.savefig(ROOT / "rate-sweep.png", dpi=150); plt.close(fig)
