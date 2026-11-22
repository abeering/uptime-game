"""Reduce actual Unity witness traces. No gameplay simulation is implemented here.

python3 Docs/Review/ConnectionsCongestionAmbientV1/summarize.py [--plots]
Only --plots requires matplotlib. Each *.json.gz input is one recorded Unity suite.
"""
import csv
import gzip
import json
import math
from pathlib import Path
import statistics as stats
import sys

ROOT = Path(__file__).resolve().parent
runs = []
for path in sorted(ROOT.glob("*.json.gz")):
    for item in json.load(gzip.open(path))["runs"]:
        item["suite"] = path.name.removesuffix(".json.gz")
        runs.append(item)
rows = []
comparisons = 0
for item in runs:
    run = item["measurement"]; config = item["configuration"]
    row = {"suite": item["suite"], "configuration": config["name"], **{k: v for k,v in run.items()
        if k not in ("samples", "operations", "nodes", "work", "demandPrefixSignatures", "planPrefixSignatures")}}
    row.update({"confirmedWork": run["work"]["credited"], "lostWork": run["work"]["lost"], "unfinishedWork": run["work"]["unfinished"]})
    row["actualHealthyPair"] = False
    healthy = next((x["measurement"] for x in runs if x["suite"] == item["suite"] and x["configuration"]["name"] == config["name"]
        and x["measurement"]["scene"] == run["scene"] and x["measurement"]["seed"] == run["seed"] and x["measurement"]["scenario"] == "healthy"), None)
    if healthy and run["policy"] != "PopulationResponsive":
        assert run["arrived"] == run["admitted"] and run["waiting"] == run["unadmitted"] == 0
        same = all(run[k] == healthy[k] for k in ("demandSignature", "planSignature"))
        if not same:
            n = min(run["arrived"], healthy["arrived"])
            assert n > 0 and (run["coreBreachAt"] >= 0 or healthy["coreBreachAt"] >= 0 or
                             item["suite"].endswith("quotas") and (run["ending"] == "WorkComplete" or healthy["ending"] == "WorkComplete"))
            assert all(run[k][n-1] == healthy[k][n-1] for k in ("demandPrefixSignatures", "planPrefixSignatures"))
        row["actualHealthyPair"] = True
        if run["scenario"] != "healthy": comparisons += 1
        if run["removedAt"] >= 0:
            base_by_second = {math.floor(s["time"]): s for s in healthy["samples"]}
            paired = [(s, base_by_second[math.floor(s["time"])]) for s in run["samples"] if math.floor(s["time"]) in base_by_second]
            tail = [(s,h) for s,h in paired if s["time"] >= run["removedAt"]]
            def shortfall(s):
                return sum(max(0,a-b) for a,b in zip(s["benignRequested"],s["benignAllocated"]))
            if tail:
                differences = [shortfall(s)-shortfall(h) for s,h in tail]
                row["extraShortfallLoadSecondsSampled"] = sum(differences)
                row["positiveExtraShortfallLoadSecondsSampled"] = sum(max(0,d) for d in differences)
                row["extraContentionSecondsSampled"] = sum(int(shortfall(s)>.01)-int(shortfall(h)>.01) for s,h in tail)
                longest = current = 0
                for delta in differences:
                    current = current+1 if delta>.1 else 0
                    longest = max(longest,current)
                row["longestExtraShortfallSecondsSampled"] = longest
                row["extraOutstandingAtRemovalSampled"] = tail[0][0]["outstandingWork"]-tail[0][1]["outstandingWork"]
                row["extraWorkDebtAreaSampled"] = sum(max(0,s["outstandingWork"]-h["outstandingWork"]) for s,h in tail)
                row["creditDeficitAtCommonHorizon"] = paired[-1][1]["credited"]-paired[-1][0]["credited"]
                row["maxCreditDeficit"] = max(h["credited"]-s["credited"] for s,h in paired)
                row["positivePostSourceCreditDeficitAreaSampled"] = sum(max(0,h["credited"]-s["credited"]) for s,h in tail)
                for label, values in (("healthyQuietDelaySampled", [(h["time"], shortfall(h)>.01) for s,h in tail]),
                                      ("workDebtClearDelaySampled", [(s["time"], s["outstandingWork"]-h["outstandingWork"]>1) for s,h in tail])):
                    start = None; row[label] = -1
                    for time,busy in values:
                        if busy: start = None
                        else:
                            start = time if start is None else start
                            if time-start >= (10 if label.startswith("healthy") else 5):
                                row[label] = max(0,start-run["removedAt"]); break
                base_ops = {(o["id"],o["index"]):o for o in healthy["operations"]}
                delays = [o["actualSeconds"]-base_ops[o["id"],o["index"]]["actualSeconds"] for o in run["operations"] if (o["id"],o["index"]) in base_ops]
                row["pairedCompletedOperationMeanExtraSeconds"] = stats.mean(delays) if delays else None
    rows.append(row)

