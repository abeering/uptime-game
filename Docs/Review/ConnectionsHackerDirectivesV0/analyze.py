"""Derive this experiment's tables from committed Unity recorder evidence. Run from any directory."""
import collections
import csv
import gzip
import heapq
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PARENT = ROOT.parent / 'ConnectionsIntegrityV0'
SEEDS = (1701, 1702, 3141, 9001)
POLICIES = ('unattended', 'visible-defense', 'kill-all')


def read(path):
    with gzip.open(path, 'rt') as stream:
        return json.load(stream)


def parent_equivalent(parent, control):
    events = [e for e in control['events'] if not e['type'].startswith('hacker_')]
    references = {e['sequence']: i + 1 for i, e in enumerate(events)}

    def normalize(value):
        if isinstance(value, list):
            return [normalize(v) for v in value]
        if isinstance(value, dict):
            return {k: references.get(v, v) if k in ('sequence', 'actionSequence') and isinstance(v, int)
                    else normalize(v) for k, v in value.items() if k not in
                    ('hackerOffensive', 'hackerTarget', 'hackerDirective',
                     'hackerCommitmentSeconds', 'hackerPreferenceProbability')}
        return value

    assert parent['events'] == normalize(events), 'parent gameplay event mismatch'
    assert parent['snapshots'] == normalize(control['snapshots']), 'parent snapshot mismatch'
    old_meta = {k: v for k, v in parent['run'].items() if k not in ('runId', 'startedUtc', 'endedUtc', 'lastSavedUtc')}
    new_meta = {k: v for k, v in normalize(control['run']).items() if k not in ('runId', 'startedUtc', 'endedUtc', 'lastSavedUtc')}
    assert old_meta == new_meta, 'parent configuration mismatch'
    return len(events), len(parent['snapshots'])


def remaining_cost(graph, values):
    adjacency = collections.defaultdict(set)
    for edge in graph['links']:
        adjacency[edge['nodeA']].add(edge['nodeB'])
        adjacency[edge['nodeB']].add(edge['nodeA'])
    distances = {n['id']: 0 if n['external'] else 999 for n in graph['nodes']}
    heap = [(0, n['id']) for n in graph['nodes'] if n['external']]
    heapq.heapify(heap)
    while heap:
        distance, node = heapq.heappop(heap)
        if distance != distances[node]:
            continue
        for neighbor in adjacency[node]:
            candidate = distance + max(0, values[neighbor])
            if candidate < distances[neighbor]:
                distances[neighbor] = candidate
                heapq.heappush(heap, (candidate, neighbor))
    return min(distances[n['id']] for n in graph['nodes'] if n['critical'])


def one_hit_seconds(graph, damage, end):
    values = {n['id']: -1 if n['external'] else 2 for n in graph['nodes']}
    last, duration = 0, 0
    for hit in sorted(damage, key=lambda d: d['time']):
        if remaining_cost(graph, values) == 1:
            duration += hit['time'] - last
        values[hit['target']] = hit['after']
        last = hit['time']
    if remaining_cost(graph, values) == 1:
        duration += end - last
    return duration


def counts(values):
    return ' '.join(f'{key}:{value}' for key, value in sorted(collections.Counter(values).items())) or '—'


