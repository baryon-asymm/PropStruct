## inpt

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..15 (16 seeds); 20 cycle(s) of 1000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 60 s over 14 arms (Original 5, All 4, ExactConstants 4, BridgeFrustum 5, Fmkarm2OnAcceptance 4, WindowLowerEdge 4, VdokTotalOnCopy 4, ZdoksmallCoarseShare 4, GdokleftOnce 4, DoksdConsistent 4, CategoriesOnCopy 4, CategoriesLastRowRecomputed 4, CategoriesEmptyRowsMerged 4, PdoksmallFirstCell 4 s).

Failed runs: none.

Builds behind the runs: 2 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 215.069 | 0.073 | 0.290 |
| Dkarm10 | 160.734 | 0.022 | 0.087 |
| Zkarm | 0.500 | 1.766e-4 | 7.064e-4 |
| Zkarm_cor | 0.500 | 1.766e-4 | 7.064e-4 |
| Dagg43 | 167.345 | 0.056 | 0.225 |
| da_coef | 0.778 | 5.733e-17 | 2.293e-16 |
| Dqmkm2 | 0.520 | 3.860e-5 | 1.544e-4 |
| Dok43sd | 24.830 | 1.835e-15 | 7.338e-15 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 37.625 | 0.125 | 0.500 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.048 +- 0.047 | +0.037 +- 0.019 | -4.566 +- 0.053* | -4.566 +- 0.053* | +0.037 +- 0.047 | -9.433e-3 +- 8.238e-15* | -2.793 +- 0.011* | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | -14.950 +- 0.332* | n/a |
| ExactConstants | +0.048 +- 0.047 | +0.037 +- 0.019 | -0.092 +- 0.047 | -0.092 +- 0.047 | +0.037 +- 0.047 | -9.433e-3 +- 8.238e-15* | +0.012 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| BridgeFrustum | 0 +- 0.048 | 0 +- 0.019 | -12.424 +- 0.050* | -12.424 +- 0.050* | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| Fmkarm2OnAcceptance | 0 +- 0.048 | 0 +- 0.019 | 0 +- 0.050 | 0 +- 0.050 | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| WindowLowerEdge | 0 +- 0.048 | 0 +- 0.019 | +7.139 +- 0.054* | +7.139 +- 0.054* | 0 +- 0.048 | 0 +- 1.042e-14 | -2.805 +- 0.011* | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| VdokTotalOnCopy | 0 +- 0.048 | 0 +- 0.019 | 0 +- 0.050 | 0 +- 0.050 | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| ZdoksmallCoarseShare | 0 +- 0.048 | 0 +- 0.019 | 0 +- 0.050 | 0 +- 0.050 | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| GdokleftOnce | 0 +- 0.048 | 0 +- 0.019 | 0 +- 0.050 | 0 +- 0.050 | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| DoksdConsistent | 0 +- 0.048 | 0 +- 0.019 | 0 +- 0.050 | 0 +- 0.050 | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| CategoriesOnCopy | 0 +- 0.048 | 0 +- 0.019 | 0 +- 0.050 | 0 +- 0.050 | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | +0.997 +- 0.332 | n/a |
| CategoriesLastRowRecomputed | 0 +- 0.048 | 0 +- 0.019 | 0 +- 0.050 | 0 +- 0.050 | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| CategoriesEmptyRowsMerged | 0 +- 0.048 | 0 +- 0.019 | 0 +- 0.050 | 0 +- 0.050 | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |
| PdoksmallFirstCell | 0 +- 0.048 | 0 +- 0.019 | 0 +- 0.050 | 0 +- 0.050 | 0 +- 0.048 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.470 | n/a |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +1.0 (+8.6) | +1.9 (+19.0) | -86.4 (-140.0) | -86.4 (-140.0) | +0.8 (+7.0) | -1145108022942.4 (-inf) | -243.4 (-850.5) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -45.0 (-45.0) | n/a |
| ExactConstants | +1.0 (+8.6) | +1.9 (+19.0) | -1.9 (-9.2) | -1.9 (-9.2) | +0.8 (+7.0) | -1145108022942.4 (-inf) | +1.2 (+5.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | -248.6 (-545.6) | -248.6 (-545.6) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| Fmkarm2OnAcceptance | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +132.3 (+476.7) | +132.3 (+476.7) | 0.0 (0.0) | 0.0 (0.0) | -253.1 (-942.4) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +3.0 (+3.0) | n/a |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 2 / 2633 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-inpt.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 476 / 2420 | fqmkm2 (74), fmkarm_cor2 (35), Dkarmcat (32), dokkarm43 (32), dokkarm10 (31), epszkarm_n (20), fqdokkarm(4,:) (10), fqdokkarm(5,:) (10), and 37 more |
| ExactConstants | 1 / 2654 | da_coef (1) |
| BridgeFrustum | 23 / 2654 | epszkarm_n (20), Zkarm_cor (2), Zkarm (1) |
| Fmkarm2OnAcceptance | 38 / 2654 | fmkarm_cor2 (35), Dagg43_cor(2) (1), Dkarm43_cor(2) (1), Dkarm43sd_cor(2) (1) |
| WindowLowerEdge | 99 / 2654 | fqmkm2 (75), epszkarm_n (20), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 2654 | none |
| ZdoksmallCoarseShare | 0 / 2654 | none |
| GdokleftOnce | 0 / 2654 | none |
| DoksdConsistent | 0 / 2654 | none |
| CategoriesOnCopy | 5 / 2654 | fqdokkarm(8,:) (3), fqdokkarm(9,:) (2) |
| CategoriesLastRowRecomputed | 0 / 2654 | none |
| CategoriesEmptyRowsMerged | 82 / 2654 | fqdokkarm(7,:) (10), fqdokkarm(2,:) (9), fqdokkarm(3,:) (9), fqdokkarm(4,:) (9), fqdokkarm(5,:) (9), fqdokkarm(6,:) (9), dokkarm10 (8), dokkarm43 (8), and 2 more |
| PdoksmallFirstCell | 0 / 2654 | none |