fields = list(dict.fromkeys(k for row in rows for k in row))
with (ROOT/"runs.csv").open("w",newline="") as file:
    writer=csv.DictWriter(file,fieldnames=fields);writer.writeheader();writer.writerows(rows)

lines=["# Actual Unity measurement tables", "", "Healthy rows use observed duration; CORE endings censor runs. Extra metrics below compare identical scheduled demand at common approximately one-second samples, not unpaired population-controller traces.", "",
       "| Suite | Scene | Configuration | Seeds | Mean arrivals | Mean active | Peak active | Healthy contention | Healthy Integrity damage | Healthy CORE |",
       "|---|---|---|---:|---:|---:|---:|---:|---:|---:|"]
for key in sorted({(r["suite"],r["scene"],r["configuration"]) for r in rows if r["scenario"]=="healthy"}):
    g=[r for r in rows if (r["suite"],r["scene"],r["configuration"])==key and r["scenario"]=="healthy"]
    lines.append("| "+" | ".join(key)+f" | {len(g)} | {stats.mean(r['arrived'] for r in g):.1f} | {stats.mean(r['averageActive'] for r in g):.2f} | {max(r['peakActive'] for r in g)} | {stats.mean(r['contentionSeconds']/r['seconds'] for r in g):.1%} | {sum(r['integrityStrikes']>0 for r in g)}/{len(g)} | {sum(r['coreBreachAt']>=0 for r in g)}/{len(g)} |")
lines += ["", "| Suite | Scene | Configuration / scenario | Seeds | Median positive extra shortfall LOAD·s | Median extra contention seconds | Longest extra-shortfall run | Median extra WORK at removal | Median credit deficit at common horizon |",
          "|---|---|---|---:|---:|---:|---:|---:|---:|"]
for key in sorted({(r["suite"],r["scene"],r["configuration"],r["scenario"]) for r in rows if "extraContentionSecondsSampled" in r}):
    g=[r for r in rows if (r["suite"],r["scene"],r["configuration"],r["scenario"])==key and "extraContentionSecondsSampled" in r]
    lines.append("| "+" | ".join(key[:2])+f" | {key[2]} / {key[3]} | {len(g)} | {stats.median(r['positiveExtraShortfallLoadSecondsSampled'] for r in g):.2f} | {stats.median(r['extraContentionSecondsSampled'] for r in g):.1f} | {max(r['longestExtraShortfallSecondsSampled'] for r in g)}s | {stats.median(r['extraOutstandingAtRemovalSampled'] for r in g):.1f} | {stats.median(r['creditDeficitAtCommonHorizon'] for r in g):.1f} |")
lines += ["", "| Suite | Scene | Configuration / operator | Success | CORE | Median successful time | Peak visible |",
          "|---|---|---|---:|---:|---:|---:|"]
for key in sorted({(r["suite"],r["scene"],r["configuration"],r["scenario"]) for r in rows if r["suite"].endswith("natural")}):
    g=[r for r in rows if (r["suite"],r["scene"],r["configuration"],r["scenario"])==key]
    wins=[r["seconds"] for r in g if r["ending"]=="WorkComplete"]
    median=f"{stats.median(wins):.2f}s" if wins else "—"
    lines.append("| "+" | ".join(key[:2])+f" | {key[2]} / {key[3]} | {len(wins)}/{len(g)} | {sum(r['coreBreachAt']>=0 for r in g)}/{len(g)} | {median} | {max(r['peakVisible'] for r in g)} |")
