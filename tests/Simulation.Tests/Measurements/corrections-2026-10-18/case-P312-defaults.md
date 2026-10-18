## P312-defaults

P312 at its defaults, particles per cycle reduced from 5000 to 100. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..7 (8 seeds); 1 cycle(s) of 100 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 965 s over 14 arms (Original 53, All 84, ExactConstants 62, BridgeFrustum 81, Fmkarm2OnAcceptance 84, WindowLowerEdge 80, VdokTotalOnCopy 63, ZdoksmallCoarseShare 62, GdokleftOnce 50, DoksdConsistent 63, CategoriesOnCopy 50, CategoriesLastRowRecomputed 80, CategoriesEmptyRowsMerged 82, PdoksmallFirstCell 72 s).

Failed runs: none.

Builds behind the runs: 2 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 62.864 | 0.352 | 0.996 |
| Dkarm10 | 38.570 | 8.452e-3 | 0.024 |
| Zkarm | 0.889 | 8.628e-4 | 2.440e-3 |
| Zkarm_cor | 0.889 | 8.628e-4 | 2.440e-3 |
| Dagg43 | 40.591 | 0.228 | 0.645 |
| da_coef | 0.646 | 6.427e-5 | 1.818e-4 |
| Dqmkm2 | 0.809 | 2.777e-4 | 7.856e-4 |
| Dok43sd | 67.740 | 0 | 0 |
| fineoxy_fr | 0.226 | 3.967e-4 | 1.122e-3 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 3.000 | 0 | 0 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.249 +- 0.814 | 0 +- 0.031 | +2.510 +- 0.140* | +2.510 +- 0.140* | +0.237 +- 0.818 | -0.011 +- 0.014 | -3.923 +- 0.053* | 0 +- 0 | -2.336e-3 +- 0.241 | abs 0 +- 0 | +66.667 +- 0* | [3] -20.704 +- 0.216* |
| ExactConstants | +0.249 +- 0.814 | 0 +- 0.031 | +0.028 +- 0.159 | +0.028 +- 0.159 | +0.237 +- 0.818 | -0.011 +- 0.014 | +0.012 +- 0.049 | 0 +- 0 | -2.336e-3 +- 0.241 | abs 0 +- 0 | 0 +- 0 | [2] -0.064 +- 0.242 |
| BridgeFrustum | 0 +- 0.792 | 0 +- 0.031 | -1.045 +- 0.129* | -1.045 +- 0.129* | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.256 |
| Fmkarm2OnAcceptance | 0 +- 0.792 | 0 +- 0.031 | 0 +- 0.137 | 0 +- 0.137 | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.256 |
| WindowLowerEdge | 0 +- 0.792 | 0 +- 0.031 | +3.297 +- 0.130* | +3.297 +- 0.130* | 0 +- 0.795 | 0 +- 0.014 | -3.956 +- 0.049* | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.256 |
| VdokTotalOnCopy | 0 +- 0.792 | 0 +- 0.031 | 0 +- 0.137 | 0 +- 0.137 | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.256 |
| ZdoksmallCoarseShare | 0 +- 0.792 | 0 +- 0.031 | 0 +- 0.137 | 0 +- 0.137 | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | 0 +- 0 | [3] -20.704 +- 0.216* |
| GdokleftOnce | 0 +- 0.792 | 0 +- 0.031 | 0 +- 0.137 | 0 +- 0.137 | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.256 |
| DoksdConsistent | 0 +- 0.792 | 0 +- 0.031 | 0 +- 0.137 | 0 +- 0.137 | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.256 |
| CategoriesOnCopy | 0 +- 0.792 | 0 +- 0.031 | 0 +- 0.137 | 0 +- 0.137 | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | +66.667 +- 0* | [2] 0 +- 0.256 |
| CategoriesLastRowRecomputed | 0 +- 0.792 | 0 +- 0.031 | 0 +- 0.137 | 0 +- 0.137 | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | -33.333 +- 0* | [2] 0 +- 0.256 |
| CategoriesEmptyRowsMerged | 0 +- 0.792 | 0 +- 0.031 | 0 +- 0.137 | 0 +- 0.137 | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.256 |
| PdoksmallFirstCell | 0 +- 0.792 | 0 +- 0.031 | 0 +- 0.137 | 0 +- 0.137 | 0 +- 0.795 | 0 +- 0.014 | 0 +- 0.049 | 0 +- 0 | 0 +- 0.249 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.256 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.3 (+1.5) | 0.0 (0.0) | +17.9 (+40.6) | +17.9 (+40.6) | +0.3 (+1.5) | -0.8 (-15.0) | -74.7 (-130.0) | 0.0 (0.0) | 0.0 (-0.2) | 0.0 (0.0) | +inf (+inf) | -96.1 (-211.1) |
| ExactConstants | +0.3 (+1.5) | 0.0 (0.0) | +0.2 (+0.3) | +0.2 (+0.3) | +0.3 (+1.5) | -0.8 (-15.0) | +0.3 (+1.0) | 0.0 (0.0) | 0.0 (-0.2) | 0.0 (0.0) | 0.0 (0.0) | -0.3 (-1.0) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | -8.1 (-50.7) | -8.1 (-50.7) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| Fmkarm2OnAcceptance | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +25.4 (+51.7) | +25.4 (+51.7) | 0.0 (0.0) | 0.0 (0.0) | -81.5 (-306.8) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -96.1 (-211.1) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +inf (+inf) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -inf (-inf) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (4) against odd seeds (4): 0 / 1384 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-P312-defaults.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 65 / 1398 | fqmkm2 (39), pdoksmall (17), Zkarm_cor (2), fqdokkarm(3,:) (2), Dkarmcat (1), Dqmkm2 (1), Zkarm (1), dokkarm10 (1), and 1 more |
| ExactConstants | 0 / 1399 | none |
| BridgeFrustum | 0 / 1399 | none |
| Fmkarm2OnAcceptance | 0 / 1399 | none |
| WindowLowerEdge | 47 / 1398 | fqmkm2 (43), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 1399 | none |
| ZdoksmallCoarseShare | 17 / 1399 | pdoksmall (17) |
| GdokleftOnce | 0 / 1399 | none |
| DoksdConsistent | 0 / 1399 | none |
| CategoriesOnCopy | 5 / 1399 | fqdokkarm(3,:) (2), Dkarmcat (1), dokkarm10 (1), dokkarm43 (1) |
| CategoriesLastRowRecomputed | 13 / 1376 | fqdokkarm(2,:) (6), Dkarmcat (2), dokkarm43 (2), fqdokkarm(1,:) (2), dokkarm10 (1) |
| CategoriesEmptyRowsMerged | 0 / 1399 | none |
| PdoksmallFirstCell | 0 / 1399 | none |

