## HMX

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..15 (16 seeds); 1 cycle(s) of 10000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 1905 s over 14 arms (Original 134, All 155, ExactConstants 149, BridgeFrustum 120, Fmkarm2OnAcceptance 123, WindowLowerEdge 123, VdokTotalOnCopy 140, ZdoksmallCoarseShare 124, GdokleftOnce 150, DoksdConsistent 138, CategoriesOnCopy 125, CategoriesLastRowRecomputed 153, CategoriesEmptyRowsMerged 150, PdoksmallFirstCell 120 s).

Failed runs: none.

Builds behind the runs: 2 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 309.809 | 0.525 | 2.101 |
| Dkarm10 | 55.083 | 0.012 | 0.049 |
| Zkarm | 0.583 | 8.250e-4 | 3.300e-3 |
| Zkarm_cor | 0.584 | 7.797e-4 | 3.119e-3 |
| Dagg43 | 263.093 | 0.446 | 1.784 |
| da_coef | 0.849 | 0 | 0 |
| Dqmkm2 | 0.577 | 5.907e-5 | 2.363e-4 |
| Dok43sd | 162.430 | 0 | 0 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 59.000 | 2.507 | 10.027 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.087 +- 0.240 | +0.027 +- 0.031 | +2.314 +- 0.195* | +2.422 +- 0.189* | +0.077 +- 0.240 | -8.573e-3 +- 3.376e-15* | -4.821 +- 0.016* | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +19.492 +- 5.672 | [27] -0.021 +- 0.024 |
| ExactConstants | +0.087 +- 0.240 | +0.027 +- 0.031 | -0.050 +- 0.199 | -0.046 +- 0.184 | +0.077 +- 0.240 | -8.573e-3 +- 3.376e-15* | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +2.331 +- 5.855 | [27] -0.021 +- 0.024 |
| BridgeFrustum | 0 +- 0.240 | 0 +- 0.031 | -2.722 +- 0.199* | -2.804 +- 0.190* | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 6.008 | [2] 0 +- 1.306e-14 |
| Fmkarm2OnAcceptance | 0 +- 0.240 | 0 +- 0.031 | 0 +- 0.200 | 0 +- 0.189 | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 6.008 | [2] 0 +- 1.306e-14 |
| WindowLowerEdge | 0 +- 0.240 | 0 +- 0.031 | +4.862 +- 0.197* | +5.037 +- 0.189* | 0 +- 0.240 | 0 +- 0 | -4.824 +- 0.015* | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 6.008 | [2] 0 +- 1.306e-14 |
| VdokTotalOnCopy | 0 +- 0.240 | 0 +- 0.031 | 0 +- 0.200 | 0 +- 0.189 | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 6.008 | [2] 0 +- 1.306e-14 |
| ZdoksmallCoarseShare | 0 +- 0.240 | 0 +- 0.031 | 0 +- 0.200 | 0 +- 0.189 | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 6.008 | [2] 0 +- 1.306e-14 |
| GdokleftOnce | 0 +- 0.240 | 0 +- 0.031 | 0 +- 0.200 | 0 +- 0.189 | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 6.008 | [2] 0 +- 1.306e-14 |
| DoksdConsistent | 0 +- 0.240 | 0 +- 0.031 | 0 +- 0.200 | 0 +- 0.189 | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 6.008 | [2] 0 +- 1.306e-14 |
| CategoriesOnCopy | 0 +- 0.240 | 0 +- 0.031 | 0 +- 0.200 | 0 +- 0.189 | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +20.551 +- 5.836 | [2] 0 +- 1.306e-14 |
| CategoriesLastRowRecomputed | 0 +- 0.240 | 0 +- 0.031 | 0 +- 0.200 | 0 +- 0.189 | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | -0.212 +- 6.042 | [2] 0 +- 1.306e-14 |
| CategoriesEmptyRowsMerged | 0 +- 0.240 | 0 +- 0.031 | 0 +- 0.200 | 0 +- 0.189 | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | -0.424 +- 5.999 | [2] 0 +- 1.306e-14 |
| PdoksmallFirstCell | 0 +- 0.240 | 0 +- 0.031 | 0 +- 0.200 | 0 +- 0.189 | 0 +- 0.240 | 0 +- 0 | 0 +- 0.014 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 6.008 | [2] 0 +- 1.306e-14 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.4 (+24.5) | +0.9 (+11.6) | +11.8 (+71.1) | +12.8 (+78.8) | +0.3 (+21.6) | -2539608541253.3 (-inf) | -304.9 (-1796.8) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +3.4 (+10.4) | -0.9 (-1.5) |
| ExactConstants | +0.4 (+24.5) | +0.9 (+11.6) | -0.3 (-1.4) | -0.3 (-1.2) | +0.3 (+21.6) | -2539608541253.3 (-inf) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.4 (+1.8) | -0.9 (-1.5) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | -13.7 (-258.8) | -14.7 (-247.8) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| Fmkarm2OnAcceptance | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +24.7 (+135.5) | +26.6 (+118.5) | 0.0 (0.0) | 0.0 (0.0) | -311.5 (-2488.7) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +3.5 (+12.2) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (-1.5) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -0.1 (-2.2) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 9 / 6938 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-HMX.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 1295 / 7779 | fqmkm2 (94), fqdokkarm(25,:) (51), fqdokkarm(23,:) (49), fqdokkarm(26,:) (48), fqdokkarm(24,:) (45), fqdokkarm(28,:) (43), fqdokkarm(22,:) (42), fqdokkarm(27,:) (42), and 46 more |
| ExactConstants | 8 / 7706 | Dkarmcat (7), da_coef (1) |
| BridgeFrustum | 3 / 7780 | Zkarm_cor (2), Zkarm (1) |
| Fmkarm2OnAcceptance | 24 / 7780 | fmkarm_cor2 (22), Dagg43_cor(2) (1), Dkarm43_cor(2) (1) |
| WindowLowerEdge | 98 / 7779 | fqmkm2 (94), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 7780 | none |
| ZdoksmallCoarseShare | 0 / 7780 | none |
| GdokleftOnce | 0 / 7780 | none |
| DoksdConsistent | 0 / 7780 | none |
| CategoriesOnCopy | 1175 / 7780 | fqdokkarm(25,:) (51), fqdokkarm(23,:) (49), fqdokkarm(26,:) (48), fqdokkarm(24,:) (45), fqdokkarm(28,:) (43), fqdokkarm(22,:) (42), fqdokkarm(27,:) (42), fqdokkarm(35,:) (42), and 38 more |
| CategoriesLastRowRecomputed | 0 / 7780 | none |
| CategoriesEmptyRowsMerged | 0 / 7780 | none |
| PdoksmallFirstCell | 0 / 7780 | none |

