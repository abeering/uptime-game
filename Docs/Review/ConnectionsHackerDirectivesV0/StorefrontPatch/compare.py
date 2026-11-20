"""Compare the preserved six-template baseline with the scene-only storefront patch."""
import csv
import gzip
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def read(path):
    with gzip.open(path, 'rt') as stream:
        return json.load(stream)


def main():
    rows, examples, checks = [], [], []
    for mode in ('SPREAD', 'BREACH'):
        for seed in (1701, 1702, 3141, 9001):
            suffix = Path(mode) / 'Runs' / f'{seed}-unattended.json.gz'
            before, after = read(ROOT.parent / suffix), read(ROOT / suffix)
            old_traffic, new_traffic = (log['run']['traffic'].copy() for log in (before, after))
            old_profiles, new_profiles = ({p['id']: p for p in traffic.pop('profiles')} for traffic in (old_traffic, new_traffic))
            assert set(new_profiles) - set(old_profiles) == {'storefront-page'}
            assert all(new_profiles[key] == value for key, value in old_profiles.items())
            assert old_traffic == new_traffic, 'coverage/hostile/origin/generation configuration changed'
            for key in ('nodes', 'connectionTravelSecondsPerLink', 'maximumIntegrity', 'integritySevereLoadRatio', 'integrityWarningSeconds'):
                assert before['run'][key] == after['run'][key]
            old_end, new_end = (log['snapshots'][-1]['timeSeconds'] for log in (before, after))
            common_end = min(old_end, new_end)
            for event_type, fields in [('hostile_opportunity', ('scheduledSpawnTime', 'nextHostileAtSeconds', 'nextHostileIntervalSeconds')),
                                      ('hostile_activity_selected', ('scheduledSpawnTime', 'hostileActivity', 'hostileActivityRoll'))]:
                sequences = [[tuple(e[f] for f in fields) for e in log['events'] if e['type'] == event_type] for log in (before, after)]
                common = min(map(len, sequences))
                assert sequences[0][:common] == sequences[1][:common], f'{mode}/{seed}: {event_type} changed'
            checks.append(f'PASS {mode}/{seed}: all existing profiles/destinations, coverage/clock/origin/anomaly settings, node/Integrity/travel settings unchanged; hostile schedule/category/roll common prefixes identical.')
            row = dict(directive=mode, seed=seed, before_seconds=round(old_end, 3), after_seconds=round(new_end, 3), common_seconds=round(common_end, 3))
            for label, log in (('before', before), ('after', after)):
                plans = [e for e in log['events'] if e['type'] == 'plan_queued' and e['definition']['target'] == 'WEB']
                for kind, hostile in (('benign', False), ('hostile', True)):
                    matching = [e for e in plans if e['definition']['isHostile'] == hostile]
                    row[f'{label}_{kind}'] = len(matching)
                    row[f'{label}_{kind}_common_window'] = sum(e['timeSeconds'] <= common_end for e in matching)
                for objective in ('NONE', 'LOADER', 'DELIVERY'):
                    row[f'{label}_storefront_{objective}'] = sum(e['definition']['profileId'] == 'storefront-page' and e['generation']['objective'] == objective for e in plans)
            rows.append(row)
            for event in after['events']:
                if event['type'] != 'plan_queued' or event['definition']['profileId'] != 'storefront-page':
                    continue
                plan, trace = event['definition'], event['generation']
                related = [e for e in after['events'] if e['connectionId'] == event['connectionId']]
                sampled = [c for snapshot in after['snapshots'] for c in snapshot['connections'] if c['id'] == event['connectionId']]
                examples.append(dict(directive=mode, seed=seed, id=event['connectionId'], target=plan['target'], origin=plan['origin'],
                    source=event['source'], objective=trace['objective'], decision=trace['generationDecision'],
                    normal=trace['normalOperations'], final=plan['operations'], anomalies=trace['anomalies'],
                    infector=trace['infector'] if trace['hasInfector'] else None,
                    loader=plan['loaderAttack'] if trace['objective'] == 'LOADER' else None,
                    maximum_sampled_request=max((c['desiredLoad'] for c in sampled), default=0),
                    maximum_sampled_work=max((c['completedWork'] for c in sampled), default=0),
                    operations_completed=sum(e['type'] == 'operation_completed' for e in related),
                    logs_emitted=sum(e['type'] == 'connection_log_emitted' for e in related),
                    loader_activated=any(e['type'] == 'loader_activated' for e in related),
                    infection_applied=any(e['type'] == 'infection_applied' for e in related),
                    delivery_attempts=sum(e['type'] == 'effect' and e['effectType'] == 'InfectionDelivery' for e in related)))
    web = [e for e in examples if e['target'] == 'WEB']
    assert any(e['objective'] == 'NONE' and e['operations_completed'] == 2 for e in web)
    assert any(e['objective'] == 'LOADER' and e['loader_activated'] for e in web)
    assert any(e['objective'] == 'DELIVERY' and e['infection_applied'] for e in web)
    with (ROOT / 'comparison.csv').open('w', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)
    (ROOT / 'natural-storefront-examples.json').write_text(json.dumps(examples, indent=2) + '\n')
    (ROOT / 'preservation-checks.txt').write_text('\n'.join(checks) + '\nPASS natural WEB storefront benign completion, LOADER activation and INFECTOR installation observed.\n')
    for row in rows:
        print(row)
    print(f'PASS eight comparisons; {len(examples)} storefront plans, {len(web)} targeting WEB.')


if __name__ == '__main__':
    main()
