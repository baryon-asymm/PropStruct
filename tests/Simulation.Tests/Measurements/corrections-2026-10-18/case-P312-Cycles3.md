## P312-Cycles3

P312 over three cycles, particles per cycle reduced from 5000 to 40. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..7 (8 seeds); 3 cycle(s) of 40 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 700 s over 14 arms (Original 56, All 47, ExactConstants 50, BridgeFrustum 48, Fmkarm2OnAcceptance 56, WindowLowerEdge 52, VdokTotalOnCopy 49, ZdoksmallCoarseShare 48, GdokleftOnce 47, DoksdConsistent 49, CategoriesOnCopy 53, CategoriesLastRowRecomputed 49, CategoriesEmptyRowsMerged 49, PdoksmallFirstCell 47 s).

Failed runs: none.

Builds behind the runs: 2 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 62.618 | 0.353 | 0.998 |
| Dkarm10 | 38.569 | 9.342e-3 | 0.026 |
| Zkarm | 0.889 | 1.007e-3 | 2.848e-3 |
| Zkarm_cor | 0.916 | 9.814e-4 | 2.776e-3 |
| Dagg43 | 40.430 | 0.229 | 0.646 |
| da_coef | 0.646 | 6.761e-5 | 1.912e-4 |
| Dqmkm2 | 0.809 | 2.375e-4 | 6.719e-4 |
| Dok43sd | 67.740 | 0 | 0 |
| fineoxy_fr | 0.226 | 4.173e-4 | 1.180e-3 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 2.000 | 0 | 0 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.238 +- 0.790 | +6.482e-3 +- 0.034 | +2.499 +- 0.135* | -0.518 +- 0.129 | +0.235 +- 0.793 | -0.011 +- 0.014 | -3.921 +- 0.053* | 0 +- 0 | -9.078e-3 +- 0.247 | abs 0 +- 0 | +87.500 +- 8.183* | [3] -20.636 +- 0.210* |
| ExactConstants | +0.238 +- 0.790 | +6.482e-3 +- 0.034 | -0.097 +- 0.155 | -0.068 +- 0.145 | +0.235 +- 0.793 | -0.011 +- 0.014 | +0.019 +- 0.043 | 0 +- 0 | -9.078e-3 +- 0.247 | abs 0 +- 0 | 0 +- 0 | [3] +0.038 +- 0.251 |
| BridgeFrustum | 0 +- 0.797 | 0 +- 0.034 | -1.123 +- 0.170* | -0.824 +- 0.159* | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.239 |
| Fmkarm2OnAcceptance | 0 +- 0.797 | 0 +- 0.034 | 0 +- 0.160 | 0 +- 0.151 | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.239 |
| WindowLowerEdge | 0 +- 0.797 | 0 +- 0.034 | +3.269 +- 0.130* | +2.400 +- 0.123* | 0 +- 0.799 | 0 +- 0.015 | -3.986 +- 0.052* | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.239 |
| VdokTotalOnCopy | 0 +- 0.797 | 0 +- 0.034 | 0 +- 0.160 | -2.944 +- 0.153* | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.239 |
| ZdoksmallCoarseShare | 0 +- 0.797 | 0 +- 0.034 | 0 +- 0.160 | 0 +- 0.151 | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | 0 +- 0 | [3] -20.636 +- 0.210* |
| GdokleftOnce | 0 +- 0.797 | 0 +- 0.034 | 0 +- 0.160 | 0 +- 0.151 | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.239 |
| DoksdConsistent | 0 +- 0.797 | 0 +- 0.034 | 0 +- 0.160 | 0 +- 0.151 | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.239 |
| CategoriesOnCopy | 0 +- 0.797 | 0 +- 0.034 | 0 +- 0.160 | 0 +- 0.151 | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | +93.750 +- 6.250* | [2] 0 +- 0.239 |
| CategoriesLastRowRecomputed | 0 +- 0.797 | 0 +- 0.034 | 0 +- 0.160 | 0 +- 0.151 | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | -50.000 +- 0* | [2] 0 +- 0.239 |
| CategoriesEmptyRowsMerged | 0 +- 0.797 | 0 +- 0.034 | 0 +- 0.160 | 0 +- 0.151 | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.239 |
| PdoksmallFirstCell | 0 +- 0.797 | 0 +- 0.034 | 0 +- 0.160 | 0 +- 0.151 | 0 +- 0.799 | 0 +- 0.015 | 0 +- 0.042 | 0 +- 0 | 0 +- 0.261 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.239 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.3 (+1.5) | +0.2 (+1.5) | +18.5 (+34.0) | -4.0 (-6.8) | +0.3 (+1.5) | -0.8 (-7.2) | -74.0 (-193.2) | 0.0 (0.0) | 0.0 (-0.3) | 0.0 (0.0) | +10.7 (+10.7) | -98.3 (-230.9) |
| ExactConstants | +0.3 (+1.5) | +0.2 (+1.5) | -0.6 (-2.5) | -0.5 (-2.0) | +0.3 (+1.5) | -0.8 (-7.2) | +0.4 (+2.6) | 0.0 (0.0) | 0.0 (-0.3) | 0.0 (0.0) | 0.0 (0.0) | +0.2 (+1.0) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | -6.6 (-43.0) | -5.2 (-42.8) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| Fmkarm2OnAcceptance | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +25.1 (+40.8) | +19.5 (+37.6) | 0.0 (0.0) | 0.0 (0.0) | -76.0 (-257.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -19.2 (-129.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -98.3 (-230.9) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +15.0 (+15.0) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -inf (-inf) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (4) against odd seeds (4): 0 / 1374 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-P312-Cycles3.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 65 / 1396 | fqmkm2 (41), pdoksmall (17), Dkarmcat (1), Dqmkm2 (1), Zkarm (1), dokkarm10 (1), dokkarm43 (1), epszkarm_n (1), and 1 more |
| ExactConstants | 0 / 1397 | none |
| BridgeFrustum | 0 / 1397 | none |
| Fmkarm2OnAcceptance | 0 / 1397 | none |
| WindowLowerEdge | 53 / 1396 | fqmkm2 (46), epszkarm_n (3), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 4 / 1397 | Zkarm_cor (2), epszkarm_n (2) |
| ZdoksmallCoarseShare | 17 / 1397 | pdoksmall (17) |
| GdokleftOnce | 0 / 1397 | none |
| DoksdConsistent | 0 / 1397 | none |
| CategoriesOnCopy | 4 / 1397 | Dkarmcat (1), dokkarm10 (1), dokkarm43 (1), fqdokkarm(2,:) (1) |
| CategoriesLastRowRecomputed | 5 / 1374 | fqdokkarm(1,:) (3), Dkarmcat (1), dokkarm43 (1) |
| CategoriesEmptyRowsMerged | 0 / 1397 | none |
| PdoksmallFirstCell | 0 / 1397 | none |

