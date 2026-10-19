## PSAN02n

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^16`, k = 0..15 (16 seeds); 4 cycle(s) of 20000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 211 s over 14 arms (Original 15, All 15, ExactConstants 15, BridgeFrustum 15, WindowLowerEdge 15, VdokTotalOnCopy 15, ZdoksmallCoarseShare 15, GdokleftOnce 15, DoksdConsistent 15, CategoriesOnCopy 15, CategoriesLastRowRecomputed 15, PdoksmallFirstCell 15, AllButCategoriesOnCopy 15, AllButCategoriesLastRowRecomputed 15 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 162.993 | 0.020 | 0.080 |
| Dkarm43_cor(1) | 162.996 | 0.019 | 0.075 |
| Dkarm43_cor(2) | 162.996 | 0.019 | 0.075 |
| Dkarm10 | 122.255 | 9.174e-3 | 0.037 |
| Zkarm | 0.407 | 1.606e-4 | 6.424e-4 |
| Zkarm_cor | 0.407 | 1.606e-4 | 6.424e-4 |
| Dagg43 | 129.052 | 0.016 | 0.064 |
| Dagg43_cor(1) | 129.056 | 0.015 | 0.060 |
| Dagg43_cor(2) | 129.056 | 0.015 | 0.060 |
| da_coef | 0.792 | 2.867e-17 | 1.147e-16 |
| Dqmkm2 | 0.505 | 2.141e-5 | 8.563e-5 |
| Dok43sd | 28.480 | 1.835e-15 | 7.338e-15 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 30.063 | 0.063 | 0.250 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.048 +- 0.017 | +0.047 +- 0.016 | +0.047 +- 0.016 | +0.042 +- 0.010* | +1.537 +- 0.057* | +1.537 +- 0.057* | +0.042 +- 0.017 | +0.037 +- 0.016 | +0.037 +- 0.016 | -9.270e-3 +- 8.096e-15* | -3.629 +- 6.093e-3* | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | +3.119 +- 0.208* | n/a |
| ExactConstants | +0.048 +- 0.017 | +0.047 +- 0.016 | +0.047 +- 0.016 | +0.042 +- 0.010* | -0.079 +- 0.059 | -0.079 +- 0.059 | +0.042 +- 0.017 | +0.037 +- 0.016 | +0.037 +- 0.016 | -9.270e-3 +- 8.096e-15* | +0.011 +- 6.051e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | +0.208 +- 0.352 | n/a |
| BridgeFrustum | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 0.011 | -12.828 +- 0.058* | -12.828 +- 0.058* | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 5.120e-15 | 0 +- 5.992e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.294 | n/a |
| WindowLowerEdge | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 0.011 | +13.402 +- 0.054* | +13.402 +- 0.054* | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 5.120e-15 | -3.645 +- 5.846e-3* | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.294 | n/a |
| VdokTotalOnCopy | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 0.011 | 0 +- 0.056 | 0 +- 0.056 | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 5.120e-15 | 0 +- 5.992e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.294 | n/a |
| ZdoksmallCoarseShare | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 0.011 | 0 +- 0.056 | 0 +- 0.056 | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 5.120e-15 | 0 +- 5.992e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.294 | n/a |
| GdokleftOnce | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 0.011 | 0 +- 0.056 | 0 +- 0.056 | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 5.120e-15 | 0 +- 5.992e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.294 | n/a |
| DoksdConsistent | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 0.011 | 0 +- 0.056 | 0 +- 0.056 | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 5.120e-15 | 0 +- 5.992e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.294 | n/a |
| CategoriesOnCopy | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 0.011 | 0 +- 0.056 | 0 +- 0.056 | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 5.120e-15 | 0 +- 5.992e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | +3.119 +- 0.208* | n/a |
| CategoriesLastRowRecomputed | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 0.011 | 0 +- 0.056 | 0 +- 0.056 | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 5.120e-15 | 0 +- 5.992e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.294 | n/a |
| PdoksmallFirstCell | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 0.011 | 0 +- 0.056 | 0 +- 0.056 | 0 +- 0.017 | 0 +- 0.016 | 0 +- 0.016 | 0 +- 5.120e-15 | 0 +- 5.992e-3 | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0.294 | n/a |
| AllButCategoriesOnCopy | +0.048 +- 0.017 | +0.047 +- 0.016 | +0.047 +- 0.016 | +0.042 +- 0.010* | +1.537 +- 0.057* | +1.537 +- 0.057* | +0.042 +- 0.017 | +0.037 +- 0.016 | +0.037 +- 0.016 | -9.270e-3 +- 8.096e-15* | -3.629 +- 6.093e-3* | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | +0.208 +- 0.352 | n/a |
| AllButCategoriesLastRowRecomputed | +0.048 +- 0.017 | +0.047 +- 0.016 | +0.047 +- 0.016 | +0.042 +- 0.010* | +1.537 +- 0.057* | +1.537 +- 0.057* | +0.042 +- 0.017 | +0.037 +- 0.016 | +0.037 +- 0.016 | -9.270e-3 +- 8.096e-15* | -3.629 +- 6.093e-3* | 0 +- 9.110e-15 | abs 0 +- 0 | abs 0 +- 0 | +3.119 +- 0.208* | n/a |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +2.8 (+13.3) | +3.0 (+14.1) | +3.0 (+14.1) | +4.0 (+27.7) | +27.1 (+63.1) | +27.1 (+63.1) | +2.5 (+11.2) | +2.4 (+10.5) | +2.4 (+10.5) | -1145108022945.9 (-inf) | -595.6 (-1467.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +15.0 (+15.0) | n/a |
| ExactConstants | +2.8 (+13.3) | +3.0 (+14.1) | +3.0 (+14.1) | +4.0 (+27.7) | -1.3 (-6.3) | -1.3 (-6.3) | +2.5 (+11.2) | +2.4 (+10.5) | +2.4 (+10.5) | -1145108022945.9 (-inf) | +1.8 (+4.4) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.6 (+1.0) | n/a |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -221.8 (-750.2) | -221.8 (-750.2) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +247.8 (+812.0) | +247.8 (+812.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -623.6 (-1354.6) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +15.0 (+15.0) | n/a |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | n/a |
| AllButCategoriesOnCopy | +2.8 (+13.3) | +3.0 (+14.1) | +3.0 (+14.1) | +4.0 (+27.7) | +27.1 (+63.1) | +27.1 (+63.1) | +2.5 (+11.2) | +2.4 (+10.5) | +2.4 (+10.5) | -1145108022945.9 (-inf) | -595.6 (-1467.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.6 (+1.0) | n/a |
| AllButCategoriesLastRowRecomputed | +2.8 (+13.3) | +3.0 (+14.1) | +3.0 (+14.1) | +4.0 (+27.7) | +27.1 (+63.1) | +27.1 (+63.1) | +2.5 (+11.2) | +2.4 (+10.5) | +2.4 (+10.5) | -1145108022945.9 (-inf) | -595.6 (-1467.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +15.0 (+15.0) | n/a |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 0 / 2143 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-PSAN02n.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 128 / 2157 | fqmkm2 (79), fqdokkarm(7,:) (11), fqdokkarm(6,:) (10), fqdokkarm(5,:) (7), fqdokkarm(8,:) (5), epszkarm_n (4), dokkarm10 (3), dokkarm43 (3), and 5 more |
| ExactConstants | 1 / 2157 | da_coef (1) |
| BridgeFrustum | 7 / 2157 | epszkarm_n (4), Zkarm_cor (2), Zkarm (1) |
| WindowLowerEdge | 87 / 2157 | fqmkm2 (79), epszkarm_n (4), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 2157 | none |
| ZdoksmallCoarseShare | 0 / 2157 | none |
| GdokleftOnce | 0 / 2157 | none |
| DoksdConsistent | 0 / 2157 | none |
| CategoriesOnCopy | 40 / 2157 | fqdokkarm(7,:) (11), fqdokkarm(6,:) (10), fqdokkarm(5,:) (7), fqdokkarm(8,:) (5), dokkarm10 (3), dokkarm43 (3), Dkarmcat (1) |
| CategoriesLastRowRecomputed | 0 / 2157 | none |
| PdoksmallFirstCell | 0 / 2157 | none |
| AllButCategoriesOnCopy | 88 / 2157 | fqmkm2 (79), epszkarm_n (4), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1), da_coef (1) |
| AllButCategoriesLastRowRecomputed | 128 / 2157 | fqmkm2 (79), fqdokkarm(7,:) (11), fqdokkarm(6,:) (10), fqdokkarm(5,:) (7), fqdokkarm(8,:) (5), epszkarm_n (4), dokkarm10 (3), dokkarm43 (3), and 5 more |

