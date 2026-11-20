# Integrity natural Pressure results

All times are simulation seconds. No loss means censored at the recorded observation duration. I/O/D counts mean infection / overload / direct Duel damage strikes. Each run uses the unchanged Pressure workload.

| Seed | Policy | Observed | Loss | I/O/D strikes | Compromised nodes | Connected internal nodes | Closest additional compromises | Commands |
|---|---|---:|---:|---|---|---|---:|---:|
| 1701 | unattended | 403.52 | 403.52 | 4/2/3 | API, DB, JOBS, WEB | API, DB, JOBS, WEB | 0 | 0 |
| 1701 | visible-defense | 402.84 | 402.84 | 4/2/2 | API, DB, JOBS | API, DB, JOBS | 0 | 12 |
| 1701 | kill-all | 599.96 | None | 3/0/3 | DB, JOBS | JOBS | 1 | 186 |
| 1702 | unattended | 205.12 | 205.12 | 4/3/1 | API, AUTH, DB, JOBS | API, DB, JOBS | 0 | 0 |
| 1702 | visible-defense | 599.96 | None | 1/3/2 | API, AUTH | None | 1 | 22 |
| 1702 | kill-all | 599.96 | None | 4/0/2 | JOBS | JOBS | 2 | 188 |
| 3141 | unattended | 331.16 | 331.16 | 6/3/1 | API, DB, JOBS, RPT | API, DB, JOBS, RPT | 0 | 0 |
| 3141 | visible-defense | 599.96 | None | 4/2/2 | API, DB, RPT | None | 1 | 25 |
| 3141 | kill-all | 599.96 | None | 6/0/0 | None | None | 2 | 199 |
| 9001 | unattended | 133.35 | 133.35 | 4/1/1 | AUTH, WEB | AUTH, WEB | 0 | 0 |
| 9001 | visible-defense | 347.93 | 347.93 | 6/3/2 | API, DB, JOBS, RPT, WEB | API, DB, JOBS, RPT, WEB | 0 | 20 |
| 9001 | kill-all | 599.96 | None | 5/0/2 | JOBS | JOBS | 2 | 189 |

## Actual terminal paths

- Seed 1701, unattended, 403.52s: EXT1 → WEB → API → DB. Final strike: duel at DB, cause `duel:4`.
- Seed 1701, visible-defense, 402.84s: EXT2 → JOBS → API → DB. Final strike: duel at DB, cause `duel:3`.
- Seed 1702, unattended, 205.12s: EXT2 → JOBS → API → DB. Final strike: infection at API, cause `x4/op:-1/PhantomLoad`.
- Seed 3141, unattended, 331.16s: EXT2 → JOBS → API → DB. Final strike: overload at API, cause `sustained_severe_load`.
- Seed 9001, unattended, 133.35s: EXT1 → WEB → AUTH. Final strike: overload at AUTH, cause `sustained_severe_load`.
- Seed 9001, visible-defense, 347.93s: EXT1 → WEB → API → DB. Final strike: infection at DB, cause `m1/op:0/PhantomLoad`.

## Damage by target and source

| Seed | Policy | Target | Infection | Overload | Duel |
|---|---|---|---:|---:|---:|
| 1701 | unattended | API | 0 | 2 | 0 |
| 1701 | unattended | DB | 1 | 0 | 1 |
| 1701 | unattended | JOBS | 1 | 0 | 1 |
| 1701 | unattended | RPT | 1 | 0 | 0 |
| 1701 | unattended | WEB | 1 | 0 | 1 |
| 1701 | visible-defense | API | 0 | 2 | 0 |
| 1701 | visible-defense | AUTH | 1 | 0 | 0 |
| 1701 | visible-defense | DB | 1 | 0 | 1 |
| 1701 | visible-defense | JOBS | 1 | 0 | 1 |
| 1701 | visible-defense | RPT | 1 | 0 | 0 |
| 1701 | kill-all | DB | 1 | 0 | 1 |
| 1701 | kill-all | JOBS | 0 | 0 | 2 |
| 1701 | kill-all | RPT | 1 | 0 | 0 |
| 1701 | kill-all | WEB | 1 | 0 | 0 |
| 1702 | unattended | API | 1 | 1 | 0 |
| 1702 | unattended | AUTH | 1 | 1 | 0 |
| 1702 | unattended | DB | 1 | 1 | 0 |
| 1702 | unattended | JOBS | 1 | 0 | 1 |
| 1702 | visible-defense | API | 0 | 2 | 0 |
| 1702 | visible-defense | AUTH | 1 | 1 | 0 |
| 1702 | visible-defense | DB | 0 | 0 | 1 |
| 1702 | visible-defense | JOBS | 0 | 0 | 1 |
| 1702 | kill-all | API | 1 | 0 | 0 |
| 1702 | kill-all | AUTH | 1 | 0 | 0 |
| 1702 | kill-all | DB | 0 | 0 | 1 |
| 1702 | kill-all | JOBS | 1 | 0 | 1 |
| 1702 | kill-all | RPT | 1 | 0 | 0 |
| 3141 | unattended | API | 1 | 1 | 0 |
| 3141 | unattended | AUTH | 1 | 0 | 0 |
| 3141 | unattended | DB | 2 | 0 | 0 |
| 3141 | unattended | JOBS | 1 | 1 | 0 |
| 3141 | unattended | RPT | 0 | 1 | 1 |
| 3141 | unattended | WEB | 1 | 0 | 0 |
| 3141 | visible-defense | API | 0 | 1 | 1 |
| 3141 | visible-defense | AUTH | 1 | 0 | 0 |
| 3141 | visible-defense | DB | 2 | 0 | 0 |
| 3141 | visible-defense | RPT | 0 | 1 | 1 |
| 3141 | visible-defense | WEB | 1 | 0 | 0 |
| 3141 | kill-all | API | 1 | 0 | 0 |
| 3141 | kill-all | AUTH | 1 | 0 | 0 |
| 3141 | kill-all | DB | 1 | 0 | 0 |
| 3141 | kill-all | JOBS | 1 | 0 | 0 |
| 3141 | kill-all | RPT | 1 | 0 | 0 |
| 3141 | kill-all | WEB | 1 | 0 | 0 |
| 9001 | unattended | API | 1 | 0 | 0 |
| 9001 | unattended | AUTH | 1 | 1 | 0 |
| 9001 | unattended | RPT | 1 | 0 | 0 |
| 9001 | unattended | WEB | 1 | 0 | 1 |
| 9001 | visible-defense | API | 1 | 1 | 0 |
| 9001 | visible-defense | AUTH | 0 | 0 | 1 |
| 9001 | visible-defense | DB | 2 | 0 | 0 |
| 9001 | visible-defense | JOBS | 1 | 1 | 0 |
| 9001 | visible-defense | RPT | 1 | 1 | 0 |
| 9001 | visible-defense | WEB | 1 | 0 | 1 |
| 9001 | kill-all | API | 0 | 0 | 1 |
| 9001 | kill-all | AUTH | 1 | 0 | 0 |
| 9001 | kill-all | DB | 1 | 0 | 0 |
| 9001 | kill-all | JOBS | 1 | 0 | 1 |
| 9001 | kill-all | RPT | 1 | 0 | 0 |
| 9001 | kill-all | WEB | 1 | 0 | 0 |

See [README](README.md) for methodology, censoring, interpretation and manual review. Per-second CSVs preserve region growth; compressed logs retain all causes, attempts, rejected commands and final full topology.
