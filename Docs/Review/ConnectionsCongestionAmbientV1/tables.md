# Actual Unity measurement tables

Healthy rows use observed duration; CORE endings censor runs. Extra metrics below compare identical scheduled demand at common approximately one-second samples, not unpaired population-controller traces.

| Suite | Scene | Configuration | Seeds | Mean arrivals | Mean active | Peak active | Healthy contention | Healthy Integrity damage | Healthy CORE |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|
| advanced-quotas | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy | 2 | 13.5 | 5.44 | 10 | 16.8% | 0/2 | 0/2 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy | 4 | 36.0 | 6.23 | 10 | 11.8% | 0/4 | 0/4 |
| comparisons | Connections | independent-matched | 2 | 16.0 | 2.49 | 6 | 0.0% | 0/2 | 0/2 |
| comparisons | Connections | independent-trial | 2 | 46.5 | 7.67 | 15 | 19.7% | 1/2 | 0/2 |
| comparisons | Connections | population | 2 | 15.0 | 2.40 | 4 | 0.0% | 0/2 | 0/2 |
| comparisons | ConnectionsAdvanced | independent-matched | 2 | 19.0 | 3.16 | 7 | 0.0% | 0/2 | 0/2 |
| comparisons | ConnectionsAdvanced | independent-trial | 2 | 62.5 | 10.96 | 20 | 31.3% | 2/2 | 0/2 |
| comparisons | ConnectionsAdvanced | population | 2 | 20.0 | 3.21 | 5 | 0.0% | 0/2 | 0/2 |
| concentration | Connections | nominal-load1.5-db-goal6 | 2 | 25.5 | 5.22 | 9 | 36.4% | 1/2 | 0/2 |
| concentration | Connections | nominal-load2-db | 2 | 14.5 | 2.52 | 4 | 16.2% | 0/2 | 0/2 |
| concentration | ConnectionsAdvanced | nominal-load1.5-db-goal6 | 2 | 36.0 | 6.23 | 10 | 9.7% | 0/2 | 0/2 |
| concentration | ConnectionsAdvanced | nominal-load2-db | 2 | 18.5 | 3.27 | 6 | 4.9% | 0/2 | 0/2 |
| full | Connections | nominal-load2-db-busy | 4 | 14.8 | 2.50 | 4 | 10.2% | 0/4 | 0/4 |
| full | ConnectionsAdvanced | nominal-load2-db-busy | 4 | 18.8 | 3.29 | 6 | 2.5% | 0/4 | 0/4 |
| headroom | Connections | nominal-load2-goal6 | 2 | 28.0 | 5.24 | 9 | 35.0% | 1/2 | 0/2 |
| headroom | ConnectionsAdvanced | nominal-load2-goal6 | 2 | 34.5 | 6.23 | 9 | 38.7% | 2/2 | 0/2 |
| nominal-first | Connections | nominal | 2 | 15.0 | 2.41 | 4 | 0.0% | 0/2 | 0/2 |
| nominal-first | ConnectionsAdvanced | nominal | 2 | 19.5 | 3.21 | 5 | 0.0% | 0/2 | 0/2 |
| quotas | Connections | nominal | 2 | 12.0 | 2.31 | 4 | 0.0% | 0/2 | 0/2 |
| quotas | Connections | nominal-load2-db-busy | 2 | 7.5 | 2.37 | 4 | 11.7% | 0/2 | 0/2 |
| quotas | ConnectionsAdvanced | nominal | 2 | 17.0 | 3.17 | 5 | 0.0% | 0/2 | 0/2 |
| quotas | ConnectionsAdvanced | nominal-load2-db-busy | 2 | 11.5 | 3.07 | 5 | 0.0% | 0/2 | 0/2 |
| variations | Connections | nominal-load3 | 2 | 13.5 | 2.54 | 5 | 33.0% | 1/2 | 1/2 |
| variations | Connections | nominal-load3-headroom | 2 | 13.5 | 2.31 | 4 | 22.6% | 1/2 | 1/2 |
| variations | ConnectionsAdvanced | nominal-load3 | 2 | 19.5 | 3.37 | 6 | 27.6% | 2/2 | 0/2 |
| variations | ConnectionsAdvanced | nominal-load3-headroom | 2 | 15.5 | 2.50 | 4 | 15.8% | 0/2 | 0/2 |

