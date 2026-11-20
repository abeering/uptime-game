"""Derive canonical run tables and verify preserved assets against the integration base."""
import collections
import csv
import gzip
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parents[2]
BASE = 'e551207c30236cfeb0dd95e7a5a6ce5ab0eb52df'
FAMILIES = {'ordinary', 'reconnaissance', 'payload', 'worm', 'payload_launcher', 'direct_duel', 'connection_duel'}


def previous(path):
    return subprocess.check_output(['git', 'show', f'{BASE}:{path}'], cwd=REPO, text=True)


def rects(text):
    result = {}
    for block in re.split(r'(?=--- !u!)', text):
        match = re.match(r'--- !u!224 &(\d+)', block)
        if match:
            result[match[1]] = {key: found.group(1) for key in ('m_LocalRotation', 'm_LocalPosition', 'm_LocalScale',
                'm_AnchorMin', 'm_AnchorMax', 'm_AnchoredPosition', 'm_SizeDelta', 'm_Pivot')
                if (found := re.search(r'^  ' + key + r': (.+)$', block, re.M))}
    return result


def topology(text):
    nodes, links = {}, []
    for block in re.split(r'(?=--- !u!)', text):
        ident = re.match(r'--- !u!114 &(\d+)', block)
        node = re.search(r'^  nodeId: (.+)$', block, re.M)
        edge = re.search(r'^  linkId: (.+)$', block, re.M)
        if ident and node:
            nodes[ident[1]] = node[1]
        if ident and edge:
            a, b = (re.search(r'^  node' + end + r': \{fileID: (\d+)\}', block, re.M)[1] for end in ('A', 'B'))
            links.append((edge[1], a, b))
    return sorted(nodes.values()), sorted((key, nodes[a], nodes[b]) for key, a, b in links)


