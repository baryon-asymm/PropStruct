## P312-Cycles3

P312 over three cycles, particles per cycle reduced from 5000 to 40. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^16`, k = 0..7 (8 seeds); 3 cycle(s) of 40 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 817 s over 16 arms (Original 56, All 50, ExactConstants 50, BridgeFrustum 50, WindowLowerEdge 51, VdokTotalOnCopy 56, ZdoksmallCoarseShare 48, GdokleftOnce 49, DoksdConsistent 48, CategoriesOnCopy 51, CategoriesLastRowRecomputed 49, CategoriesEmptyRowsMerged 56, PdoksmallFirstCell 49, AllButCategoriesOnCopy 50, AllButCategoriesLastRowRecomputed 52, AllButCategoriesEmptyRowsMerged 49 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 62.998 | 0.541 | 1.529 |
| Dkarm43_cor(1) | 62.474 | 0.756 | 2.137 |
| Dkarm43_cor(2) | 211.209 | 2.049 | 5.796 |
| Dkarm10 | 38.580 | 9.258e-3 | 0.026 |
| Zkarm | 0.889 | 1.130e-3 | 3.197e-3 |
| Zkarm_cor | 0.916 | 1.045e-3 | 2.956e-3 |
| Dagg43 | 40.673 | 0.349 | 0.988 |
| Dagg43_cor(1) | 40.335 | 0.487 | 1.378 |
| Dagg43_cor(2) | 136.360 | 1.320 | 3.732 |
| da_coef | 0.646 | 5.828e-5 | 1.648e-4 |
| Dqmkm2 | 0.809 | 2.738e-4 | 7.745e-4 |
| Dok43sd | 67.740 | 0 | 0 |
| fineoxy_fr | 0.226 | 3.598e-4 | 1.018e-3 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 2.000 | 0 | 0 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.046 +- 1.199 | +0.026 +- 1.699 | +0.202 +- 1.388 | 0 +- 0.034 | +2.550 +- 0.147* | -0.486 +- 0.135 | +0.034 +- 1.198 | +0.015 +- 1.697 | +0.191 +- 1.385 | -9.998e-3 +- 0.012 | -3.954 +- 0.052* | 0 +- 0 | -0.023 +- 0.211 | abs 0 +- 0 | +87.500 +- 8.183* | [3] -20.695 +- 0.189* |
| ExactConstants | +0.046 +- 1.199 | +0.026 +- 1.699 | +0.156 +- 1.388 | 0 +- 0.034 | +5.633e-3 +- 0.172 | +0.011 +- 0.151 | +0.034 +- 1.198 | +0.015 +- 1.697 | +0.144 +- 1.385 | -9.998e-3 +- 0.012 | +0.023 +- 0.045 | 0 +- 0 | -0.023 +- 0.211 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.236 |
| BridgeFrustum | 0 +- 1.214 | 0 +- 1.711 | 0 +- 1.372 | 0 +- 0.034 | -1.122 +- 0.188* | -0.823 +- 0.168* | 0 +- 1.214 | 0 +- 1.709 | 0 +- 1.368 | 0 +- 0.013 | 0 +- 0.048 | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.236 |
| WindowLowerEdge | 0 +- 1.214 | 0 +- 1.711 | +0.081 +- 1.354 | 0 +- 0.034 | +3.253 +- 0.145* | +2.388 +- 0.132* | 0 +- 1.214 | 0 +- 1.709 | +0.079 +- 1.351 | 0 +- 0.013 | -3.982 +- 0.056* | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.236 |
| VdokTotalOnCopy | 0 +- 1.214 | 0 +- 1.711 | 0 +- 1.372 | 0 +- 0.034 | 0 +- 0.180 | -2.960 +- 0.168* | 0 +- 1.214 | 0 +- 1.709 | 0 +- 1.368 | 0 +- 0.013 | 0 +- 0.048 | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.236 |
| ZdoksmallCoarseShare | 0 +- 1.214 | 0 +- 1.711 | +0.065 +- 1.363 | 0 +- 0.034 | 0 +- 0.180 | 0 +- 0.161 | 0 +- 1.214 | 0 +- 1.709 | +0.063 +- 1.359 | 0 +- 0.013 | 0 +- 0.048 | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | 0 +- 0 | [3] -20.695 +- 0.189* |
| GdokleftOnce | 0 +- 1.214 | 0 +- 1.711 | 0 +- 1.372 | 0 +- 0.034 | 0 +- 0.180 | 0 +- 0.161 | 0 +- 1.214 | 0 +- 1.709 | 0 +- 1.368 | 0 +- 0.013 | 0 +- 0.048 | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.236 |
| DoksdConsistent | 0 +- 1.214 | 0 +- 1.711 | 0 +- 1.372 | 0 +- 0.034 | 0 +- 0.180 | 0 +- 0.161 | 0 +- 1.214 | 0 +- 1.709 | 0 +- 1.368 | 0 +- 0.013 | 0 +- 0.048 | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.236 |
| CategoriesOnCopy | 0 +- 1.214 | 0 +- 1.711 | 0 +- 1.372 | 0 +- 0.034 | 0 +- 0.180 | 0 +- 0.161 | 0 +- 1.214 | 0 +- 1.709 | 0 +- 1.368 | 0 +- 0.013 | 0 +- 0.048 | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | +93.750 +- 6.250* | [2] 0 +- 0.236 |
| CategoriesLastRowRecomputed | 0 +- 1.214 | 0 +- 1.711 | 0 +- 1.372 | 0 +- 0.034 | 0 +- 0.180 | 0 +- 0.161 | 0 +- 1.214 | 0 +- 1.709 | 0 +- 1.368 | 0 +- 0.013 | 0 +- 0.048 | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | -50.000 +- 0* | [2] 0 +- 0.236 |
| CategoriesEmptyRowsMerged | 0 +- 1.214 | 0 +- 1.711 | 0 +- 1.372 | 0 +- 0.034 | 0 +- 0.180 | 0 +- 0.161 | 0 +- 1.214 | 0 +- 1.709 | 0 +- 1.368 | 0 +- 0.013 | 0 +- 0.048 | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.236 |
| PdoksmallFirstCell | 0 +- 1.214 | 0 +- 1.711 | 0 +- 1.372 | 0 +- 0.034 | 0 +- 0.180 | 0 +- 0.161 | 0 +- 1.214 | 0 +- 1.709 | 0 +- 1.368 | 0 +- 0.013 | 0 +- 0.048 | 0 +- 0 | 0 +- 0.225 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.236 |
| AllButCategoriesOnCopy | +0.046 +- 1.199 | +0.026 +- 1.699 | +0.202 +- 1.388 | 0 +- 0.034 | +2.550 +- 0.147* | -0.486 +- 0.135 | +0.034 +- 1.198 | +0.015 +- 1.697 | +0.191 +- 1.385 | -9.998e-3 +- 0.012 | -3.954 +- 0.052* | 0 +- 0 | -0.023 +- 0.211 | abs 0 +- 0 | -50.000 +- 0* | [3] -20.695 +- 0.189* |
| AllButCategoriesLastRowRecomputed | +0.046 +- 1.199 | +0.026 +- 1.699 | +0.202 +- 1.388 | 0 +- 0.034 | +2.550 +- 0.147* | -0.486 +- 0.135 | +0.034 +- 1.198 | +0.015 +- 1.697 | +0.191 +- 1.385 | -9.998e-3 +- 0.012 | -3.954 +- 0.052* | 0 +- 0 | -0.023 +- 0.211 | abs 0 +- 0 | +87.500 +- 8.183* | [3] -20.695 +- 0.189* |
| AllButCategoriesEmptyRowsMerged | +0.046 +- 1.199 | +0.026 +- 1.699 | +0.202 +- 1.388 | 0 +- 0.034 | +2.550 +- 0.147* | -0.486 +- 0.135 | +0.034 +- 1.198 | +0.015 +- 1.697 | +0.191 +- 1.385 | -9.998e-3 +- 0.012 | -3.954 +- 0.052* | 0 +- 0 | -0.023 +- 0.211 | abs 0 +- 0 | +87.500 +- 8.183* | [3] -20.695 +- 0.189* |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | 0.0 (+0.4) | 0.0 (+0.2) | +0.1 (+2.2) | 0.0 (0.0) | +17.3 (+34.2) | -3.6 (-6.3) | 0.0 (+0.3) | 0.0 (+0.1) | +0.1 (+2.1) | -0.8 (-6.7) | -76.7 (-258.8) | 0.0 (0.0) | -0.1 (-0.9) | 0.0 (0.0) | +10.7 (+10.7) | -109.3 (-209.3) |
| ExactConstants | 0.0 (+0.4) | 0.0 (+0.2) | +0.1 (+1.6) | 0.0 (0.0) | 0.0 (+0.1) | +0.1 (+0.2) | 0.0 (+0.3) | 0.0 (+0.1) | +0.1 (+1.4) | -0.8 (-6.7) | +0.5 (+2.0) | 0.0 (0.0) | -0.1 (-0.9) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -6.0 (-50.9) | -4.9 (-49.5) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +0.1 (+1.8) | 0.0 (0.0) | +22.5 (+40.5) | +18.2 (+37.9) | 0.0 (0.0) | 0.0 (0.0) | +0.1 (+1.8) | 0.0 (0.0) | -71.1 (-227.7) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -17.6 (-113.4) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (+0.7) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (+0.7) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -109.3 (-209.3) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +15.0 (+15.0) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -inf (-inf) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| AllButCategoriesOnCopy | 0.0 (+0.4) | 0.0 (+0.2) | +0.1 (+2.2) | 0.0 (0.0) | +17.3 (+34.2) | -3.6 (-6.3) | 0.0 (+0.3) | 0.0 (+0.1) | +0.1 (+2.1) | -0.8 (-6.7) | -76.7 (-258.8) | 0.0 (0.0) | -0.1 (-0.9) | 0.0 (0.0) | -inf (-inf) | -109.3 (-209.3) |
| AllButCategoriesLastRowRecomputed | 0.0 (+0.4) | 0.0 (+0.2) | +0.1 (+2.2) | 0.0 (0.0) | +17.3 (+34.2) | -3.6 (-6.3) | 0.0 (+0.3) | 0.0 (+0.1) | +0.1 (+2.1) | -0.8 (-6.7) | -76.7 (-258.8) | 0.0 (0.0) | -0.1 (-0.9) | 0.0 (0.0) | +10.7 (+10.7) | -109.3 (-209.3) |
| AllButCategoriesEmptyRowsMerged | 0.0 (+0.4) | 0.0 (+0.2) | +0.1 (+2.2) | 0.0 (0.0) | +17.3 (+34.2) | -3.6 (-6.3) | 0.0 (+0.3) | 0.0 (+0.1) | +0.1 (+2.1) | -0.8 (-6.7) | -76.7 (-258.8) | 0.0 (0.0) | -0.1 (-0.9) | 0.0 (0.0) | +10.7 (+10.7) | -109.3 (-209.3) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (4) against odd seeds (4): 0 / 1381 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-P312-Cycles3.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 69 / 1401 | fqmkm2 (45), pdoksmall (17), fqdokkarm(2,:) (2), Dkarmcat (1), Dqmkm2 (1), Zkarm (1), dokkarm10 (1), dokkarm43 (1) |
| ExactConstants | 0 / 1402 | none |
| BridgeFrustum | 0 / 1402 | none |
| WindowLowerEdge | 50 / 1401 | fqmkm2 (43), epszkarm_n (3), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 3 / 1402 | Zkarm_cor (2), epszkarm_n (1) |
| ZdoksmallCoarseShare | 17 / 1402 | pdoksmall (17) |
| GdokleftOnce | 0 / 1402 | none |
| DoksdConsistent | 0 / 1402 | none |
| CategoriesOnCopy | 5 / 1402 | fqdokkarm(2,:) (2), Dkarmcat (1), dokkarm10 (1), dokkarm43 (1) |
| CategoriesLastRowRecomputed | 5 / 1379 | fqdokkarm(1,:) (3), Dkarmcat (1), dokkarm43 (1) |
| CategoriesEmptyRowsMerged | 0 / 1402 | none |
| PdoksmallFirstCell | 0 / 1402 | none |
| AllButCategoriesOnCopy | 70 / 1378 | fqmkm2 (45), pdoksmall (17), fqdokkarm(1,:) (4), Dkarmcat (1), Dqmkm2 (1), Zkarm (1), dokkarm43 (1) |
| AllButCategoriesLastRowRecomputed | 69 / 1401 | fqmkm2 (45), pdoksmall (17), fqdokkarm(2,:) (2), Dkarmcat (1), Dqmkm2 (1), Zkarm (1), dokkarm10 (1), dokkarm43 (1) |
| AllButCategoriesEmptyRowsMerged | 69 / 1401 | fqmkm2 (45), pdoksmall (17), fqdokkarm(2,:) (2), Dkarmcat (1), Dqmkm2 (1), Zkarm (1), dokkarm10 (1), dokkarm43 (1) |

