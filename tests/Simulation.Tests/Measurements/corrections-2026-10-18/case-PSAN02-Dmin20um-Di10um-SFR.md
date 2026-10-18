## PSAN02-Dmin20um-Di10um-SFR

PSAN02 read with its pocket-forming fractions, Dmin 20 um, Di 10 um, particles per cycle as read. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..7 (8 seeds); 1 cycle(s) of 5000 particles; `Dmin` 20 um, `Di` 10 um, `Dj` 10 um.

Work: 857 s over 14 arms (Original 60, All 59, ExactConstants 59, BridgeFrustum 58, Fmkarm2OnAcceptance 61, WindowLowerEdge 61, VdokTotalOnCopy 60, ZdoksmallCoarseShare 61, GdokleftOnce 61, DoksdConsistent 64, CategoriesOnCopy 63, CategoriesLastRowRecomputed 63, CategoriesEmptyRowsMerged 64, PdoksmallFirstCell 61 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 569.966 | 0.320 | 0.905 |
| Dkarm10 | 432.044 | 0.211 | 0.596 |
| Zkarm | 0.845 | 1.370e-4 | 3.876e-4 |
| Zkarm_cor | 0.845 | 1.370e-4 | 3.876e-4 |
| Dagg43 | 341.504 | 0.192 | 0.542 |
| da_coef | 0.599 | 4.219e-7 | 1.193e-6 |
| Dqmkm2 | 0.706 | 2.413e-4 | 6.824e-4 |
| Dok43sd | 75.820 | 0 | 0 |
| fineoxy_fr | 0.724 | 2.706e-6 | 7.653e-6 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 80.000 | 0.327 | 0.926 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.056 +- 0.080 | +0.062 +- 0.071 | -2.859 +- 0.030* | -2.859 +- 0.030* | +0.506 +- 0.081* | +0.449 +- 9.888e-5* | -0.630 +- 0.053* | 0 +- 0 | -2.420 +- 5.208e-4* | abs 0 +- 0 | -3.906 +- 0.551* | [50] -59.952 +- 0.014* |
| ExactConstants | +0.056 +- 0.080 | +0.062 +- 0.071 | -3.060e-3 +- 0.024 | -3.060e-3 +- 0.024 | +0.044 +- 0.080 | -0.012 +- 9.790e-5* | +1.770e-3 +- 0.051 | 0 +- 0 | +1.899e-5 +- 5.208e-4 | abs 0 +- 0 | +0.156 +- 0.551 | [40] +0.032 +- 0.063 |
| BridgeFrustum | 0 +- 0.079 | 0 +- 0.069 | -1.586 +- 0.027* | -1.586 +- 0.027* | 0 +- 0.079 | 0 +- 9.958e-5 | 0 +- 0.048 | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | 0 +- 0.579 | [32] 0 +- 0.350 |
| Fmkarm2OnAcceptance | 0 +- 0.079 | 0 +- 0.069 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.079 | 0 +- 9.958e-5 | 0 +- 0.048 | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | 0 +- 0.579 | [32] 0 +- 0.350 |
| WindowLowerEdge | 0 +- 0.079 | 0 +- 0.069 | +0.249 +- 0.023* | +0.249 +- 0.023* | 0 +- 0.079 | 0 +- 9.958e-5 | -0.635 +- 0.050* | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | 0 +- 0.579 | [32] 0 +- 0.350 |
| VdokTotalOnCopy | 0 +- 0.079 | 0 +- 0.069 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.079 | 0 +- 9.958e-5 | 0 +- 0.048 | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | 0 +- 0.579 | [32] 0 +- 0.350 |
| ZdoksmallCoarseShare | 0 +- 0.079 | 0 +- 0.069 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.079 | 0 +- 9.958e-5 | 0 +- 0.048 | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | 0 +- 0.579 | [50] -62.452 +- 9.542e-3* |
| GdokleftOnce | 0 +- 0.079 | 0 +- 0.069 | -1.437 +- 0.024* | -1.437 +- 0.024* | +0.462 +- 0.079* | +0.461 +- 1.005e-4* | 0 +- 0.048 | 0 +- 0 | -2.420 +- 5.286e-4* | abs 0 +- 0 | 0 +- 0.579 | [49] +0.787 +- 6.608e-15* |
| DoksdConsistent | 0 +- 0.079 | 0 +- 0.069 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.079 | 0 +- 9.958e-5 | 0 +- 0.048 | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | 0 +- 0.579 | [32] 0 +- 0.350 |
| CategoriesOnCopy | 0 +- 0.079 | 0 +- 0.069 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.079 | 0 +- 9.958e-5 | 0 +- 0.048 | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | +1.094 +- 0.551 | [32] 0 +- 0.350 |
| CategoriesLastRowRecomputed | 0 +- 0.079 | 0 +- 0.069 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.079 | 0 +- 9.958e-5 | 0 +- 0.048 | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | 0 +- 0.579 | [32] 0 +- 0.350 |
| CategoriesEmptyRowsMerged | 0 +- 0.079 | 0 +- 0.069 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.079 | 0 +- 9.958e-5 | 0 +- 0.048 | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | +0.156 +- 0.438 | [32] 0 +- 0.350 |
| PdoksmallFirstCell | 0 +- 0.079 | 0 +- 0.069 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.079 | 0 +- 9.958e-5 | 0 +- 0.048 | 0 +- 0 | 0 +- 5.286e-4 | abs 0 +- 0 | 0 +- 0.579 | [32] 0 +- 0.350 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.7 (+8.9) | +0.9 (+12.5) | -94.7 (-215.6) | -94.7 (-215.6) | +6.3 (+76.5) | +4543.4 (+73554.7) | -12.0 (-78.5) | 0.0 (0.0) | -4646.7 (-72627.2) | 0.0 (0.0) | -7.1 (-13.8) | -4292.1 (-4292.1) |
| ExactConstants | +0.7 (+8.9) | +0.9 (+12.5) | -0.1 (-1.0) | -0.1 (-1.0) | +0.6 (+6.8) | -123.2 (-2207.3) | 0.0 (+0.3) | 0.0 (0.0) | 0.0 (+0.6) | 0.0 (0.0) | +0.3 (+1.0) | +0.5 (+1.0) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | -59.3 (-157.0) | -59.3 (-157.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| Fmkarm2OnAcceptance | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +10.6 (+150.8) | +10.6 (+150.8) | 0.0 (0.0) | 0.0 (0.0) | -12.8 (-152.1) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -6545.0 (-6545.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | -60.2 (-1138.7) | -60.2 (-1138.7) | +5.8 (+963.9) | +4591.2 (+168890.8) | 0.0 (0.0) | 0.0 (0.0) | -4578.7 (-inf) | 0.0 (0.0) | 0.0 (0.0) | +119154046186258.0 (+inf) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +2.0 (+3.9) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.4 (+0.4) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (4) against odd seeds (4): 1 / 6094 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-PSAN02-Dmin20um-Di10um-SFR.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 195 / 6012 | fmkarm_cor2 (73), Dkarmcat (72), pdoksmall (19), fqdokkarm(4,:) (8), dokkarm10 (3), dokkarm43 (3), fqdokkarm(5,:) (3), fqdokkarm(6,:) (3), and 10 more |
| ExactConstants | 1 / 6177 | da_coef (1) |
| BridgeFrustum | 3 / 6177 | Zkarm_cor (2), Zkarm (1) |
| Fmkarm2OnAcceptance | 77 / 6177 | fmkarm_cor2 (74), Dagg43_cor(2) (1), Dkarm43_cor(2) (1), Dkarm43sd_cor(2) (1) |
| WindowLowerEdge | 4 / 6177 | Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 6177 | none |
| ZdoksmallCoarseShare | 19 / 6177 | pdoksmall (19) |
| GdokleftOnce | 15 / 6177 | pdoksmall (10), Zkarm_cor (2), Zkarm (1), da_coef (1), fineoxy_fr (1) |
| DoksdConsistent | 0 / 6177 | none |
| CategoriesOnCopy | 0 / 6177 | none |
| CategoriesLastRowRecomputed | 0 / 6177 | none |
| CategoriesEmptyRowsMerged | 28 / 6122 | fqdokkarm(4,:) (9), fqdokkarm(3,:) (6), fqdokkarm(5,:) (4), dokkarm10 (3), dokkarm43 (3), fqdokkarm(6,:) (2), fqdokkarm(2,:) (1) |
| PdoksmallFirstCell | 0 / 6177 | none |

