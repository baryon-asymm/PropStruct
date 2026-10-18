## P312-Dmin5um

P312 at Dmin 5 um, particles per cycle reduced from 5000 to 100. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..7 (8 seeds); 1 cycle(s) of 100 particles; `Dmin` 5 um, `Di` 10 um, `Dj` 10 um.

Work: 8 s over 14 arms (Original 6, All 0, ExactConstants 0, BridgeFrustum 0, Fmkarm2OnAcceptance 0, WindowLowerEdge 0, VdokTotalOnCopy 0, ZdoksmallCoarseShare 0, GdokleftOnce 0, DoksdConsistent 0, CategoriesOnCopy 0, CategoriesLastRowRecomputed 0, CategoriesEmptyRowsMerged 0, PdoksmallFirstCell 0 s).

Failed runs: none.

Builds behind the runs: 2 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 40.684 | 7.997 | 22.620 |
| Dkarm10 | 23.624 | 0.075 | 0.212 |
| Zkarm | 0.244 | 9.701e-3 | 0.027 |
| Zkarm_cor | 0.244 | 9.701e-3 | 0.027 |
| Dagg43 | 27.951 | 5.494 | 15.540 |
| da_coef | 0.687 | 0 | 0 |
| Dqmkm2 | 0.705 | 1.979e-3 | 5.599e-3 |
| Dok43sd | 67.740 | 0 | 0 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 4.750 | 0.590 | 1.669 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | -0.197 +- 27.691 | -0.021 +- 0.440 | +28.774 +- 5.729* | +28.774 +- 5.729* | -0.210 +- 27.689 | -0.011 +- 0* | -6.083 +- 0.466* | 0 +- 0 | abs 0 +- 0 | abs +0.142 +- 5.355e-3* | -5.263 +- 16.283 | [1] +0.047 +- 4.695 |
| ExactConstants | -0.197 +- 27.691 | -0.021 +- 0.440 | +0.452 +- 5.684 | +0.452 +- 5.684 | -0.210 +- 27.689 | -0.011 +- 0* | -3.545e-3 +- 0.401 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] +0.047 +- 4.695 |
| BridgeFrustum | 0 +- 27.800 | 0 +- 0.449 | -15.623 +- 5.557 | -15.623 +- 5.557 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] 0 +- 4.504 |
| Fmkarm2OnAcceptance | 0 +- 27.800 | 0 +- 0.449 | 0 +- 5.613 | 0 +- 5.613 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] 0 +- 4.504 |
| WindowLowerEdge | 0 +- 27.800 | 0 +- 0.449 | +41.681 +- 5.511* | +41.681 +- 5.511* | 0 +- 27.798 | 0 +- 0 | -6.058 +- 0.441* | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] 0 +- 4.504 |
| VdokTotalOnCopy | 0 +- 27.800 | 0 +- 0.449 | 0 +- 5.613 | 0 +- 5.613 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] 0 +- 4.504 |
| ZdoksmallCoarseShare | 0 +- 27.800 | 0 +- 0.449 | 0 +- 5.613 | 0 +- 5.613 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] 0 +- 4.504 |
| GdokleftOnce | 0 +- 27.800 | 0 +- 0.449 | 0 +- 5.613 | 0 +- 5.613 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] 0 +- 4.504 |
| DoksdConsistent | 0 +- 27.800 | 0 +- 0.449 | 0 +- 5.613 | 0 +- 5.613 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] 0 +- 4.504 |
| CategoriesOnCopy | 0 +- 27.800 | 0 +- 0.449 | 0 +- 5.613 | 0 +- 5.613 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 13.492 | [1] 0 +- 4.504 |
| CategoriesLastRowRecomputed | 0 +- 27.800 | 0 +- 0.449 | 0 +- 5.613 | 0 +- 5.613 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] 0 +- 4.504 |
| CategoriesEmptyRowsMerged | 0 +- 27.800 | 0 +- 0.449 | 0 +- 5.613 | 0 +- 5.613 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 17.569 | [1] 0 +- 4.504 |
| PdoksmallFirstCell | 0 +- 27.800 | 0 +- 0.449 | 0 +- 5.613 | 0 +- 5.613 | 0 +- 27.798 | 0 +- 0 | 0 +- 0.397 | 0 +- 0 | abs 0 +- 0 | abs +0.142 +- 4.875e-3* | 0 +- 17.569 | [1] 0 +- 4.504 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | 0.0 (-1.2) | 0.0 (-0.4) | +5.0 (+22.2) | +5.0 (+22.2) | 0.0 (-1.3) | -inf (-inf) | -13.0 (-29.3) | 0.0 (0.0) | 0.0 (0.0) | +26.5 (+26.5) | -0.3 (-1.0) | 0.0 (+0.1) |
| ExactConstants | 0.0 (-1.2) | 0.0 (-0.4) | +0.1 (+0.7) | +0.1 (+0.7) | 0.0 (-1.3) | -inf (-inf) | 0.0 (-0.1) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (+0.1) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | -2.8 (-15.6) | -2.8 (-15.6) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| Fmkarm2OnAcceptance | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +7.6 (+98.2) | +7.6 (+98.2) | 0.0 (0.0) | 0.0 (0.0) | -13.8 (-40.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +29.1 (+29.1) | 0.0 (0.0) | 0.0 (0.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (4) against odd seeds (4): 0 / 1234 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-P312-Dmin5um.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 4 / 1233 | Dkarmcat (1), Dqmkm2 (1), da_coef (1), pdoksmall (1) |
| ExactConstants | 1 / 1258 | da_coef (1) |
| BridgeFrustum | 0 / 1259 | none |
| Fmkarm2OnAcceptance | 0 / 1259 | none |
| WindowLowerEdge | 1 / 1257 | Dqmkm2 (1) |
| VdokTotalOnCopy | 0 / 1259 | none |
| ZdoksmallCoarseShare | 0 / 1259 | none |
| GdokleftOnce | 0 / 1259 | none |
| DoksdConsistent | 0 / 1259 | none |
| CategoriesOnCopy | 1 / 1236 | Dkarmcat (1) |
| CategoriesLastRowRecomputed | 0 / 1259 | none |
| CategoriesEmptyRowsMerged | 0 / 1259 | none |
| PdoksmallFirstCell | 1 / 1259 | pdoksmall (1) |

