## HPEPA3

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..15 (16 seeds); 1 cycle(s) of 100000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 908 s over 14 arms (Original 80, All 73, ExactConstants 69, BridgeFrustum 64, Fmkarm2OnAcceptance 59, WindowLowerEdge 63, VdokTotalOnCopy 59, ZdoksmallCoarseShare 60, GdokleftOnce 59, DoksdConsistent 60, CategoriesOnCopy 71, CategoriesLastRowRecomputed 71, CategoriesEmptyRowsMerged 60, PdoksmallFirstCell 61 s).

Failed runs: none.

Builds behind the runs: 2 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 223.936 | 1.478 | 5.912 |
| Dkarm10 | 34.805 | 8.660e-3 | 0.035 |
| Zkarm | 0.585 | 1.291e-3 | 5.162e-3 |
| Zkarm_cor | 0.585 | 1.291e-3 | 5.162e-3 |
| Dagg43 | 165.478 | 1.092 | 4.368 |
| da_coef | 0.739 | 2.867e-17 | 1.147e-16 |
| Dqmkm2 | 0.637 | 1.250e-5 | 5.000e-5 |
| Dok43sd | 108.600 | 7.338e-15 | 2.935e-14 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 17.625 | 0.221 | 0.885 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.090 +- 0.932 | +0.050 +- 0.035 | +8.945 +- 0.329* | +8.945 +- 0.329* | +0.080 +- 0.931 | -9.960e-3 +- 3.879e-15* | -6.944 +- 5.243e-3* | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | +21.986 +- 1.785* | [4] +0.024 +- 0.044 |
| ExactConstants | +0.090 +- 0.932 | +0.050 +- 0.035 | -0.024 +- 0.312 | -0.024 +- 0.312 | +0.080 +- 0.931 | -9.960e-3 +- 3.879e-15* | +9.816e-3 +- 3.475e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [4] +0.024 +- 0.044 |
| BridgeFrustum | 0 +- 0.933 | 0 +- 0.035 | -3.406 +- 0.323* | -3.406 +- 0.323* | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |
| Fmkarm2OnAcceptance | 0 +- 0.933 | 0 +- 0.035 | 0 +- 0.312 | 0 +- 0.312 | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |
| WindowLowerEdge | 0 +- 0.933 | 0 +- 0.035 | +11.808 +- 0.323* | +11.808 +- 0.323* | 0 +- 0.933 | 0 +- 5.486e-15 | -6.951 +- 3.893e-3* | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |
| VdokTotalOnCopy | 0 +- 0.933 | 0 +- 0.035 | 0 +- 0.312 | 0 +- 0.312 | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |
| ZdoksmallCoarseShare | 0 +- 0.933 | 0 +- 0.035 | 0 +- 0.312 | 0 +- 0.312 | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |
| GdokleftOnce | 0 +- 0.933 | 0 +- 0.035 | 0 +- 0.312 | 0 +- 0.312 | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |
| DoksdConsistent | 0 +- 0.933 | 0 +- 0.035 | 0 +- 0.312 | 0 +- 0.312 | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |
| CategoriesOnCopy | 0 +- 0.933 | 0 +- 0.035 | 0 +- 0.312 | 0 +- 0.312 | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | +21.986 +- 1.785* | [2] 0 +- 0.074 |
| CategoriesLastRowRecomputed | 0 +- 0.933 | 0 +- 0.035 | 0 +- 0.312 | 0 +- 0.312 | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |
| CategoriesEmptyRowsMerged | 0 +- 0.933 | 0 +- 0.035 | 0 +- 0.312 | 0 +- 0.312 | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |
| PdoksmallFirstCell | 0 +- 0.933 | 0 +- 0.035 | 0 +- 0.312 | 0 +- 0.312 | 0 +- 0.933 | 0 +- 5.486e-15 | 0 +- 2.776e-3 | 0 +- 9.556e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 1.775 | [2] 0 +- 0.074 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.1 (+6.0) | +1.4 (+15.7) | +27.2 (+130.5) | +27.2 (+130.5) | +0.1 (+5.4) | -2567516327429.5 (-inf) | -1324.3 (-1625.7) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +12.3 (+25.0) | +0.5 (+1.5) |
| ExactConstants | +0.1 (+6.0) | +1.4 (+15.7) | -0.1 (-1.5) | -0.1 (-1.5) | +0.1 (+5.4) | -2567516327429.5 (-inf) | +2.8 (+5.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.5 (+1.5) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | -10.6 (-123.8) | -10.6 (-123.8) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| Fmkarm2OnAcceptance | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +36.5 (+148.9) | +36.5 (+148.9) | 0.0 (0.0) | 0.0 (0.0) | -1785.4 (-2463.1) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +12.3 (+25.0) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 0 / 2109 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-HPEPA3.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 190 / 2163 | fqmkm2 (90), fqdokkarm(9,:) (19), fqdokkarm(8,:) (18), fqdokkarm(10,:) (17), fqdokkarm(19,:) (11), fmkarm_cor2 (7), Dkarmcat (5), dokkarm43 (4), and 12 more |
| ExactConstants | 22 / 2164 | fqdokkarm(19,:) (13), fmkarm_cor (3), fqkarm_cor (3), da_coef (1), fmkarm (1), fmkarm_cor2 (1) |
| BridgeFrustum | 2 / 2164 | Zkarm (1), Zkarm_cor (1) |
| Fmkarm2OnAcceptance | 10 / 2164 | fmkarm_cor2 (7), Dagg43_cor(2) (1), Dkarm43_cor(2) (1), Dkarm43sd_cor(2) (1) |
| WindowLowerEdge | 96 / 2163 | fqmkm2 (90), Zkarm_cor (2), Dqmkm2 (1), NFQ (1), Zkarm (1), fmkarm_cor2 (1) |
| VdokTotalOnCopy | 0 / 2164 | none |
| ZdoksmallCoarseShare | 0 / 2164 | none |
| GdokleftOnce | 0 / 2164 | none |
| DoksdConsistent | 0 / 2164 | none |
| CategoriesOnCopy | 74 / 2164 | fqdokkarm(9,:) (19), fqdokkarm(8,:) (18), fqdokkarm(10,:) (17), fqdokkarm(19,:) (8), Dkarmcat (5), dokkarm43 (4), dokkarm10 (3) |
| CategoriesLastRowRecomputed | 18 / 2164 | fqdokkarm(19,:) (16), dokkarm10 (1), dokkarm43 (1) |
| CategoriesEmptyRowsMerged | 0 / 2164 | none |
| PdoksmallFirstCell | 0 / 2164 | none |