def write_csv(name, rows):
    with (ROOT / name).open('w', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def main():
    summary = json.loads((ROOT / 'pressure-summary.json').read_text())['runs']
    metrics, ledger, comparisons, baseline, tells = [], [], [], [], []
    logs = {}
    for entry in summary:
        mode, run = entry['directive'], entry['result']
        name = f"{run['seed']}-{run['policy']}"
        log = read(ROOT / mode / 'Runs' / (name + '.json.gz'))
        logs[mode, run['seed'], run['policy']] = log
        events, graph = log['events'], run['finalGraph']
        choices = [e['hackerTarget'] for e in events if e['type'] == 'hacker_target_decision']
        breach = graph['breach'] if graph['breached'] else {}
        damage = run['damage']
        metrics.append(dict(directive=mode, seed=run['seed'], policy=run['policy'],
            observed_seconds=round(run['observedSeconds'], 3),
            breach_seconds=round(run['lossTime'], 3) if graph['breached'] else '',
            core=breach.get('criticalNode', ''), path='>'.join(breach.get('path', [])),
            one_hit_seconds=round(one_hit_seconds(graph, damage, run['observedSeconds']), 3),
            eligible=sum(c['eligible'] for c in choices), preferred=sum(c['preferred'] for c in choices),
            target_changed=sum(c['influenced'] for c in choices),
            fallback=sum(not c['preferred'] for c in choices),
            fallback_reasons=counts(c['reason'] for c in choices if not c['preferred']),
            damage_by_node=counts(h['target'] for h in damage), damage_by_source=counts(h['source'] for h in damage),
            hostile_targets=counts(e['targetNodeId'] or '(targetless)' for e in events if e['type'] == 'hostile_activity_selected'),
            commands=run['commands']))
        ledger.extend(dict(directive=mode, seed=run['seed'], policy=run['policy'], **hit) for hit in damage)
        profiles = log['run']['traffic']['profiles']
        legitimate = {target for p in profiles if not p['isHostile'] and p['ambientWeight'] > 0 for target in p['eligibleTargetIds']}
        transit_only = sorted(n['id'] for n in graph['nodes'] if not n['external'] and n['id'] not in legitimate)
        plans = [e['definition'] for e in events if e['type'] == 'plan_queued']
        tells.append(dict(directive=mode, seed=run['seed'], policy=run['policy'],
            canonical_benign_targets='|'.join(sorted(legitimate)), transit_only='|'.join(transit_only),
            hostile_web_plans=sum(p['target'] == 'WEB' and p['isHostile'] for p in plans),
            benign_web_plans=sum(p['target'] == 'WEB' and not p['isHostile'] for p in plans),
            benign_web_transits=sum('WEB' in p['route'] and p['target'] != 'WEB' and not p['isHostile'] for p in plans)))
        if mode == 'SPREAD':
            n_events, n_snapshots = parent_equivalent(read(PARENT / 'Runs' / (name + '.json.gz')), log)
            baseline.append(f'PASS {name}: exact {n_events} gameplay events, {n_snapshots} snapshots and configuration; added telemetry removed and sequence/action references normalized.')
    for seed in SEEDS:
        for policy in POLICIES:
            a, b = (logs[mode, seed, policy]['events'] for mode in ('SPREAD', 'BREACH'))
            for event_type, fields in [('hostile_opportunity', ('scheduledSpawnTime', 'nextHostileIntervalSeconds', 'nextHostileAtSeconds')),
                                      ('hostile_activity_selected', ('scheduledSpawnTime', 'hostileActivity', 'hostileActivityRoll'))]:
                left, right = ([tuple(e[f] for f in fields) for e in events if e['type'] == event_type] for events in (a, b))
                common = min(len(left), len(right))
                mismatch = next((i for i in range(common) if left[i] != right[i]), None)
                comparisons.append(dict(seed=seed, policy=policy, event=event_type,
                    spread_count=len(left), breach_count=len(right), common_count=common,
                    identical_common_prefix=mismatch is None, first_mismatch_index='' if mismatch is None else mismatch))
    write_csv('metrics.csv', metrics)
    write_csv('damage.csv', ledger)
    write_csv('clock-category-comparison.csv', comparisons)
    write_csv('transit-target-tell.csv', tells)
    (ROOT / 'baseline-equivalence.txt').write_text('\n'.join(baseline) + '\n')
    table = ['# Natural directive comparison', '', 'Derived by `python3 analyze.py`; seconds are simulation time. Empty breach time means no loss by the observation horizon.', '',
             '| Directive | Seed | Policy | Breach s | Core/path | One-hit-away s | Eligible / preferred / changed | Fallback |',
             '|---|---:|---|---:|---|---:|---|---:|']
    for r in metrics:
        table.append(f"| {r['directive']} | {r['seed']} | {r['policy']} | {r['breach_seconds'] or '—'} | {r['path'] or '—'} | {r['one_hit_seconds']} | {r['eligible']} / {r['preferred']} / {r['target_changed']} | {r['fallback']} |")
    table += ['', 'One-hit-away is the exact integrated time when the minimum **sum of remaining Integrity** over any ingress-to-critical physical path equals one. It is computed from damage-event boundaries, not one-second topology samples. It differs from the parent’s count of nodes still needing compromise.', '',
              'Eligible means a legal, uncompromised candidate exists (on the offensive path for BREACH). Preferred includes selections already matching the ambient target; changed counts actual target changes. Fallback counts every decision that did not enter the preferred branch, including unsupported activities and the ambient probability branch. SPREAD has no preferred branch.', '',
              '`metrics.csv` contains every run’s target distribution, per-node/per-source damage, fallback reasons, commands and horizon. `damage.csv` retains each causal strike. `clock-category-comparison.csv` compares each common observed prefix without requiring runs with different loss times to have equal lengths. `transit-target-tell.csv` separates direct WEB targets from benign WEB transits.']
    (ROOT / 'RESULTS.md').write_text('\n'.join(table) + '\n')
    print(f'PASS: {len(metrics)} runs; {len(baseline)} parent equivalence comparisons; {len(ledger)} damage receipts.')


if __name__ == '__main__':
    main()