| Suite | Scene | Configuration / scenario | Seeds | Median positive extra shortfall LOAD·s | Median extra contention seconds | Longest extra-shortfall run | Median extra WORK at removal | Median credit deficit at common horizon |
|---|---|---|---:|---:|---:|---:|---:|---:|
| advanced-quotas | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / kill10 | 2 | 7.34 | 1.5 | 4s | 20.0 | 158.3 |
| advanced-quotas | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / kill30 | 2 | 18.72 | 4.0 | 17s | 81.5 | 295.3 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / kill10 | 4 | 0.00 | 0.0 | 19s | 9.9 | 0.0 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / kill20 | 4 | 0.00 | 0.0 | 23s | 26.5 | 0.0 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / kill20-overclock | 4 | 0.00 | 0.0 | 12s | 26.5 | 0.0 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / kill20-shed | 4 | 0.00 | 0.0 | 0s | -59.8 | 142.4 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / kill30 | 4 | 0.00 | 0.0 | 27s | 45.5 | 0.0 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / kill5 | 4 | 0.00 | 0.0 | 8s | 3.3 | 0.0 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / overlap20 | 4 | 0.00 | 0.0 | 29s | 58.2 | 0.0 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / overlap20-overclock | 4 | 0.00 | 0.0 | 23s | 58.2 | 0.0 |
| advanced-target | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / overlap20-shed | 4 | 0.00 | 0.0 | 8s | -25.5 | 142.4 |
| comparisons | Connections | independent-matched / kill10 | 2 | 0.00 | 0.0 | 0s | 5.4 | 0.0 |
| comparisons | Connections | independent-matched / kill30 | 2 | 0.00 | 0.0 | 0s | 17.8 | 0.0 |
| comparisons | Connections | independent-matched / overlap20 | 2 | 0.00 | 0.0 | 0s | 28.7 | 0.0 |
| comparisons | Connections | independent-trial / kill10 | 2 | 1.41 | 0.0 | 4s | 15.0 | 0.0 |
| comparisons | Connections | independent-trial / kill30 | 2 | 1.03 | 0.5 | 5s | 51.3 | 0.0 |
| comparisons | Connections | independent-trial / overlap20 | 2 | 1.14 | 1.5 | 4s | 63.5 | 0.0 |
| comparisons | ConnectionsAdvanced | independent-matched / kill10 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| comparisons | ConnectionsAdvanced | independent-matched / kill30 | 2 | 0.00 | 0.0 | 0s | -0.0 | 0.0 |
| comparisons | ConnectionsAdvanced | independent-matched / overlap20 | 2 | 0.00 | 0.0 | 0s | 4.6 | 0.0 |
| comparisons | ConnectionsAdvanced | independent-trial / kill10 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| comparisons | ConnectionsAdvanced | independent-trial / kill30 | 2 | 0.00 | 0.0 | 0s | 0.6 | 0.0 |
| comparisons | ConnectionsAdvanced | independent-trial / overlap20 | 2 | 0.00 | 0.0 | 0s | 7.5 | 0.0 |
| concentration | Connections | nominal-load1.5-db-goal6 / kill10 | 2 | 121.16 | 6.5 | 20s | 26.1 | 0.0 |
| concentration | Connections | nominal-load1.5-db-goal6 / kill30 | 2 | 335.10 | 15.0 | 69s | 85.1 | 71.6 |
| concentration | Connections | nominal-load1.5-db-goal6 / overlap20 | 2 | 360.78 | 15.5 | 75s | 95.7 | 71.6 |
| concentration | Connections | nominal-load2-db / kill10 | 2 | 11.30 | 5.0 | 5s | 24.6 | 0.0 |
| concentration | Connections | nominal-load2-db / kill30 | 2 | 41.50 | 19.0 | 17s | 72.3 | 0.0 |
| concentration | Connections | nominal-load2-db / overlap20 | 2 | 45.84 | 21.5 | 18s | 83.0 | 0.0 |
| concentration | ConnectionsAdvanced | nominal-load1.5-db-goal6 / kill10 | 2 | 0.00 | 0.0 | 0s | 2.8 | 0.0 |
| concentration | ConnectionsAdvanced | nominal-load1.5-db-goal6 / kill30 | 2 | 0.00 | 0.0 | 0s | 7.8 | 0.0 |
| concentration | ConnectionsAdvanced | nominal-load1.5-db-goal6 / overlap20 | 2 | 0.00 | 0.0 | 0s | 22.0 | 0.0 |
| concentration | ConnectionsAdvanced | nominal-load2-db / kill10 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| concentration | ConnectionsAdvanced | nominal-load2-db / kill30 | 2 | 0.00 | 0.0 | 0s | 1.1 | 0.0 |
| concentration | ConnectionsAdvanced | nominal-load2-db / overlap20 | 2 | 0.00 | 0.0 | 0s | 9.0 | 0.0 |
| full | Connections | nominal-load2-db-busy / kill10 | 4 | 6.05 | 2.5 | 5s | 19.4 | 0.0 |
| full | Connections | nominal-load2-db-busy / kill20 | 4 | 10.93 | 4.5 | 11s | 35.4 | 0.0 |
| full | Connections | nominal-load2-db-busy / kill20-overclock | 4 | 10.93 | 1.5 | 9s | 35.4 | 0.0 |
| full | Connections | nominal-load2-db-busy / kill20-shed | 4 | 0.00 | -0.5 | 4s | -67.7 | 214.6 |
| full | Connections | nominal-load2-db-busy / kill30 | 4 | 15.06 | 6.5 | 17s | 45.3 | 0.0 |
| full | Connections | nominal-load2-db-busy / kill5 | 4 | 2.90 | 1.5 | 2s | 10.7 | 0.0 |
| full | Connections | nominal-load2-db-busy / overlap20 | 4 | 17.84 | 7.5 | 18s | 59.0 | 0.0 |
| full | Connections | nominal-load2-db-busy / overlap20-overclock | 4 | 11.59 | 4.5 | 16s | 59.0 | 0.0 |
| full | Connections | nominal-load2-db-busy / overlap20-shed | 4 | 0.00 | -0.5 | 4s | -47.6 | 214.6 |
| full | ConnectionsAdvanced | nominal-load2-db-busy / kill10 | 4 | 0.00 | 0.0 | 0s | 13.0 | 0.0 |
| full | ConnectionsAdvanced | nominal-load2-db-busy / kill20 | 4 | 0.00 | 0.0 | 0s | 7.9 | 0.0 |
| full | ConnectionsAdvanced | nominal-load2-db-busy / kill20-overclock | 4 | 0.00 | 0.0 | 0s | 7.9 | 0.0 |
| full | ConnectionsAdvanced | nominal-load2-db-busy / kill20-shed | 4 | 0.00 | 0.0 | 0s | -33.7 | 102.5 |
| full | ConnectionsAdvanced | nominal-load2-db-busy / kill30 | 4 | 0.00 | 0.0 | 0s | 7.4 | 0.0 |
| full | ConnectionsAdvanced | nominal-load2-db-busy / kill5 | 4 | 0.00 | 0.0 | 0s | 6.5 | 0.0 |
| full | ConnectionsAdvanced | nominal-load2-db-busy / overlap20 | 4 | 0.00 | 0.0 | 0s | 22.4 | 0.0 |
| full | ConnectionsAdvanced | nominal-load2-db-busy / overlap20-overclock | 4 | 0.00 | 0.0 | 0s | 22.4 | 0.0 |
| full | ConnectionsAdvanced | nominal-load2-db-busy / overlap20-shed | 4 | 0.00 | 0.0 | 0s | -14.8 | 85.4 |
| headroom | Connections | nominal-load2-goal6 / kill10 | 2 | 2.37 | 0.0 | 4s | 30.2 | 0.0 |
| headroom | Connections | nominal-load2-goal6 / kill30 | 2 | 6.81 | 0.0 | 12s | 93.5 | 0.0 |
| headroom | Connections | nominal-load2-goal6 / overlap20 | 2 | 7.21 | 0.0 | 13s | 103.3 | 0.0 |
| headroom | ConnectionsAdvanced | nominal-load2-goal6 / kill10 | 2 | 0.00 | 0.0 | 0s | 0.7 | 0.0 |
| headroom | ConnectionsAdvanced | nominal-load2-goal6 / kill30 | 2 | 0.00 | 0.0 | 0s | 1.9 | 0.0 |
| headroom | ConnectionsAdvanced | nominal-load2-goal6 / overlap20 | 2 | 0.00 | 0.0 | 0s | 12.5 | 0.0 |
| nominal-first | Connections | nominal / kill10 | 2 | 0.00 | 0.0 | 0s | 6.6 | 0.0 |
| nominal-first | Connections | nominal / kill20 | 2 | 0.00 | 0.0 | 0s | 13.3 | 0.0 |
| nominal-first | Connections | nominal / kill30 | 2 | 0.00 | 0.0 | 0s | 14.7 | 0.0 |
| nominal-first | Connections | nominal / kill5 | 2 | 0.00 | 0.0 | 0s | 3.3 | 0.0 |
| nominal-first | Connections | nominal / overlap20 | 2 | 0.00 | 0.0 | 0s | 31.5 | 0.0 |
| nominal-first | ConnectionsAdvanced | nominal / kill10 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| nominal-first | ConnectionsAdvanced | nominal / kill20 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| nominal-first | ConnectionsAdvanced | nominal / kill30 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| nominal-first | ConnectionsAdvanced | nominal / kill5 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| nominal-first | ConnectionsAdvanced | nominal / overlap20 | 2 | 0.00 | 0.0 | 0s | 3.1 | 0.0 |
| quotas | Connections | nominal / kill10 | 2 | 0.00 | 0.0 | 0s | 6.6 | 0.0 |
| quotas | Connections | nominal / kill30 | 2 | 0.00 | 0.0 | 0s | 14.7 | 0.0 |
| quotas | Connections | nominal-load2-db-busy / kill10 | 2 | 2.55 | 2.0 | 4s | 22.0 | 331.1 |
| quotas | Connections | nominal-load2-db-busy / kill30 | 2 | 5.10 | 5.0 | 10s | 60.3 | 331.1 |
| quotas | ConnectionsAdvanced | nominal / kill10 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| quotas | ConnectionsAdvanced | nominal / kill30 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| quotas | ConnectionsAdvanced | nominal-load2-db-busy / kill10 | 2 | 0.00 | 0.0 | 0s | 9.3 | 0.0 |
| quotas | ConnectionsAdvanced | nominal-load2-db-busy / kill30 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| variations | Connections | nominal-load3 / kill10 | 2 | 9.84 | 2.5 | 4s | 29.9 | 0.0 |
| variations | Connections | nominal-load3 / kill30 | 2 | 32.15 | 2.5 | 10s | 74.0 | 0.0 |
| variations | Connections | nominal-load3 / overlap20 | 2 | 35.03 | 3.0 | 11s | 85.8 | 0.0 |
| variations | Connections | nominal-load3-headroom / kill10 | 2 | 9.54 | 3.0 | 3s | 25.4 | 0.0 |
| variations | Connections | nominal-load3-headroom / kill30 | 2 | 32.72 | 8.0 | 10s | 70.3 | 0.0 |
| variations | Connections | nominal-load3-headroom / overlap20 | 2 | 35.67 | 9.0 | 11s | 82.2 | 0.0 |
| variations | ConnectionsAdvanced | nominal-load3 / kill10 | 2 | 0.00 | 0.0 | 0s | 0.1 | 0.0 |
| variations | ConnectionsAdvanced | nominal-load3 / kill30 | 2 | 0.00 | 0.0 | 0s | 0.2 | 0.0 |
| variations | ConnectionsAdvanced | nominal-load3 / overlap20 | 2 | 0.00 | 0.0 | 0s | 10.2 | 0.0 |
| variations | ConnectionsAdvanced | nominal-load3-headroom / kill10 | 2 | 0.00 | 0.0 | 0s | 0.0 | 0.0 |
| variations | ConnectionsAdvanced | nominal-load3-headroom / kill30 | 2 | 0.00 | 0.0 | 0s | 0.1 | 0.0 |
| variations | ConnectionsAdvanced | nominal-load3-headroom / overlap20 | 2 | 0.00 | 0.0 | 0s | 10.0 | 0.0 |

