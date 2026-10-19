## P312-Dmin5um

P312 at Dmin 5 um, particles per cycle reduced from 5000 to 100. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^16`, k = 0..7 (8 seeds); 1 cycle(s) of 100 particles; `Dmin` 5 um, `Di` 10 um, `Dj` 10 um.

Work: 4 s over 14 arms (Original 0, All 0, ExactConstants 0, BridgeFrustum 0, WindowLowerEdge 0, VdokTotalOnCopy 0, ZdoksmallCoarseShare 0, GdokleftOnce 0, DoksdConsistent 0, CategoriesOnCopy 0, CategoriesLastRowRecomputed 0, PdoksmallFirstCell 0, AllButCategoriesOnCopy 1, AllButCategoriesLastRowRecomputed 0 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 47.178 | 8.703 | 24.616 |
| Dkarm43_cor(1) | 33.800 | 1.029 | 2.911 |
| Dkarm43_cor(2) | 34.314 | 1.117 | 3.158 |
| Dkarm10 | 23.616 | 0.073 | 0.205 |
| Zkarm | 0.330 | 0.081 | 0.228 |
| Zkarm_cor | 0.330 | 0.081 | 0.228 |
| Dagg43 | 32.414 | 5.979 | 16.910 |
| Dagg43_cor(1) | 23.221 | 0.708 | 2.001 |
| Dagg43_cor(2) | 23.576 | 0.766 | 2.168 |
| da_coef | 0.687 | 0 | 0 |
| Dqmkm2 | 0.707 | 1.331e-3 | 3.766e-3 |
| Dok43sd | 67.740 | 0 | 0 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 3.625 | 0.800 | 2.264 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | -0.148 +- 26.014 | -7.396e-3 +- 4.359 | +0.984 +- 4.802 | +0.016 +- 0.422 | +18.355 +- 33.003 | +18.355 +- 33.003 | -0.162 +- 26.011 | -0.011 +- 4.361 | +0.965 +- 4.802 | -0.011 +- 0* | -5.930 +- 0.390* | 0 +- 0 | abs 0 +- 0 | abs +0.150 +- 4.336e-3* | 0 +- 30.787 | [3] +0.294 +- 3.501 |
| ExactConstants | -0.148 +- 26.014 | -7.396e-3 +- 4.359 | -0.022 +- 4.636 | +0.016 +- 0.422 | -0.125 +- 34.534 | -0.125 +- 34.534 | -0.162 +- 26.011 | -0.011 +- 4.361 | -0.027 +- 4.634 | -0.011 +- 0* | +7.071e-3 +- 0.293 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 31.225 | [3] +0.294 +- 3.501 |
| BridgeFrustum | 0 +- 26.088 | 0 +- 4.307 | 0 +- 4.602 | 0 +- 0.434 | -12.019 +- 34.034 | -12.019 +- 34.034 | 0 +- 26.085 | 0 +- 4.309 | 0 +- 4.597 | 0 +- 0 | 0 +- 0.266 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 31.225 | [1] 0 +- 3.613 |
| WindowLowerEdge | 0 +- 26.088 | 0 +- 4.307 | -3.643e-3 +- 4.602 | 0 +- 0.434 | +28.186 +- 33.068 | +28.186 +- 33.068 | 0 +- 26.085 | 0 +- 4.309 | -5.302e-3 +- 4.597 | 0 +- 0 | -5.953 +- 0.318* | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 31.225 | [1] 0 +- 3.613 |
| VdokTotalOnCopy | 0 +- 26.088 | 0 +- 4.307 | 0 +- 4.602 | 0 +- 0.434 | 0 +- 34.515 | 0 +- 34.515 | 0 +- 26.085 | 0 +- 4.309 | 0 +- 4.597 | 0 +- 0 | 0 +- 0.266 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 31.225 | [1] 0 +- 3.613 |
| ZdoksmallCoarseShare | 0 +- 26.088 | 0 +- 4.307 | 0 +- 4.602 | 0 +- 0.434 | 0 +- 34.515 | 0 +- 34.515 | 0 +- 26.085 | 0 +- 4.309 | 0 +- 4.597 | 0 +- 0 | 0 +- 0.266 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 31.225 | [1] 0 +- 3.613 |
| GdokleftOnce | 0 +- 26.088 | 0 +- 4.307 | 0 +- 4.602 | 0 +- 0.434 | 0 +- 34.515 | 0 +- 34.515 | 0 +- 26.085 | 0 +- 4.309 | 0 +- 4.597 | 0 +- 0 | 0 +- 0.266 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 31.225 | [1] 0 +- 3.613 |
| DoksdConsistent | 0 +- 26.088 | 0 +- 4.307 | 0 +- 4.602 | 0 +- 0.434 | 0 +- 34.515 | 0 +- 34.515 | 0 +- 26.085 | 0 +- 4.309 | 0 +- 4.597 | 0 +- 0 | 0 +- 0.266 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 31.225 | [1] 0 +- 3.613 |
| CategoriesOnCopy | 0 +- 26.088 | 0 +- 4.307 | 0 +- 4.602 | 0 +- 0.434 | 0 +- 34.515 | 0 +- 34.515 | 0 +- 26.085 | 0 +- 4.309 | 0 +- 4.597 | 0 +- 0 | 0 +- 0.266 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +20.690 +- 24.934 | [1] 0 +- 3.613 |
| CategoriesLastRowRecomputed | 0 +- 26.088 | 0 +- 4.307 | 0 +- 4.602 | 0 +- 0.434 | 0 +- 34.515 | 0 +- 34.515 | 0 +- 26.085 | 0 +- 4.309 | 0 +- 4.597 | 0 +- 0 | 0 +- 0.266 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 31.225 | [1] 0 +- 3.613 |
| PdoksmallFirstCell | 0 +- 26.088 | 0 +- 4.307 | +0.987 +- 4.768 | 0 +- 0.434 | 0 +- 34.515 | 0 +- 34.515 | 0 +- 26.085 | 0 +- 4.309 | +0.981 +- 4.765 | 0 +- 0 | 0 +- 0.266 | 0 +- 0 | abs 0 +- 0 | abs +0.149 +- 4.048e-3* | 0 +- 31.225 | [1] 0 +- 3.613 |
| AllButCategoriesOnCopy | -0.148 +- 26.014 | -7.396e-3 +- 4.359 | +0.984 +- 4.802 | +0.016 +- 0.422 | +18.355 +- 33.003 | +18.355 +- 33.003 | -0.162 +- 26.011 | -0.011 +- 4.361 | +0.965 +- 4.802 | -0.011 +- 0* | -5.930 +- 0.390* | 0 +- 0 | abs 0 +- 0 | abs +0.150 +- 4.336e-3* | 0 +- 31.225 | [3] +0.294 +- 3.501 |
| AllButCategoriesLastRowRecomputed | -0.148 +- 26.014 | -7.396e-3 +- 4.359 | +0.984 +- 4.802 | +0.016 +- 0.422 | +18.355 +- 33.003 | +18.355 +- 33.003 | -0.162 +- 26.011 | -0.011 +- 4.361 | +0.965 +- 4.802 | -0.011 +- 0* | -5.930 +- 0.390* | 0 +- 0 | abs 0 +- 0 | abs +0.150 +- 4.336e-3* | +20.690 +- 24.934 | [3] +0.294 +- 3.501 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | 0.0 (-1.0) | 0.0 (-0.1) | +0.2 (+3.2) | 0.0 (+0.2) | +0.6 (+8.0) | +0.6 (+8.0) | 0.0 (-1.1) | 0.0 (-0.1) | +0.2 (+3.1) | -inf (-inf) | -15.2 (-30.6) | 0.0 (0.0) | 0.0 (0.0) | +34.6 (+34.6) | 0.0 (0.0) | +0.1 (+0.9) |
| ExactConstants | 0.0 (-1.0) | 0.0 (-0.1) | 0.0 (-0.2) | 0.0 (+0.2) | 0.0 (-0.4) | 0.0 (-0.4) | 0.0 (-1.1) | 0.0 (-0.1) | 0.0 (-0.2) | -inf (-inf) | 0.0 (+0.1) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.1 (+0.9) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -0.4 (-13.0) | -0.4 (-13.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | 0.0 (-1.0) | 0.0 (0.0) | +0.9 (+13.0) | +0.9 (+13.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (-1.0) | 0.0 (0.0) | -18.7 (-53.9) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.8 (+1.8) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | +0.2 (+4.2) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.2 (+4.1) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +36.9 (+36.9) | 0.0 (0.0) | 0.0 (0.0) |
| AllButCategoriesOnCopy | 0.0 (-1.0) | 0.0 (-0.1) | +0.2 (+3.2) | 0.0 (+0.2) | +0.6 (+8.0) | +0.6 (+8.0) | 0.0 (-1.1) | 0.0 (-0.1) | +0.2 (+3.1) | -inf (-inf) | -15.2 (-30.6) | 0.0 (0.0) | 0.0 (0.0) | +34.6 (+34.6) | 0.0 (0.0) | +0.1 (+0.9) |
| AllButCategoriesLastRowRecomputed | 0.0 (-1.0) | 0.0 (-0.1) | +0.2 (+3.2) | 0.0 (+0.2) | +0.6 (+8.0) | +0.6 (+8.0) | 0.0 (-1.1) | 0.0 (-0.1) | +0.2 (+3.1) | -inf (-inf) | -15.2 (-30.6) | 0.0 (0.0) | 0.0 (0.0) | +34.6 (+34.6) | +0.8 (+1.8) | +0.1 (+0.9) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (4) against odd seeds (4): 0 / 1234 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-P312-Dmin5um.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 4 / 1275 | Dkarmcat (1), Dqmkm2 (1), da_coef (1), pdoksmall (1) |
| ExactConstants | 1 / 1300 | da_coef (1) |
| BridgeFrustum | 0 / 1300 | none |
| WindowLowerEdge | 1 / 1298 | Dqmkm2 (1) |
| VdokTotalOnCopy | 0 / 1300 | none |
| ZdoksmallCoarseShare | 0 / 1300 | none |
| GdokleftOnce | 0 / 1300 | none |
| DoksdConsistent | 0 / 1300 | none |
| CategoriesOnCopy | 1 / 1277 | Dkarmcat (1) |
| CategoriesLastRowRecomputed | 0 / 1300 | none |
| PdoksmallFirstCell | 1 / 1300 | pdoksmall (1) |
| AllButCategoriesOnCopy | 3 / 1298 | Dqmkm2 (1), da_coef (1), pdoksmall (1) |
| AllButCategoriesLastRowRecomputed | 4 / 1275 | Dkarmcat (1), Dqmkm2 (1), da_coef (1), pdoksmall (1) |

