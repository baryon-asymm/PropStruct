## P312-defaults

P312 at its defaults, particles per cycle reduced from 5000 to 100. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^16`, k = 0..7 (8 seeds); 1 cycle(s) of 100 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 882 s over 16 arms (Original 59, All 60, ExactConstants 51, BridgeFrustum 49, WindowLowerEdge 51, VdokTotalOnCopy 61, ZdoksmallCoarseShare 52, GdokleftOnce 62, DoksdConsistent 51, CategoriesOnCopy 63, CategoriesLastRowRecomputed 52, CategoriesEmptyRowsMerged 59, PdoksmallFirstCell 50, AllButCategoriesOnCopy 50, AllButCategoriesLastRowRecomputed 50, AllButCategoriesEmptyRowsMerged 62 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 63.143 | 0.682 | 1.930 |
| Dkarm43_cor(1) | 62.723 | 1.092 | 3.090 |
| Dkarm43_cor(2) | 211.830 | 2.228 | 6.303 |
| Dkarm10 | 38.580 | 9.063e-3 | 0.026 |
| Zkarm | 0.889 | 1.430e-3 | 4.045e-3 |
| Zkarm_cor | 0.889 | 1.430e-3 | 4.045e-3 |
| Dagg43 | 40.768 | 0.439 | 1.242 |
| Dagg43_cor(1) | 40.495 | 0.705 | 1.995 |
| Dagg43_cor(2) | 136.769 | 1.442 | 4.079 |
| da_coef | 0.646 | 6.078e-5 | 1.719e-4 |
| Dqmkm2 | 0.809 | 3.075e-4 | 8.697e-4 |
| Dok43sd | 67.740 | 0 | 0 |
| fineoxy_fr | 0.226 | 3.751e-4 | 1.061e-3 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 3.000 | 0 | 0 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.053 +- 1.530 | +0.076 +- 2.485 | -0.058 +- 1.504 | -3.240e-3 +- 0.033 | +2.431 +- 0.202* | +2.431 +- 0.202* | +0.046 +- 1.526 | +0.068 +- 2.485 | -0.069 +- 1.507 | -0.010 +- 0.013 | -3.959 +- 0.059* | 0 +- 0 | -0.022 +- 0.233 | abs 0 +- 0 | +66.667 +- 0* | [3] -20.786 +- 0.204* |
| ExactConstants | +0.053 +- 1.530 | +0.076 +- 2.485 | -0.161 +- 1.486 | -3.240e-3 +- 0.033 | +4.380e-3 +- 0.221 | +4.380e-3 +- 0.221 | +0.046 +- 1.526 | +0.068 +- 2.485 | -0.169 +- 1.490 | -0.010 +- 0.013 | -1.372e-14 +- 0.054 | 0 +- 0 | -0.022 +- 0.233 | abs 0 +- 0 | 0 +- 0 | [3] -0.076 +- 0.247 |
| BridgeFrustum | 0 +- 1.528 | 0 +- 2.463 | 0 +- 1.488 | 0 +- 0.033 | -1.097 +- 0.222* | -1.097 +- 0.222* | 0 +- 1.523 | 0 +- 2.464 | 0 +- 1.491 | 0 +- 0.013 | 0 +- 0.054 | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.276 |
| WindowLowerEdge | 0 +- 1.528 | 0 +- 2.463 | -0.137 +- 1.512 | 0 +- 0.033 | +3.287 +- 0.202* | +3.287 +- 0.202* | 0 +- 1.523 | 0 +- 2.464 | -0.136 +- 1.515 | 0 +- 0.013 | -3.974 +- 0.058* | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.276 |
| VdokTotalOnCopy | 0 +- 1.528 | 0 +- 2.463 | 0 +- 1.488 | 0 +- 0.033 | 0 +- 0.228 | 0 +- 0.228 | 0 +- 1.523 | 0 +- 2.464 | 0 +- 1.491 | 0 +- 0.013 | 0 +- 0.054 | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.276 |
| ZdoksmallCoarseShare | 0 +- 1.528 | 0 +- 2.463 | +0.082 +- 1.506 | 0 +- 0.033 | 0 +- 0.228 | 0 +- 0.228 | 0 +- 1.523 | 0 +- 2.464 | +0.080 +- 1.510 | 0 +- 0.013 | 0 +- 0.054 | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | 0 +- 0 | [3] -20.786 +- 0.204* |
| GdokleftOnce | 0 +- 1.528 | 0 +- 2.463 | 0 +- 1.488 | 0 +- 0.033 | 0 +- 0.228 | 0 +- 0.228 | 0 +- 1.523 | 0 +- 2.464 | 0 +- 1.491 | 0 +- 0.013 | 0 +- 0.054 | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.276 |
| DoksdConsistent | 0 +- 1.528 | 0 +- 2.463 | 0 +- 1.488 | 0 +- 0.033 | 0 +- 0.228 | 0 +- 0.228 | 0 +- 1.523 | 0 +- 2.464 | 0 +- 1.491 | 0 +- 0.013 | 0 +- 0.054 | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.276 |
| CategoriesOnCopy | 0 +- 1.528 | 0 +- 2.463 | 0 +- 1.488 | 0 +- 0.033 | 0 +- 0.228 | 0 +- 0.228 | 0 +- 1.523 | 0 +- 2.464 | 0 +- 1.491 | 0 +- 0.013 | 0 +- 0.054 | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | +66.667 +- 0* | [2] 0 +- 0.276 |
| CategoriesLastRowRecomputed | 0 +- 1.528 | 0 +- 2.463 | 0 +- 1.488 | 0 +- 0.033 | 0 +- 0.228 | 0 +- 0.228 | 0 +- 1.523 | 0 +- 2.464 | 0 +- 1.491 | 0 +- 0.013 | 0 +- 0.054 | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | -33.333 +- 0* | [2] 0 +- 0.276 |
| CategoriesEmptyRowsMerged | 0 +- 1.528 | 0 +- 2.463 | 0 +- 1.488 | 0 +- 0.033 | 0 +- 0.228 | 0 +- 0.228 | 0 +- 1.523 | 0 +- 2.464 | 0 +- 1.491 | 0 +- 0.013 | 0 +- 0.054 | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.276 |
| PdoksmallFirstCell | 0 +- 1.528 | 0 +- 2.463 | 0 +- 1.488 | 0 +- 0.033 | 0 +- 0.228 | 0 +- 0.228 | 0 +- 1.523 | 0 +- 2.464 | 0 +- 1.491 | 0 +- 0.013 | 0 +- 0.054 | 0 +- 0 | 0 +- 0.235 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.276 |
| AllButCategoriesOnCopy | +0.053 +- 1.530 | +0.076 +- 2.485 | -0.058 +- 1.504 | -3.240e-3 +- 0.033 | +2.431 +- 0.202* | +2.431 +- 0.202* | +0.046 +- 1.526 | +0.068 +- 2.485 | -0.069 +- 1.507 | -0.010 +- 0.013 | -3.959 +- 0.059* | 0 +- 0 | -0.022 +- 0.233 | abs 0 +- 0 | -33.333 +- 0* | [3] -20.786 +- 0.204* |
| AllButCategoriesLastRowRecomputed | +0.053 +- 1.530 | +0.076 +- 2.485 | -0.058 +- 1.504 | -3.240e-3 +- 0.033 | +2.431 +- 0.202* | +2.431 +- 0.202* | +0.046 +- 1.526 | +0.068 +- 2.485 | -0.069 +- 1.507 | -0.010 +- 0.013 | -3.959 +- 0.059* | 0 +- 0 | -0.022 +- 0.233 | abs 0 +- 0 | +66.667 +- 0* | [3] -20.786 +- 0.204* |
| AllButCategoriesEmptyRowsMerged | +0.053 +- 1.530 | +0.076 +- 2.485 | -0.058 +- 1.504 | -3.240e-3 +- 0.033 | +2.431 +- 0.202* | +2.431 +- 0.202* | +0.046 +- 1.526 | +0.068 +- 2.485 | -0.069 +- 1.507 | -0.010 +- 0.013 | -3.959 +- 0.059* | 0 +- 0 | -0.022 +- 0.233 | abs 0 +- 0 | +66.667 +- 0* | [3] -20.786 +- 0.204* |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | 0.0 (+0.3) | 0.0 (+0.3) | 0.0 (-1.0) | -0.1 (-0.6) | +12.1 (+33.0) | +12.1 (+33.0) | 0.0 (+0.3) | 0.0 (+0.2) | 0.0 (-1.2) | -0.8 (-12.0) | -66.6 (-176.0) | 0.0 (0.0) | -0.1 (-1.5) | 0.0 (0.0) | +inf (+inf) | -101.9 (-275.0) |
| ExactConstants | 0.0 (+0.3) | 0.0 (+0.3) | -0.1 (-2.2) | -0.1 (-0.6) | 0.0 (+0.1) | 0.0 (+0.1) | 0.0 (+0.3) | 0.0 (+0.2) | -0.1 (-2.3) | -0.8 (-12.0) | 0.0 (0.0) | 0.0 (0.0) | -0.1 (-1.5) | 0.0 (0.0) | 0.0 (0.0) | -0.3 (-1.5) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -4.9 (-41.1) | -4.9 (-41.1) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | -0.1 (-2.0) | 0.0 (0.0) | +16.3 (+70.8) | +16.3 (+70.8) | 0.0 (0.0) | 0.0 (0.0) | -0.1 (-2.0) | 0.0 (0.0) | -68.4 (-219.6) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | +0.1 (+1.4) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.1 (+1.4) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -101.9 (-275.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +inf (+inf) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -inf (-inf) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| AllButCategoriesOnCopy | 0.0 (+0.3) | 0.0 (+0.3) | 0.0 (-1.0) | -0.1 (-0.6) | +12.1 (+33.0) | +12.1 (+33.0) | 0.0 (+0.3) | 0.0 (+0.2) | 0.0 (-1.2) | -0.8 (-12.0) | -66.6 (-176.0) | 0.0 (0.0) | -0.1 (-1.5) | 0.0 (0.0) | -inf (-inf) | -101.9 (-275.0) |
| AllButCategoriesLastRowRecomputed | 0.0 (+0.3) | 0.0 (+0.3) | 0.0 (-1.0) | -0.1 (-0.6) | +12.1 (+33.0) | +12.1 (+33.0) | 0.0 (+0.3) | 0.0 (+0.2) | 0.0 (-1.2) | -0.8 (-12.0) | -66.6 (-176.0) | 0.0 (0.0) | -0.1 (-1.5) | 0.0 (0.0) | +inf (+inf) | -101.9 (-275.0) |
| AllButCategoriesEmptyRowsMerged | 0.0 (+0.3) | 0.0 (+0.3) | 0.0 (-1.0) | -0.1 (-0.6) | +12.1 (+33.0) | +12.1 (+33.0) | 0.0 (+0.3) | 0.0 (+0.2) | 0.0 (-1.2) | -0.8 (-12.0) | -66.6 (-176.0) | 0.0 (0.0) | -0.1 (-1.5) | 0.0 (0.0) | +inf (+inf) | -101.9 (-275.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (4) against odd seeds (4): 0 / 1385 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-P312-defaults.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 68 / 1397 | fqmkm2 (41), pdoksmall (17), fqdokkarm(3,:) (4), Dkarmcat (1), Dqmkm2 (1), Zkarm (1), Zkarm_cor (1), dokkarm10 (1), and 1 more |
| ExactConstants | 0 / 1398 | none |
| BridgeFrustum | 0 / 1398 | none |
| WindowLowerEdge | 44 / 1397 | fqmkm2 (40), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 1398 | none |
| ZdoksmallCoarseShare | 17 / 1398 | pdoksmall (17) |
| GdokleftOnce | 0 / 1398 | none |
| DoksdConsistent | 0 / 1398 | none |
| CategoriesOnCopy | 7 / 1398 | fqdokkarm(3,:) (4), Dkarmcat (1), dokkarm10 (1), dokkarm43 (1) |
| CategoriesLastRowRecomputed | 14 / 1375 | fqdokkarm(2,:) (5), fqdokkarm(1,:) (3), Dkarmcat (2), dokkarm10 (2), dokkarm43 (2) |
| CategoriesEmptyRowsMerged | 0 / 1398 | none |
| PdoksmallFirstCell | 0 / 1398 | none |
| AllButCategoriesOnCopy | 75 / 1374 | fqmkm2 (41), pdoksmall (17), fqdokkarm(2,:) (7), Dkarmcat (2), dokkarm10 (2), dokkarm43 (2), Dqmkm2 (1), Zkarm (1), and 2 more |
| AllButCategoriesLastRowRecomputed | 68 / 1397 | fqmkm2 (41), pdoksmall (17), fqdokkarm(3,:) (4), Dkarmcat (1), Dqmkm2 (1), Zkarm (1), Zkarm_cor (1), dokkarm10 (1), and 1 more |
| AllButCategoriesEmptyRowsMerged | 68 / 1397 | fqmkm2 (41), pdoksmall (17), fqdokkarm(3,:) (4), Dkarmcat (1), Dqmkm2 (1), Zkarm (1), Zkarm_cor (1), dokkarm10 (1), and 1 more |

