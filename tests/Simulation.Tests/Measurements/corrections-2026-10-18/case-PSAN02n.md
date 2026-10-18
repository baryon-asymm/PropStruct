## PSAN02n

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..15 (16 seeds); 4 cycle(s) of 20000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 234 s over 14 arms (Original 34, All 17, ExactConstants 16, BridgeFrustum 15, Fmkarm2OnAcceptance 15, WindowLowerEdge 16, VdokTotalOnCopy 15, ZdoksmallCoarseShare 16, GdokleftOnce 15, DoksdConsistent 15, CategoriesOnCopy 16, CategoriesLastRowRecomputed 15, CategoriesEmptyRowsMerged 14, PdoksmallFirstCell 15 s).

Failed runs: none.

Builds behind the runs: 2 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 162.999 | 0.022 | 0.088 |
| Dkarm10 | 122.258 | 0.011 | 0.046 |
| Zkarm | 0.407 | 1.880e-4 | 7.521e-4 |
| Zkarm_cor | 0.407 | 1.880e-4 | 7.521e-4 |
| Dagg43 | 129.055 | 0.018 | 0.072 |
| da_coef | 0.792 | 2.867e-17 | 1.147e-16 |
| Dqmkm2 | 0.505 | 2.582e-5 | 1.033e-4 |
| Dok43sd | 28.480 | 1.835e-15 | 7.338e-15 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 30.125 | 0.085 | 0.342 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.044 +- 0.019 | +0.040 +- 0.013 | +1.551 +- 0.067* | +1.551 +- 0.067* | +0.040 +- 0.019 | -9.270e-3 +- 8.096e-15* | -3.627 +- 7.113e-3* | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | -10.373 +- 0.283* | n/a |
| ExactConstants | +0.044 +- 0.019 | +0.040 +- 0.013 | -0.067 +- 0.066 | -0.067 +- 0.066 | +0.040 +- 0.019 | -9.270e-3 +- 8.096e-15* | +0.015 +- 7.113e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | +0.415 +- 0.467 | n/a |
| BridgeFrustum | 0 +- 0.019 | 0 +- 0.013 | -12.823 +- 0.069* | -12.823 +- 0.069* | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |
| Fmkarm2OnAcceptance | 0 +- 0.019 | 0 +- 0.013 | 0 +- 0.065 | 0 +- 0.065 | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |
| WindowLowerEdge | 0 +- 0.019 | 0 +- 0.013 | +13.405 +- 0.065* | +13.405 +- 0.065* | 0 +- 0.020 | 0 +- 5.120e-15 | -3.642 +- 6.760e-3* | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |
| VdokTotalOnCopy | 0 +- 0.019 | 0 +- 0.013 | 0 +- 0.065 | 0 +- 0.065 | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |
| ZdoksmallCoarseShare | 0 +- 0.019 | 0 +- 0.013 | 0 +- 0.065 | 0 +- 0.065 | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |
| GdokleftOnce | 0 +- 0.019 | 0 +- 0.013 | 0 +- 0.065 | 0 +- 0.065 | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |
| DoksdConsistent | 0 +- 0.019 | 0 +- 0.013 | 0 +- 0.065 | 0 +- 0.065 | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |
| CategoriesOnCopy | 0 +- 0.019 | 0 +- 0.013 | 0 +- 0.065 | 0 +- 0.065 | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | +2.905 +- 0.283* | n/a |
| CategoriesLastRowRecomputed | 0 +- 0.019 | 0 +- 0.013 | 0 +- 0.065 | 0 +- 0.065 | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |
| CategoriesEmptyRowsMerged | 0 +- 0.019 | 0 +- 0.013 | 0 +- 0.065 | 0 +- 0.065 | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |
| PdoksmallFirstCell | 0 +- 0.019 | 0 +- 0.013 | 0 +- 0.065 | 0 +- 0.065 | 0 +- 0.020 | 0 +- 5.120e-15 | 0 +- 7.227e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.401 | n/a |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +2.3 (+15.6) | +3.0 (+31.5) | +23.0 (+67.1) | +23.0 (+67.1) | +2.1 (+13.7) | -1145108022945.9 (-inf) | -509.9 (-1639.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -36.6 (-36.6) | n/a |
| ExactConstants | +2.3 (+15.6) | +3.0 (+31.5) | -1.0 (-6.2) | -1.0 (-6.2) | +2.1 (+13.7) | -1145108022945.9 (-inf) | +2.1 (+6.7) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.9 (+1.5) | n/a |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | -185.2 (-642.3) | -185.2 (-642.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| Fmkarm2OnAcceptance | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +205.7 (+838.2) | +205.7 (+838.2) | 0.0 (0.0) | 0.0 (0.0) | -538.7 (-1425.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +10.2 (+10.2) | n/a |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 2 / 2147 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-PSAN02n.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 576 / 2050 | fqmkm2 (80), fmkarm_cor2 (29), Dkarmcat (27), dokkarm10 (27), dokkarm43 (27), fqdokkarm(10,:) (16), fqdokkarm(11,:) (16), fqdokkarm(12,:) (16), and 32 more |
| ExactConstants | 2 / 2194 | da_coef (1), fqdokkarm(31,:) (1) |
| BridgeFrustum | 7 / 2194 | epszkarm_n (4), Zkarm_cor (2), Zkarm (1) |
| Fmkarm2OnAcceptance | 32 / 2194 | fmkarm_cor2 (29), Dagg43_cor(2) (1), Dkarm43_cor(2) (1), Dkarm43sd_cor(2) (1) |
| WindowLowerEdge | 88 / 2194 | fqmkm2 (80), epszkarm_n (4), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 2194 | none |
| ZdoksmallCoarseShare | 0 / 2194 | none |
| GdokleftOnce | 0 / 2194 | none |
| DoksdConsistent | 0 / 2194 | none |
| CategoriesOnCopy | 43 / 2194 | fqdokkarm(6,:) (10), fqdokkarm(7,:) (10), fqdokkarm(5,:) (7), dokkarm43 (5), fqdokkarm(8,:) (5), dokkarm10 (4), Dkarmcat (1), fqdokkarm(31,:) (1) |
| CategoriesLastRowRecomputed | 14 / 2194 | fqdokkarm(31,:) (13), dokkarm10 (1) |
| CategoriesEmptyRowsMerged | 98 / 2194 | fqdokkarm(4,:) (16), fqdokkarm(5,:) (16), fqdokkarm(6,:) (16), fqdokkarm(3,:) (14), fqdokkarm(2,:) (10), fqdokkarm(1,:) (7), dokkarm10 (6), dokkarm43 (6), and 2 more |
| PdoksmallFirstCell | 0 / 2194 | none |