(ROOT/"tables.md").write_text("\n".join(lines)+"\n")
print(f"PASS: {len(runs)} actual Unity runs reduced; {comparisons} nominal/independent healthy pairs verified through common ending-censored demand/recipe prefix.")
if "--plots" in sys.argv:
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    # Select explicit configurations in RESULTS.md; no automatic best-seed selection.
    chosen=[("nominal-first","nominal",1701),("full","nominal-load2-db-busy",1701)]
    fig,axes=plt.subplots(4,len(chosen),figsize=(13,12),sharex="col",layout="constrained")
    for col,(suite,name,seed) in enumerate(chosen):
        group=[x["measurement"] for x in runs if x["suite"]==suite and x["configuration"]["name"]==name and x["measurement"]["scene"]=="Connections" and x["measurement"]["seed"]==seed]
        for run in group:
            if run["scenario"].endswith(("-overclock","-shed")): continue
            samples=run["samples"]; db=next(i for i,n in enumerate(run["nodes"]) if n["id"]=="DB");times=[s["time"] for s in samples]
            values=([s["requested"][db] for s in samples],[s["active"] for s in samples],[s["outstandingWork"] for s in samples],[s["credited"] for s in samples])
            for row in range(4):
                axes[row,col].plot(times,values[row],label=run["scenario"],linewidth=1.3)
                if run["removedAt"]>=0:axes[row,col].axvline(run["removedAt"],color="gray",alpha=.2,linestyle=":")
        axes[0,col].axhline(10,color="black",linestyle="--",linewidth=.8)
        axes[0,col].set_title(f"Small / {name} / seed {seed}")
        axes[0,col].legend(fontsize=7)
        for row,label in enumerate(("DB requested LOAD","Active legitimate","Outstanding WORK","Confirmed WORK")):
            axes[row,col].set_ylabel(label);axes[row,col].grid(alpha=.2)
        axes[3,col].set_xlabel("Actual simulation seconds")
    fig.suptitle("Actual Unity paired demand: nominal clock alone and the moderate LOAD/concentration candidate\nBlack dashed line: ordinary DB capacity. Gray dotted lines: last source removal. Seed 1701 is illustrative.")
    fig.savefig(ROOT/"paired-traces.png",dpi=150);plt.close(fig)
    fig,axes=plt.subplots(3,2,figsize=(12,10),layout="constrained")
    for col,(suite,scene) in enumerate((("full","Connections"),("advanced-target","ConnectionsAdvanced"))):
        group=[r for r in rows if r["suite"]==suite and r["scene"]==scene]
        for seed in (1701,1702,3141,9001):
            points=[next(r for r in group if r["seed"]==seed and r["scenario"]==f"kill{seconds}") for seconds in (5,10,20,30)]
            axes[0,col].plot((5,10,20,30),[r["positiveExtraShortfallLoadSecondsSampled"] for r in points],"o-",label=str(seed))
            axes[1,col].plot((5,10,20,30),[r["pairedCompletedOperationMeanExtraSeconds"] for r in points],"o-")
        strategies=("kill20","kill20-overclock","kill20-shed")
        values=[stats.mean(r["positiveExtraShortfallLoadSecondsSampled"] for r in group if r["scenario"]==strategy) for strategy in strategies]
        axes[2,col].bar(("kill only","+ Overclock","+ 1 legit kill"),values,color=("#b07e44","#507e60","#805e90"))
        axes[0,col].set_title(scene + (" / LOAD×2, goal3" if col==0 else " / LOAD×1.5, goal8"))
        axes[0,col].set_ylabel("Positive extra post-source\nshortfall (LOAD·seconds)");axes[0,col].legend(title="Seed",fontsize=8)
        axes[1,col].set_ylabel("Paired completed-operation\nmean extra seconds");axes[1,col].set_xlabel("Source active survival seconds")
        axes[2,col].set_ylabel("Mean extra post-source\nshortfall (LOAD·seconds)")
        for row in range(3):axes[row,col].grid(axis="y",alpha=.2)
    fig.suptitle("Real Unity dose response and recovery tools, relative to identical healthy demand\nTarget fixed from the healthy trace; sampled shortfall ≠ all background contention. Legitimate kills lose WORK.")
    fig.savefig(ROOT/"duration-and-tools.png",dpi=150);plt.close(fig)
