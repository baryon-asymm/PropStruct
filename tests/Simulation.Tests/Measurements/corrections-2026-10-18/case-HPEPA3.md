## HPEPA3

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^16`, k = 0..15 (16 seeds); 1 cycle(s) of 100000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 868 s over 14 arms (Original 60, All 60, ExactConstants 73, BridgeFrustum 60, WindowLowerEdge 60, VdokTotalOnCopy 60, ZdoksmallCoarseShare 60, GdokleftOnce 60, DoksdConsistent 60, CategoriesOnCopy 74, CategoriesLastRowRecomputed 60, PdoksmallFirstCell 60, AllButCategoriesOnCopy 61, AllButCategoriesLastRowRecomputed 60 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 222.850 | 1.280 | 5.120 |
| Dkarm43_cor(1) | 222.188 | 1.843 | 7.374 |
| Dkarm43_cor(2) | 102.785 | 0.822 | 3.286 |
| Dkarm10 | 34.803 | 7.159e-3 | 0.029 |
| Zkarm | 0.585 | 1.300e-3 | 5.198e-3 |
| Zkarm_cor | 0.585 | 1.300e-3 | 5.198e-3 |
| Dagg43 | 164.675 | 0.946 | 3.783 |
| Dagg43_cor(1) | 164.186 | 1.363 | 5.450 |
| Dagg43_cor(2) | 75.953 | 0.607 | 2.426 |
| da_coef | 0.739 | 2.867e-17 | 1.147e-16 |
| Dqmkm2 | 0.637 | 2.495e-5 | 9.979e-5 |
| Dok43sd | 108.600 | 7.338e-15 | 2.935e-14 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 17.563 | 0.182 | 0.727 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.079 +- 0.812 | +0.073 +- 1.175 | +0.040 +- 1.129 | +0.052 +- 0.029 | +8.979 +- 0.318* | +8.979 +- 0.318* | +0.069 +- 0.812 | +0.062 +- 1.175 | +0.030 +- 1.128 | -9.960e-3 +- 3.879e-15* | -6.947 +- 6.155e-3* | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | +23.132 +- 1.546* | [4] +0.012 +- 0.050 |
| ExactConstants | +0.079 +- 0.812 | +0.073 +- 1.175 | +0.055 +- 1.132 | +0.052 +- 0.029 | -0.022 +- 0.316 | -0.022 +- 0.316 | +0.069 +- 0.812 | +0.062 +- 1.175 | +0.045 +- 1.131 | -9.960e-3 +- 3.879e-15* | +5.889e-3 +- 5.400e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [4] +0.012 +- 0.050 |
| BridgeFrustum | 0 +- 0.812 | 0 +- 1.173 | 0 +- 1.130 | 0 +- 0.029 | -3.400 +- 0.319* | -3.400 +- 0.319* | 0 +- 0.812 | 0 +- 1.174 | 0 +- 1.129 | 0 +- 5.486e-15 | 0 +- 5.541e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [2] 0 +- 0.066 |
| WindowLowerEdge | 0 +- 0.812 | 0 +- 1.173 | -0.049 +- 1.124 | 0 +- 0.029 | +11.844 +- 0.314* | +11.844 +- 0.314* | 0 +- 0.812 | 0 +- 1.174 | -0.053 +- 1.124 | 0 +- 5.486e-15 | -6.953 +- 5.856e-3* | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [2] 0 +- 0.066 |
| VdokTotalOnCopy | 0 +- 0.812 | 0 +- 1.173 | 0 +- 1.130 | 0 +- 0.029 | 0 +- 0.314 | 0 +- 0.314 | 0 +- 0.812 | 0 +- 1.174 | 0 +- 1.129 | 0 +- 5.486e-15 | 0 +- 5.541e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [2] 0 +- 0.066 |
| ZdoksmallCoarseShare | 0 +- 0.812 | 0 +- 1.173 | 0 +- 1.130 | 0 +- 0.029 | 0 +- 0.314 | 0 +- 0.314 | 0 +- 0.812 | 0 +- 1.174 | 0 +- 1.129 | 0 +- 5.486e-15 | 0 +- 5.541e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [2] 0 +- 0.066 |
| GdokleftOnce | 0 +- 0.812 | 0 +- 1.173 | 0 +- 1.130 | 0 +- 0.029 | 0 +- 0.314 | 0 +- 0.314 | 0 +- 0.812 | 0 +- 1.174 | 0 +- 1.129 | 0 +- 5.486e-15 | 0 +- 5.541e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [2] 0 +- 0.066 |
| DoksdConsistent | 0 +- 0.812 | 0 +- 1.173 | 0 +- 1.130 | 0 +- 0.029 | 0 +- 0.314 | 0 +- 0.314 | 0 +- 0.812 | 0 +- 1.174 | 0 +- 1.129 | 0 +- 5.486e-15 | 0 +- 5.541e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [2] 0 +- 0.066 |
| CategoriesOnCopy | 0 +- 0.812 | 0 +- 1.173 | 0 +- 1.130 | 0 +- 0.029 | 0 +- 0.314 | 0 +- 0.314 | 0 +- 0.812 | 0 +- 1.174 | 0 +- 1.129 | 0 +- 5.486e-15 | 0 +- 5.541e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | +23.132 +- 1.546* | [2] 0 +- 0.066 |
| CategoriesLastRowRecomputed | 0 +- 0.812 | 0 +- 1.173 | 0 +- 1.130 | 0 +- 0.029 | 0 +- 0.314 | 0 +- 0.314 | 0 +- 0.812 | 0 +- 1.174 | 0 +- 1.129 | 0 +- 5.486e-15 | 0 +- 5.541e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [2] 0 +- 0.066 |
| PdoksmallFirstCell | 0 +- 0.812 | 0 +- 1.173 | 0 +- 1.130 | 0 +- 0.029 | 0 +- 0.314 | 0 +- 0.314 | 0 +- 0.812 | 0 +- 1.174 | 0 +- 1.129 | 0 +- 5.486e-15 | 0 +- 5.541e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [2] 0 +- 0.066 |
| AllButCategoriesOnCopy | +0.079 +- 0.812 | +0.073 +- 1.175 | +0.040 +- 1.129 | +0.052 +- 0.029 | +8.979 +- 0.318* | +8.979 +- 0.318* | +0.069 +- 0.812 | +0.062 +- 1.175 | +0.030 +- 1.128 | -9.960e-3 +- 3.879e-15* | -6.947 +- 6.155e-3* | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.464 | [4] +0.012 +- 0.050 |
| AllButCategoriesLastRowRecomputed | +0.079 +- 0.812 | +0.073 +- 1.175 | +0.040 +- 1.129 | +0.052 +- 0.029 | +8.979 +- 0.318* | +8.979 +- 0.318* | +0.069 +- 0.812 | +0.062 +- 1.175 | +0.030 +- 1.128 | -9.960e-3 +- 3.879e-15* | -6.947 +- 6.155e-3* | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | +23.132 +- 1.546* | [4] +0.012 +- 0.050 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.1 (+6.8) | +0.1 (+5.7) | 0.0 (+0.3) | +1.8 (+18.0) | +28.2 (+124.0) | +28.2 (+124.0) | +0.1 (+6.0) | +0.1 (+4.9) | 0.0 (+0.3) | -2567516327429.5 (-inf) | -1128.6 (-1999.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +15.0 (+23.9) | +0.2 (+1.0) |
| ExactConstants | +0.1 (+6.8) | +0.1 (+5.7) | 0.0 (+2.1) | +1.8 (+18.0) | -0.1 (-1.5) | -0.1 (-1.5) | +0.1 (+6.0) | +0.1 (+4.9) | 0.0 (+1.7) | -2567516327429.5 (-inf) | +1.1 (+3.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.2 (+1.0) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -10.7 (-111.5) | -10.7 (-111.5) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | 0.0 (-0.5) | 0.0 (0.0) | +37.7 (+169.3) | +37.7 (+169.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (-0.6) | 0.0 (0.0) | -1187.3 (-2286.4) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +15.0 (+23.9) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| AllButCategoriesOnCopy | +0.1 (+6.8) | +0.1 (+5.7) | 0.0 (+0.3) | +1.8 (+18.0) | +28.2 (+124.0) | +28.2 (+124.0) | +0.1 (+6.0) | +0.1 (+4.9) | 0.0 (+0.3) | -2567516327429.5 (-inf) | -1128.6 (-1999.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.2 (+1.0) |
| AllButCategoriesLastRowRecomputed | +0.1 (+6.8) | +0.1 (+5.7) | 0.0 (+0.3) | +1.8 (+18.0) | +28.2 (+124.0) | +28.2 (+124.0) | +0.1 (+6.0) | +0.1 (+4.9) | 0.0 (+0.3) | -2567516327429.5 (-inf) | -1128.6 (-1999.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +15.0 (+23.9) | +0.2 (+1.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 0 / 2109 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-HPEPA3.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 158 / 2138 | fqmkm2 (90), fqdokkarm(9,:) (19), fqdokkarm(8,:) (18), fqdokkarm(10,:) (16), Dkarmcat (4), dokkarm43 (3), Zkarm_cor (2), dokkarm10 (2), and 4 more |
| ExactConstants | 1 / 2139 | da_coef (1) |
| BridgeFrustum | 2 / 2139 | Zkarm (1), Zkarm_cor (1) |
| WindowLowerEdge | 95 / 2138 | fqmkm2 (90), Zkarm_cor (2), Dqmkm2 (1), NFQ (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 2139 | none |
| ZdoksmallCoarseShare | 0 / 2139 | none |
| GdokleftOnce | 0 / 2139 | none |
| DoksdConsistent | 0 / 2139 | none |
| CategoriesOnCopy | 62 / 2139 | fqdokkarm(9,:) (19), fqdokkarm(8,:) (18), fqdokkarm(10,:) (16), Dkarmcat (4), dokkarm43 (3), dokkarm10 (2) |
| CategoriesLastRowRecomputed | 0 / 2139 | none |
| PdoksmallFirstCell | 0 / 2139 | none |
| AllButCategoriesOnCopy | 96 / 2138 | fqmkm2 (90), Zkarm_cor (2), Dqmkm2 (1), NFQ (1), Zkarm (1), da_coef (1) |
| AllButCategoriesLastRowRecomputed | 158 / 2138 | fqmkm2 (90), fqdokkarm(9,:) (19), fqdokkarm(8,:) (18), fqdokkarm(10,:) (16), Dkarmcat (4), dokkarm43 (3), Zkarm_cor (2), dokkarm10 (2), and 4 more |

