## PSAN02-Dmin20um-Di10um-SFR

PSAN02 read with its pocket-forming fractions, Dmin 20 um, Di 10 um, particles per cycle as read. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^16`, k = 0..7 (8 seeds); 1 cycle(s) of 5000 particles; `Dmin` 20 um, `Di` 10 um, `Dj` 10 um.

Work: 855 s over 14 arms (Original 62, All 61, ExactConstants 61, BridgeFrustum 61, WindowLowerEdge 61, VdokTotalOnCopy 61, ZdoksmallCoarseShare 61, GdokleftOnce 61, DoksdConsistent 62, CategoriesOnCopy 61, CategoriesLastRowRecomputed 61, PdoksmallFirstCell 62, AllButCategoriesOnCopy 61, AllButCategoriesLastRowRecomputed 61 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 570.568 | 0.328 | 0.926 |
| Dkarm43_cor(1) | 570.224 | 0.530 | 1.499 |
| Dkarm43_cor(2) | 569.233 | 0.575 | 1.627 |
| Dkarm10 | 432.390 | 0.245 | 0.693 |
| Zkarm | 0.845 | 1.384e-4 | 3.915e-4 |
| Zkarm_cor | 0.845 | 1.384e-4 | 3.915e-4 |
| Dagg43 | 341.863 | 0.197 | 0.556 |
| Dagg43_cor(1) | 341.659 | 0.318 | 0.899 |
| Dagg43_cor(2) | 341.065 | 0.345 | 0.976 |
| da_coef | 0.599 | 4.257e-7 | 1.204e-6 |
| Dqmkm2 | 0.707 | 3.903e-4 | 1.104e-3 |
| Dok43sd | 75.820 | 0 | 0 |
| fineoxy_fr | 0.724 | 2.717e-6 | 7.686e-6 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 80.500 | 0.189 | 0.535 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.058 +- 0.080 | +0.066 +- 0.133 | +0.067 +- 0.144 | +0.063 +- 0.080 | -2.879 +- 0.029* | -2.879 +- 0.029* | +0.508 +- 0.080* | +0.516 +- 0.133 | +0.517 +- 0.145 | +0.449 +- 9.986e-5* | -0.633 +- 0.081* | 0 +- 0 | -2.420 +- 5.199e-4* | abs 0 +- 0 | +0.311 +- 0.389 | [50] -59.952 +- 0.014* |
| ExactConstants | +0.058 +- 0.080 | +0.066 +- 0.133 | +0.067 +- 0.144 | +0.063 +- 0.080 | -5.351e-3 +- 0.023 | -5.351e-3 +- 0.023 | +0.046 +- 0.080 | +0.054 +- 0.133 | +0.055 +- 0.144 | -0.012 +- 9.794e-5* | -3.537e-3 +- 0.079 | 0 +- 0 | +5.180e-6 +- 5.199e-4 | abs 0 +- 0 | -0.155 +- 0.327 | [48] +0.105 +- 0.222 |
| BridgeFrustum | 0 +- 0.081 | 0 +- 0.131 | 0 +- 0.143 | 0 +- 0.080 | -1.604 +- 0.027* | -1.604 +- 0.027* | 0 +- 0.081 | 0 +- 0.132 | 0 +- 0.143 | 0 +- 1.005e-4 | 0 +- 0.078 | 0 +- 0 | 0 +- 5.308e-4 | abs 0 +- 0 | 0 +- 0.332 | [32] 0 +- 0.300 |
| WindowLowerEdge | 0 +- 0.081 | 0 +- 0.131 | 0 +- 0.143 | 0 +- 0.080 | +0.249 +- 0.023* | +0.249 +- 0.023* | 0 +- 0.081 | 0 +- 0.132 | 0 +- 0.143 | 0 +- 1.005e-4 | -0.633 +- 0.079* | 0 +- 0 | 0 +- 5.308e-4 | abs 0 +- 0 | 0 +- 0.332 | [32] 0 +- 0.300 |
| VdokTotalOnCopy | 0 +- 0.081 | 0 +- 0.131 | 0 +- 0.143 | 0 +- 0.080 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.081 | 0 +- 0.132 | 0 +- 0.143 | 0 +- 1.005e-4 | 0 +- 0.078 | 0 +- 0 | 0 +- 5.308e-4 | abs 0 +- 0 | 0 +- 0.332 | [32] 0 +- 0.300 |
| ZdoksmallCoarseShare | 0 +- 0.081 | 0 +- 0.131 | 0 +- 0.143 | 0 +- 0.080 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.081 | 0 +- 0.132 | 0 +- 0.143 | 0 +- 1.005e-4 | 0 +- 0.078 | 0 +- 0 | 0 +- 5.308e-4 | abs 0 +- 0 | 0 +- 0.332 | [50] -62.452 +- 9.542e-3* |
| GdokleftOnce | 0 +- 0.081 | 0 +- 0.131 | 0 +- 0.143 | 0 +- 0.080 | -1.436 +- 0.024* | -1.436 +- 0.024* | +0.462 +- 0.081* | +0.461 +- 0.132 | +0.461 +- 0.143 | +0.461 +- 1.014e-4* | 0 +- 0.078 | 0 +- 0 | -2.420 +- 5.308e-4* | abs 0 +- 0 | 0 +- 0.332 | [49] +0.787 +- 6.608e-15* |
| DoksdConsistent | 0 +- 0.081 | 0 +- 0.131 | 0 +- 0.143 | 0 +- 0.080 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.081 | 0 +- 0.132 | 0 +- 0.143 | 0 +- 1.005e-4 | 0 +- 0.078 | 0 +- 0 | 0 +- 5.308e-4 | abs 0 +- 0 | 0 +- 0.332 | [32] 0 +- 0.300 |
| CategoriesOnCopy | 0 +- 0.081 | 0 +- 0.131 | 0 +- 0.143 | 0 +- 0.080 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.081 | 0 +- 0.132 | 0 +- 0.143 | 0 +- 1.005e-4 | 0 +- 0.078 | 0 +- 0 | 0 +- 5.308e-4 | abs 0 +- 0 | +0.311 +- 0.389 | [32] 0 +- 0.300 |
| CategoriesLastRowRecomputed | 0 +- 0.081 | 0 +- 0.131 | 0 +- 0.143 | 0 +- 0.080 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.081 | 0 +- 0.132 | 0 +- 0.143 | 0 +- 1.005e-4 | 0 +- 0.078 | 0 +- 0 | 0 +- 5.308e-4 | abs 0 +- 0 | 0 +- 0.332 | [32] 0 +- 0.300 |
| PdoksmallFirstCell | 0 +- 0.081 | 0 +- 0.131 | 0 +- 0.143 | 0 +- 0.080 | 0 +- 0.023 | 0 +- 0.023 | 0 +- 0.081 | 0 +- 0.132 | 0 +- 0.143 | 0 +- 1.005e-4 | 0 +- 0.078 | 0 +- 0 | 0 +- 5.308e-4 | abs 0 +- 0 | 0 +- 0.332 | [32] 0 +- 0.300 |
| AllButCategoriesOnCopy | +0.058 +- 0.080 | +0.066 +- 0.133 | +0.067 +- 0.144 | +0.063 +- 0.080 | -2.879 +- 0.029* | -2.879 +- 0.029* | +0.508 +- 0.080* | +0.516 +- 0.133 | +0.517 +- 0.145 | +0.449 +- 9.986e-5* | -0.633 +- 0.081* | 0 +- 0 | -2.420 +- 5.199e-4* | abs 0 +- 0 | -0.155 +- 0.327 | [50] -59.952 +- 0.014* |
| AllButCategoriesLastRowRecomputed | +0.058 +- 0.080 | +0.066 +- 0.133 | +0.067 +- 0.144 | +0.063 +- 0.080 | -2.879 +- 0.029* | -2.879 +- 0.029* | +0.508 +- 0.080* | +0.516 +- 0.133 | +0.517 +- 0.145 | +0.449 +- 9.986e-5* | -0.633 +- 0.081* | 0 +- 0 | -2.420 +- 5.199e-4* | abs 0 +- 0 | +0.311 +- 0.389 | [50] -59.952 +- 0.014* |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.7 (+10.9) | +0.5 (+6.8) | +0.5 (+6.7) | +0.8 (+14.3) | -99.1 (-220.0) | -99.1 (-220.0) | +6.3 (+91.0) | +3.9 (+53.2) | +3.6 (+51.3) | +4499.0 (+142439.3) | -7.8 (-108.6) | 0.0 (0.0) | -4654.9 (-91821.7) | 0.0 (0.0) | +0.8 (+1.5) | -4292.1 (-4292.1) |
| ExactConstants | +0.7 (+10.9) | +0.5 (+6.8) | +0.5 (+6.7) | +0.8 (+14.3) | -0.2 (-1.8) | -0.2 (-1.8) | +0.6 (+8.6) | +0.4 (+5.6) | +0.4 (+5.7) | -123.1 (-2447.5) | 0.0 (-1.0) | 0.0 (0.0) | 0.0 (+0.2) | 0.0 (0.0) | -0.5 (-1.0) | +0.5 (+1.0) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -60.0 (-153.3) | -60.0 (-153.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +10.7 (+159.7) | +10.7 (+159.7) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -8.0 (-179.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -6545.0 (-6545.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -59.6 (-1118.4) | -59.6 (-1118.4) | +5.7 (+169834250956647.0) | +3.5 (+833.4) | +3.2 (+960.8) | +4548.2 (+151061.2) | 0.0 (0.0) | 0.0 (0.0) | -4559.2 (-inf) | 0.0 (0.0) | 0.0 (0.0) | +119154046186258.0 (+inf) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.8 (+1.5) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| AllButCategoriesOnCopy | +0.7 (+10.9) | +0.5 (+6.8) | +0.5 (+6.7) | +0.8 (+14.3) | -99.1 (-220.0) | -99.1 (-220.0) | +6.3 (+91.0) | +3.9 (+53.2) | +3.6 (+51.3) | +4499.0 (+142439.3) | -7.8 (-108.6) | 0.0 (0.0) | -4654.9 (-91821.7) | 0.0 (0.0) | -0.5 (-1.0) | -4292.1 (-4292.1) |
| AllButCategoriesLastRowRecomputed | +0.7 (+10.9) | +0.5 (+6.8) | +0.5 (+6.7) | +0.8 (+14.3) | -99.1 (-220.0) | -99.1 (-220.0) | +6.3 (+91.0) | +3.9 (+53.2) | +3.6 (+51.3) | +4499.0 (+142439.3) | -7.8 (-108.6) | 0.0 (0.0) | -4654.9 (-91821.7) | 0.0 (0.0) | +0.8 (+1.5) | -4292.1 (-4292.1) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (4) against odd seeds (4): 0 / 6166 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-PSAN02-Dmin20um-Di10um-SFR.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 24 / 6189 | pdoksmall (19), Zkarm_cor (2), Zkarm (1), da_coef (1), fineoxy_fr (1) |
| ExactConstants | 1 / 6189 | da_coef (1) |
| BridgeFrustum | 3 / 6189 | Zkarm_cor (2), Zkarm (1) |
| WindowLowerEdge | 3 / 6189 | Zkarm_cor (2), Zkarm (1) |
| VdokTotalOnCopy | 0 / 6189 | none |
| ZdoksmallCoarseShare | 19 / 6189 | pdoksmall (19) |
| GdokleftOnce | 16 / 6189 | pdoksmall (11), Zkarm_cor (2), Zkarm (1), da_coef (1), fineoxy_fr (1) |
| DoksdConsistent | 0 / 6189 | none |
| CategoriesOnCopy | 0 / 6189 | none |
| CategoriesLastRowRecomputed | 0 / 6189 | none |
| PdoksmallFirstCell | 0 / 6189 | none |
| AllButCategoriesOnCopy | 24 / 6189 | pdoksmall (19), Zkarm_cor (2), Zkarm (1), da_coef (1), fineoxy_fr (1) |
| AllButCategoriesLastRowRecomputed | 24 / 6189 | pdoksmall (19), Zkarm_cor (2), Zkarm (1), da_coef (1), fineoxy_fr (1) |