| Suite | Scene | Configuration / operator | Success | CORE | Median successful time | Peak visible |
|---|---|---|---:|---:|---:|---:|
| advanced-natural | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / unattended | 2/2 | 0/2 | 147.63s | 11 |
| advanced-natural | ConnectionsAdvanced | nominal-load1.5-db-goal8-busy / visible-defense | 2/2 | 0/2 | 147.63s | 11 |
| natural | Connections | nominal / unattended | 3/4 | 0/4 | 255.98s | 5 |
| natural | Connections | nominal / visible-defense | 3/4 | 0/4 | 255.98s | 5 |
| natural | Connections | nominal-load2-db-busy / unattended | 3/4 | 0/4 | 188.74s | 5 |
| natural | Connections | nominal-load2-db-busy / visible-defense | 3/4 | 0/4 | 188.74s | 5 |
| natural | ConnectionsAdvanced | nominal / unattended | 2/4 | 2/4 | 289.90s | 9 |
| natural | ConnectionsAdvanced | nominal / visible-defense | 3/4 | 1/4 | 299.29s | 9 |
| natural | ConnectionsAdvanced | nominal-load2-db-busy / unattended | 4/4 | 0/4 | 210.41s | 8 |
| natural | ConnectionsAdvanced | nominal-load2-db-busy / visible-defense | 4/4 | 0/4 | 210.41s | 8 |
