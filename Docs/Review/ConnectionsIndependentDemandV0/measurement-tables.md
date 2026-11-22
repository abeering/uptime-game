# Generated measurement tables

Derived from actual Unity traces by `summarize.py`. Four seeds per healthy row; contention is mean fraction of observed time (CORE endpoints censor runs).

| Scene | Policy / mean wait | Mean arrivals | Mean active | Peak active | Contention | Mean shortfall LOAD·s | Mean completed-op delay | CORE |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Connections | Population (rate unused) | 15.5 | 2.38 | 4 | 0.4% | 0.1 | -0.46s | 0/4 |
| Connections | Independent / 23.23s | 13.8 | 2.14 | 6 | 0.3% | 0.1 | -0.50s | 0/4 |
| Connections | Independent / 12.00s | 26.5 | 4.42 | 10 | 6.1% | 70.6 | 0.21s | 0/4 |
| Connections | Independent / 8.00s | 44.0 | 7.49 | 15 | 23.1% | 248.3 | 1.30s | 0/4 |
| Connections | Independent / 6.00s | 62.0 | 11.13 | 28 | 47.0% | 943.0 | 4.63s | 0/4 |
| Connections | Independent / 4.00s | 62.0 | 15.93 | 39 | 72.3% | 1970.7 | 10.06s | 3/4 |
| ConnectionsAdvanced | Population (rate unused) | 19.0 | 3.21 | 5 | 0.0% | 0.0 | -0.31s | 0/4 |
| ConnectionsAdvanced | Independent / 18.95s | 17.2 | 2.83 | 7 | 0.0% | 0.0 | -0.45s | 0/4 |
| ConnectionsAdvanced | Independent / 12.00s | 26.5 | 4.47 | 9 | 0.1% | 0.0 | -1.12s | 0/4 |
| ConnectionsAdvanced | Independent / 8.00s | 44.0 | 7.17 | 15 | 5.9% | 70.1 | -0.77s | 0/4 |
| ConnectionsAdvanced | Independent / 6.00s | 62.0 | 10.62 | 22 | 26.6% | 329.2 | 1.19s | 0/4 |
| ConnectionsAdvanced | Independent / 4.00s | 91.5 | 17.92 | 37 | 56.9% | 1941.5 | 7.03s | 0/4 |

Operation means pool completed operations, including dynamic outliers; unfinished operations are censored. Negative delay is possible under natural LOAD noise. Baseline rate matching uses observed requests/360 seconds, not calibrated Python throughput.

| Scene | Policy | Operator | Success | CORE | Median successful seconds | Mean active | Peak visible |
|---|---|---|---:|---:|---:|---:|---:|
| Connections | PopulationResponsive | unattended | 3/6 | 1/6 | 262.15 | 2.33 | 7 |
| Connections | PopulationResponsive | visible-defense | 4/6 | 0/6 | 278.88 | 2.08 | 8 |
| Connections | IndependentDemand | unattended | 5/6 | 0/6 | 165.54 | 6.10 | 26 |
| Connections | IndependentDemand | visible-defense | 5/6 | 0/6 | 164.06 | 5.58 | 18 |
| ConnectionsAdvanced | PopulationResponsive | unattended | 2/6 | 2/6 | 295.98 | 3.12 | 9 |
| ConnectionsAdvanced | PopulationResponsive | visible-defense | 4/6 | 1/6 | 310.81 | 3.04 | 9 |
| ConnectionsAdvanced | IndependentDemand | unattended | 6/6 | 0/6 | 163.51 | 8.12 | 23 |
| ConnectionsAdvanced | IndependentDemand | visible-defense | 6/6 | 0/6 | 162.81 | 8.03 | 23 |