def main():
    checks, metrics, activity_rows = [], [], []
    all_produced, all_benign = collections.defaultdict(set), collections.defaultdict(set)
    summary = json.loads((ROOT / 'natural-summary.json').read_text())['runs']
    for entry in summary:
        scene = Path(entry['scene']).stem
        run = entry['result']
        path = ROOT / scene / entry['directive'] / entry['difficulty'] / 'Runs' / f"{run['seed']}-unattended.json.gz"
        with gzip.open(path, 'rt') as stream:
            log = json.load(stream)
        events, graph = log['events'], run['finalGraph']
        nodes = {n['id'] for n in graph['nodes']}
        edges = {frozenset((e['nodeA'], e['nodeB'])) for e in graph['links']}
        plans = [e['definition'] for e in events if e['type'] == 'plan_queued']
        for plan in plans:
            route = plan['route']
            assert route[0] == plan['origin'] and route[-1] == plan['target'] and set(route) <= nodes
            assert len(route) == len(set(route)) and all(frozenset(pair) in edges for pair in zip(route, route[1:]))
        selected = collections.Counter(e['hostileActivity'] for e in events if e['type'] == 'hostile_activity_selected')
        produced = collections.Counter(e['hostileActivity'] for e in events if e['type'] == 'hostile_activity_selected' and e['success'])
        all_produced[scene].update(produced)
        all_benign[scene].update(p['target'] for p in plans if not p['isHostile'])
        for family in sorted(FAMILIES):
            activity_rows.append(dict(scene=scene, directive=entry['directive'], difficulty=entry['difficulty'], seed=run['seed'],
                activity=family, selected=selected[family], produced=produced[family]))
        breach = graph['breach'] if graph['breached'] else {}
        metrics.append(dict(scene=scene, directive=entry['directive'], difficulty=entry['difficulty'], seed=run['seed'],
            observed_seconds=round(run['observedSeconds'], 3), breach_seconds=round(run['lossTime'], 3) if graph['breached'] else '',
            path='>'.join(breach.get('path', [])), benign_plans=sum(not p['isHostile'] for p in plans),
            hostile_plans=sum(p['isHostile'] for p in plans), hostile_mean=log['run']['traffic']['hostileMeanIntervalSeconds'],
            benign_goal=log['run']['traffic']['benignPopulationGoal'],
            influenced=sum(e['hackerTarget']['influenced'] for e in events if e['type'] == 'hacker_target_decision'),
            benign_targets=' '.join(f'{k}:{v}' for k, v in sorted(collections.Counter(p['target'] for p in plans if not p['isHostile']).items())),
            objectives=' '.join(f'{k}:{v}' for k, v in sorted(collections.Counter(e['generation']['objective'] for e in events
                if e['type'] == 'plan_queued' and e['generation']['objective']).items()))))
        checks.append(f"PASS legal natural plan routes: {scene}/{entry['directive']}/{entry['difficulty']}/{run['seed']} ({len(plans)} plans)")
    for scene in ('Connections', 'ConnectionsAdvanced'):
        assert all_produced[scene] == FAMILIES, (scene, all_produced[scene])
        expected = {'API', 'AUTH', 'DB'} if scene == 'Connections' else {'API', 'AUTH', 'DB', 'WEB', 'JOBS', 'RPT'}
        assert all_benign[scene] == expected
        checks.append(f'PASS {scene}: every activity family actually produced in natural mixed runs; every internal node observed as benign destination.')
        path = f'Assets/Scenes/{scene}.unity'
        old, new = previous(path), (REPO / path).read_text()
        assert topology(old) == topology(new)
        old_rects, new_rects = rects(old), rects(new)
        assert all(new_rects.get(key) == value for key, value in old_rects.items() if value)
        assert previous(path + '.meta') == (REPO / (path + '.meta')).read_text()
        checks.append(f'PASS {scene}: scene GUID, exact topology and all {len(old_rects)} original RectTransform geometries preserved.')
    for scene in ('ConnectionsAdvancedPressure', 'ConnectionsAdvancedIntegrity', 'ConnectionsStatusLab', 'ConnectionsTopologyStress', 'GameScene', 'ABScene'):
        for extension in ('.unity', '.unity.meta'):
            path = f'Assets/Scenes/{scene}{extension}'
            assert previous(path) == (REPO / path).read_text(), path
        checks.append('PASS unchanged fixture/Packet asset and GUID: ' + scene)
    assert not subprocess.check_output(['git', 'diff', BASE, '--name-only', '--', 'Assets/Prefabs', 'Assets/Scripts'], cwd=REPO, text=True).strip()
    checks.append('PASS runtime scripts and all prefab assets unchanged from integration base.')
    for name, rows in (('natural-metrics.csv', metrics), ('natural-activities.csv', activity_rows)):
        with (ROOT / name).open('w', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=list(rows[0])); writer.writeheader(); writer.writerows(rows)
    (ROOT / 'preservation-and-natural-checks.txt').write_text('\n'.join(checks) + '\n')
    table = ['# Natural canonical-scene checks', '', 'Sixteen unforced mixed-activity runs; seconds are simulation time. No breach means the run reached approximately 600s.', '',
        '| Scene | Directive | Intensity | Seed | Breach s | Winning path | Benign / hostile plans |', '|---|---|---|---:|---:|---|---:|']
    for row in metrics:
        table.append(f"| {row['scene']} | {row['directive']} | {row['difficulty']} | {row['seed']} | {row['breach_seconds'] or '—'} | {row['path'] or '—'} | {row['benign_plans']} / {row['hostile_plans']} |")
    table += ['', 'Authored means Small 54s / goal3 or Advanced 30s / goal4; high8s changes only the existing hostile-interval setting. All family weights remain positive. Counts are constructed connection plans, including secondary follow-ups/children, not standalone Payload/Worm/Duel counts. `natural-activities.csv` separates selected categories from successful production. A short run can legitimately contain no ordinary hostile connection despite real non-connection attacks.', '',
        'All seven families actually produce in each scene across these runs. Every internal node receives legitimate benign work. See `natural-metrics.csv` for targets, objectives and influenced selections; complete compressed recorder logs and one-second graph samples remain in each scene/directive/intensity folder. Four exact seed1701 authored-mode replays are recorded separately. This is integration evidence, not a balance or directive-effectiveness verdict.']
    (ROOT / 'RESULTS.md').write_text('\n'.join(table) + '\n')
    print(f'PASS {len(metrics)} runs, all seven families produced per scene, every internal node observed as legitimate target, geometry/GUID/fixture/runtime preservation.')


if __name__ == '__main__':
    main()
