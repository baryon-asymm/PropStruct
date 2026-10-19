## inpt

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^16`, k = 0..15 (16 seeds); 20 cycle(s) of 1000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 57 s over 14 arms (Original 4, All 4, ExactConstants 4, BridgeFrustum 4, WindowLowerEdge 4, VdokTotalOnCopy 4, ZdoksmallCoarseShare 4, GdokleftOnce 4, DoksdConsistent 4, CategoriesOnCopy 4, CategoriesLastRowRecomputed 4, PdoksmallFirstCell 4, AllButCategoriesOnCopy 4, AllButCategoriesLastRowRecomputed 4 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 215.075 | 0.065 | 0.261 |
| Dkarm43_cor(1) | 215.083 | 0.066 | 0.264 |
| Dkarm43_cor(2) | 215.083 | 0.066 | 0.264 |
| Dkarm10 | 160.753 | 0.024 | 0.098 |
| Zkarm | 0.500 | 2.601e-4 | 1.040e-3 |
| Zkarm_cor | 0.500 | 2.601e-4 | 1.040e-3 |
| Dagg43 | 167.349 | 0.051 | 0.202 |
| Dagg43_cor(1) | 167.354 | 0.051 | 0.206 |
| Dagg43_cor(2) | 167.354 | 0.051 | 0.206 |
| da_coef | 0.778 | 5.733e-17 | 2.293e-16 |
| Dqmkm2 | 0.520 | 3.860e-5 | 1.544e-4 |
| Dok43sd | 24.830 | 1.835e-15 | 7.338e-15 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 37.563 | 0.128 | 0.512 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.049 +- 0.043 | +0.049 +- 0.043 | +0.049 +- 0.043 | +0.042 +- 0.022 | -4.586 +- 0.080* | -4.586 +- 0.080* | +0.040 +- 0.043 | +0.040 +- 0.043 | +0.040 +- 0.043 | -9.433e-3 +- 8.238e-15* | -2.793 +- 0.011* | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | +1.165 +- 0.341 | n/a |
| ExactConstants | +0.049 +- 0.043 | +0.049 +- 0.043 | +0.049 +- 0.043 | +0.042 +- 0.022 | -0.074 +- 0.071 | -0.074 +- 0.071 | +0.040 +- 0.043 | +0.040 +- 0.043 | +0.040 +- 0.043 | -9.433e-3 +- 8.238e-15* | +0.011 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | -0.166 +- 0.484 | n/a |
| BridgeFrustum | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.022 | -12.434 +- 0.076* | -12.434 +- 0.076* | 0 +- 0.043 | 0 +- 0.044 | 0 +- 0.044 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.482 | n/a |
| WindowLowerEdge | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.022 | +7.128 +- 0.078* | +7.128 +- 0.078* | 0 +- 0.043 | 0 +- 0.044 | 0 +- 0.044 | 0 +- 1.042e-14 | -2.805 +- 0.011* | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.482 | n/a |
| VdokTotalOnCopy | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.022 | 0 +- 0.074 | 0 +- 0.074 | 0 +- 0.043 | 0 +- 0.044 | 0 +- 0.044 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.482 | n/a |
| ZdoksmallCoarseShare | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.022 | 0 +- 0.074 | 0 +- 0.074 | 0 +- 0.043 | 0 +- 0.044 | 0 +- 0.044 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.482 | n/a |
| GdokleftOnce | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.022 | 0 +- 0.074 | 0 +- 0.074 | 0 +- 0.043 | 0 +- 0.044 | 0 +- 0.044 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.482 | n/a |
| DoksdConsistent | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.022 | 0 +- 0.074 | 0 +- 0.074 | 0 +- 0.043 | 0 +- 0.044 | 0 +- 0.044 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.482 | n/a |
| CategoriesOnCopy | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.022 | 0 +- 0.074 | 0 +- 0.074 | 0 +- 0.043 | 0 +- 0.044 | 0 +- 0.044 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | +1.165 +- 0.341 | n/a |
| CategoriesLastRowRecomputed | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.022 | 0 +- 0.074 | 0 +- 0.074 | 0 +- 0.043 | 0 +- 0.044 | 0 +- 0.044 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.482 | n/a |
| PdoksmallFirstCell | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.043 | 0 +- 0.022 | 0 +- 0.074 | 0 +- 0.074 | 0 +- 0.043 | 0 +- 0.044 | 0 +- 0.044 | 0 +- 1.042e-14 | 0 +- 0.010 | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.482 | n/a |
| AllButCategoriesOnCopy | +0.049 +- 0.043 | +0.049 +- 0.043 | +0.049 +- 0.043 | +0.042 +- 0.022 | -4.586 +- 0.080* | -4.586 +- 0.080* | +0.040 +- 0.043 | +0.040 +- 0.043 | +0.040 +- 0.043 | -9.433e-3 +- 8.238e-15* | -2.793 +- 0.011* | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | -0.166 +- 0.484 | n/a |
| AllButCategoriesLastRowRecomputed | +0.049 +- 0.043 | +0.049 +- 0.043 | +0.049 +- 0.043 | +0.042 +- 0.022 | -4.586 +- 0.080* | -4.586 +- 0.080* | +0.040 +- 0.043 | +0.040 +- 0.043 | +0.040 +- 0.043 | -9.433e-3 +- 8.238e-15* | -2.793 +- 0.011* | 0 +- 1.045e-14 | abs 0 +- 0 | abs 0 +- 0 | +1.165 +- 0.341 | n/a |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +1.2 (+10.0) | +1.1 (+9.6) | +1.1 (+9.6) | +2.0 (+15.9) | -57.7 (-152.9) | -57.7 (-152.9) | +0.9 (+8.5) | +0.9 (+8.5) | +0.9 (+8.5) | -1145108022942.4 (-inf) | -246.3 (-850.5) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +3.4 (+3.4) | n/a |
| ExactConstants | +1.2 (+10.0) | +1.1 (+9.6) | +1.1 (+9.6) | +2.0 (+15.9) | -1.0 (-5.8) | -1.0 (-5.8) | +0.9 (+8.5) | +0.9 (+8.5) | +0.9 (+8.5) | -1145108022942.4 (-inf) | +1.1 (+4.4) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -0.3 (-1.0) | n/a |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -164.1 (-569.7) | -164.1 (-569.7) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +91.1 (+427.0) | +91.1 (+427.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -256.3 (-942.4) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +3.4 (+3.4) | n/a |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| AllButCategoriesOnCopy | +1.2 (+10.0) | +1.1 (+9.6) | +1.1 (+9.6) | +2.0 (+15.9) | -57.7 (-152.9) | -57.7 (-152.9) | +0.9 (+8.5) | +0.9 (+8.5) | +0.9 (+8.5) | -1145108022942.4 (-inf) | -246.3 (-850.5) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -0.3 (-1.0) | n/a |
| AllButCategoriesLastRowRecomputed | +1.2 (+10.0) | +1.1 (+9.6) | +1.1 (+9.6) | +2.0 (+15.9) | -57.7 (-152.9) | -57.7 (-152.9) | +0.9 (+8.5) | +0.9 (+8.5) | +0.9 (+8.5) | -1145108022942.4 (-inf) | -246.3 (-850.5) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +3.4 (+3.4) | n/a |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 0 / 2634 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-inpt.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 103 / 2643 | fqmkm2 (73), epszkarm_n (20), fqdokkarm(8,:) (3), Zkarm_cor (2), fqdokkarm(9,:) (2), Dqmkm2 (1), Zkarm (1), da_coef (1) |
| ExactConstants | 1 / 2643 | da_coef (1) |
| BridgeFrustum | 23 / 2643 | epszkarm_n (20), Zkarm_cor (2), Zkarm (1) |
| WindowLowerEdge | 98 / 2643 | fqmkm2 (74), epszkarm_n (20), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 2643 | none |
| ZdoksmallCoarseShare | 0 / 2643 | none |
| GdokleftOnce | 0 / 2643 | none |
| DoksdConsistent | 0 / 2643 | none |
| CategoriesOnCopy | 5 / 2643 | fqdokkarm(8,:) (3), fqdokkarm(9,:) (2) |
| CategoriesLastRowRecomputed | 0 / 2643 | none |
| PdoksmallFirstCell | 0 / 2643 | none |
| AllButCategoriesOnCopy | 98 / 2643 | fqmkm2 (73), epszkarm_n (20), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1), da_coef (1) |
| AllButCategoriesLastRowRecomputed | 103 / 2643 | fqmkm2 (73), epszkarm_n (20), fqdokkarm(8,:) (3), Zkarm_cor (2), fqdokkarm(9,:) (2), Dqmkm2 (1), Zkarm (1), da_coef (1) |

