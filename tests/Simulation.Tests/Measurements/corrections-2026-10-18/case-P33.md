## P33

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^16`, k = 0..15 (16 seeds); 1 cycle(s) of 20000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 71 s over 14 arms (Original 5, All 6, ExactConstants 6, BridgeFrustum 5, WindowLowerEdge 5, VdokTotalOnCopy 6, ZdoksmallCoarseShare 5, GdokleftOnce 5, DoksdConsistent 5, CategoriesOnCopy 5, CategoriesLastRowRecomputed 5, PdoksmallFirstCell 5, AllButCategoriesOnCopy 5, AllButCategoriesLastRowRecomputed 5 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 139.560 | 1.306 | 5.225 |
| Dkarm43_cor(1) | 79.208 | 0.032 | 0.128 |
| Dkarm43_cor(2) | 80.001 | 0.034 | 0.136 |
| Dkarm10 | 58.273 | 0.017 | 0.067 |
| Zkarm | 0.508 | 2.200e-3 | 8.799e-3 |
| Zkarm_cor | 5.809e-3 | 5.088e-4 | 2.035e-3 |
| Dagg43 | 110.286 | 1.032 | 4.129 |
| Dagg43_cor(1) | 62.595 | 0.025 | 0.101 |
| Dagg43_cor(2) | 63.221 | 0.027 | 0.108 |
| da_coef | 0.790 | 2.867e-17 | 1.147e-16 |
| Dqmkm2 | 0.649 | 9.937e-5 | 3.975e-4 |
| Dok43sd | 75.450 | 3.669e-15 | 1.468e-14 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 6.000 | 0 | 0 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.091 +- 1.324 | +0.047 +- 0.057 | +0.047 +- 0.059 | +0.053 +- 0.041 | +5.397 +- 0.624* | +679.999 +- 13.094* | +0.082 +- 1.324 | +0.038 +- 0.057 | +0.038 +- 0.060 | -9.288e-3 +- 8.111e-15* | -4.370 +- 0.021* | +49.317 +- 1.087e-14* | abs 0 +- 0 | abs 0 +- 0 | +84.375 +- 1.042* | [9] +0.055 +- 0.117 |
| ExactConstants | +0.091 +- 1.324 | +0.047 +- 0.057 | +0.047 +- 0.059 | +0.053 +- 0.041 | +0.074 +- 0.628 | -1.209 +- 12.399 | +0.082 +- 1.324 | +0.038 +- 0.057 | +0.038 +- 0.060 | -9.288e-3 +- 8.111e-15* | +4.816e-3 +- 0.021 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | +1.042 +- 1.042 | [9] +0.055 +- 0.117 |
| BridgeFrustum | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 0.040 | -7.037 +- 0.615* | -1174.939 +- 12.640* | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 5.130e-15 | 0 +- 0.022 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.137 |
| WindowLowerEdge | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 0.040 | +11.552 +- 0.606* | +1735.785 +- 12.656* | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 5.130e-15 | -4.376 +- 0.022* | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.137 |
| VdokTotalOnCopy | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 0.040 | 0 +- 0.613 | 0 +- 12.386 | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 5.130e-15 | 0 +- 0.022 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.137 |
| ZdoksmallCoarseShare | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 0.040 | 0 +- 0.613 | 0 +- 12.386 | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 5.130e-15 | 0 +- 0.022 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.137 |
| GdokleftOnce | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 0.040 | 0 +- 0.613 | 0 +- 12.386 | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 5.130e-15 | 0 +- 0.022 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.137 |
| DoksdConsistent | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 0.040 | 0 +- 0.613 | 0 +- 12.386 | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 5.130e-15 | 0 +- 0.022 | +49.317 +- 1.087e-14* | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.137 |
| CategoriesOnCopy | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 0.040 | 0 +- 0.613 | 0 +- 12.386 | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 5.130e-15 | 0 +- 0.022 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | +84.375 +- 1.042* | [2] 0 +- 0.137 |
| CategoriesLastRowRecomputed | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 0.040 | 0 +- 0.613 | 0 +- 12.386 | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 5.130e-15 | 0 +- 0.022 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | -4.167 +- 1.863 | [2] 0 +- 0.137 |
| PdoksmallFirstCell | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 0.040 | 0 +- 0.613 | 0 +- 12.386 | 0 +- 1.324 | 0 +- 0.057 | 0 +- 0.060 | 0 +- 5.130e-15 | 0 +- 0.022 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.137 |
| AllButCategoriesOnCopy | +0.091 +- 1.324 | +0.047 +- 0.057 | +0.047 +- 0.059 | +0.053 +- 0.041 | +5.397 +- 0.624* | +679.999 +- 13.094* | +0.082 +- 1.324 | +0.038 +- 0.057 | +0.038 +- 0.060 | -9.288e-3 +- 8.111e-15* | -4.370 +- 0.021* | +49.317 +- 1.087e-14* | abs 0 +- 0 | abs 0 +- 0 | -2.083 +- 2.083 | [9] +0.055 +- 0.117 |
| AllButCategoriesLastRowRecomputed | +0.091 +- 1.324 | +0.047 +- 0.057 | +0.047 +- 0.059 | +0.053 +- 0.041 | +5.397 +- 0.624* | +679.999 +- 13.094* | +0.082 +- 1.324 | +0.038 +- 0.057 | +0.038 +- 0.060 | -9.288e-3 +- 8.111e-15* | -4.370 +- 0.021* | +49.317 +- 1.087e-14* | abs 0 +- 0 | abs 0 +- 0 | +84.375 +- 1.042* | [9] +0.055 +- 0.117 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.1 (+5.1) | +0.8 (+26.0) | +0.8 (+12.1) | +1.3 (+49.0) | +8.6 (+66.5) | +51.9 (+203.3) | +0.1 (+4.6) | +0.7 (+19.0) | +0.6 (+9.3) | -1145108022942.4 (-inf) | -203.4 (-1393.5) | +4535238157117550.0 (+inf) | 0.0 (0.0) | 0.0 (0.0) | +81.0 (+81.0) | +0.5 (+1.0) |
| ExactConstants | +0.1 (+5.1) | +0.8 (+26.0) | +0.8 (+12.1) | +1.3 (+49.0) | +0.1 (+1.4) | -0.1 (-1.5) | +0.1 (+4.6) | +0.7 (+19.0) | +0.6 (+9.3) | -1145108022942.4 (-inf) | +0.2 (+2.6) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +1.0 (+1.0) | +0.5 (+1.0) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -11.4 (-143.2) | -93.0 (-484.2) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +19.1 (+182.0) | +137.2 (+1471.1) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -197.8 (-1330.1) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +4535238157117550.0 (+inf) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +81.0 (+81.0) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -2.2 (-2.2) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| AllButCategoriesOnCopy | +0.1 (+5.1) | +0.8 (+26.0) | +0.8 (+12.1) | +1.3 (+49.0) | +8.6 (+66.5) | +51.9 (+203.3) | +0.1 (+4.6) | +0.7 (+19.0) | +0.6 (+9.3) | -1145108022942.4 (-inf) | -203.4 (-1393.5) | +4535238157117550.0 (+inf) | 0.0 (0.0) | 0.0 (0.0) | -1.0 (-1.0) | +0.5 (+1.0) |
| AllButCategoriesLastRowRecomputed | +0.1 (+5.1) | +0.8 (+26.0) | +0.8 (+12.1) | +1.3 (+49.0) | +8.6 (+66.5) | +51.9 (+203.3) | +0.1 (+4.6) | +0.7 (+19.0) | +0.6 (+9.3) | -1145108022942.4 (-inf) | -203.4 (-1393.5) | +4535238157117550.0 (+inf) | 0.0 (0.0) | 0.0 (0.0) | +81.0 (+81.0) | +0.5 (+1.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 0 / 1506 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-P33.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 120 / 1518 | fqmkm2 (87), dokkarm10 (4), fqdokkarm(1,:) (4), fqdokkarm(2,:) (4), fqdokkarm(3,:) (4), fqdokkarm(6,:) (4), fqdokkarm(4,:) (3), Zkarm_cor (2), and 7 more |
| ExactConstants | 1 / 1519 | da_coef (1) |
| BridgeFrustum | 3 / 1519 | Zkarm_cor (2), Zkarm (1) |
| WindowLowerEdge | 91 / 1518 | fqmkm2 (87), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 1519 | none |
| ZdoksmallCoarseShare | 0 / 1519 | none |
| GdokleftOnce | 0 / 1519 | none |
| DoksdConsistent | 1 / 1519 | Dok43sd (1) |
| CategoriesOnCopy | 27 / 1519 | dokkarm10 (4), fqdokkarm(1,:) (4), fqdokkarm(2,:) (4), fqdokkarm(3,:) (4), fqdokkarm(6,:) (4), fqdokkarm(4,:) (3), dokkarm43 (2), Dkarmcat (1), and 1 more |
| CategoriesLastRowRecomputed | 22 / 1519 | fqdokkarm(4,:) (20), dokkarm10 (1), dokkarm43 (1) |
| PdoksmallFirstCell | 0 / 1519 | none |
| AllButCategoriesOnCopy | 115 / 1518 | fqmkm2 (87), fqdokkarm(4,:) (20), Zkarm_cor (2), Dok43sd (1), Dqmkm2 (1), Zkarm (1), da_coef (1), dokkarm10 (1), and 1 more |
| AllButCategoriesLastRowRecomputed | 120 / 1518 | fqmkm2 (87), dokkarm10 (4), fqdokkarm(1,:) (4), fqdokkarm(2,:) (4), fqdokkarm(3,:) (4), fqdokkarm(6,:) (4), fqdokkarm(4,:) (3), Zkarm_cor (2), and 7 more |

