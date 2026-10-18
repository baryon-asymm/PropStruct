## HMX

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^16`, k = 0..15 (16 seeds); 1 cycle(s) of 10000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 2039 s over 16 arms (Original 127, All 126, ExactConstants 125, BridgeFrustum 125, WindowLowerEdge 128, VdokTotalOnCopy 127, ZdoksmallCoarseShare 124, GdokleftOnce 129, DoksdConsistent 128, CategoriesOnCopy 127, CategoriesLastRowRecomputed 127, CategoriesEmptyRowsMerged 132, PdoksmallFirstCell 126, AllButCategoriesOnCopy 129, AllButCategoriesLastRowRecomputed 128, AllButCategoriesEmptyRowsMerged 130 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 310.778 | 1.166 | 4.665 |
| Dkarm43_cor(1) | 238.325 | 0.360 | 1.441 |
| Dkarm43_cor(2) | 286.651 | 0.265 | 1.058 |
| Dkarm10 | 55.091 | 0.010 | 0.041 |
| Zkarm | 0.581 | 7.860e-4 | 3.144e-3 |
| Zkarm_cor | 0.583 | 7.427e-4 | 2.971e-3 |
| Dagg43 | 263.914 | 0.990 | 3.962 |
| Dagg43_cor(1) | 202.386 | 0.306 | 1.223 |
| Dagg43_cor(2) | 243.424 | 0.224 | 0.897 |
| da_coef | 0.849 | 0 | 0 |
| Dqmkm2 | 0.577 | 5.182e-5 | 2.073e-4 |
| Dok43sd | 162.430 | 0 | 0 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 60.688 | 2.459 | 9.837 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.086 +- 0.532 | +0.087 +- 0.219 | +0.056 +- 0.130 | +0.027 +- 0.027 | +2.297 +- 0.187* | +2.330 +- 0.186* | +0.077 +- 0.532 | +0.078 +- 0.219 | +0.047 +- 0.130 | -8.573e-3 +- 3.376e-15* | -4.818 +- 0.014* | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +16.169 +- 5.512 | [27] -0.011 +- 0.018 |
| ExactConstants | +0.086 +- 0.532 | +0.087 +- 0.219 | +0.047 +- 0.129 | +0.027 +- 0.027 | -0.017 +- 0.184 | -0.043 +- 0.171 | +0.077 +- 0.532 | +0.078 +- 0.219 | +0.038 +- 0.129 | -8.573e-3 +- 3.376e-15* | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +1.339 +- 5.642 | [27] -0.011 +- 0.018 |
| BridgeFrustum | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0.027 | -2.732 +- 0.192* | -2.816 +- 0.185* | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0 | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 5.731 | [2] 0 +- 0.030 |
| WindowLowerEdge | 0 +- 0.531 | 0 +- 0.214 | +6.541e-3 +- 0.128 | 0 +- 0.027 | +4.860 +- 0.189* | +4.975 +- 0.182* | 0 +- 0.531 | 0 +- 0.214 | +6.419e-3 +- 0.128 | 0 +- 0 | -4.825 +- 0.013* | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 5.731 | [2] 0 +- 0.030 |
| VdokTotalOnCopy | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0.027 | 0 +- 0.191 | 0 +- 0.180 | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0 | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 5.731 | [2] 0 +- 0.030 |
| ZdoksmallCoarseShare | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0.027 | 0 +- 0.191 | 0 +- 0.180 | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0 | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 5.731 | [2] 0 +- 0.030 |
| GdokleftOnce | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0.027 | 0 +- 0.191 | 0 +- 0.180 | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0 | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 5.731 | [2] 0 +- 0.030 |
| DoksdConsistent | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0.027 | 0 +- 0.191 | 0 +- 0.180 | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0 | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 5.731 | [2] 0 +- 0.030 |
| CategoriesOnCopy | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0.027 | 0 +- 0.191 | 0 +- 0.180 | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0 | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +16.993 +- 5.713 | [2] 0 +- 0.030 |
| CategoriesLastRowRecomputed | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0.027 | 0 +- 0.191 | 0 +- 0.180 | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0 | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | -0.103 +- 5.751 | [2] 0 +- 0.030 |
| CategoriesEmptyRowsMerged | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0.027 | 0 +- 0.191 | 0 +- 0.180 | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0 | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | -0.309 +- 5.706 | [2] 0 +- 0.030 |
| PdoksmallFirstCell | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0.027 | 0 +- 0.191 | 0 +- 0.180 | 0 +- 0.531 | 0 +- 0.214 | 0 +- 0.130 | 0 +- 0 | 0 +- 0.013 | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 5.731 | [2] 0 +- 0.030 |
| AllButCategoriesOnCopy | +0.086 +- 0.532 | +0.087 +- 0.219 | +0.056 +- 0.130 | +0.027 +- 0.027 | +2.297 +- 0.187* | +2.330 +- 0.186* | +0.077 +- 0.532 | +0.078 +- 0.219 | +0.047 +- 0.130 | -8.573e-3 +- 3.376e-15* | -4.818 +- 0.014* | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +0.721 +- 5.641 | [27] -0.011 +- 0.018 |
| AllButCategoriesLastRowRecomputed | +0.086 +- 0.532 | +0.087 +- 0.219 | +0.056 +- 0.130 | +0.027 +- 0.027 | +2.297 +- 0.187* | +2.330 +- 0.186* | +0.077 +- 0.532 | +0.078 +- 0.219 | +0.047 +- 0.130 | -8.573e-3 +- 3.376e-15* | -4.818 +- 0.014* | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +16.169 +- 5.512 | [27] -0.011 +- 0.018 |
| AllButCategoriesEmptyRowsMerged | +0.086 +- 0.532 | +0.087 +- 0.219 | +0.056 +- 0.130 | +0.027 +- 0.027 | +2.297 +- 0.187* | +2.330 +- 0.186* | +0.077 +- 0.532 | +0.078 +- 0.219 | +0.047 +- 0.130 | -8.573e-3 +- 3.376e-15* | -4.818 +- 0.014* | 0 +- 0 | abs 0 +- 0 | abs 0 +- 0 | +17.199 +- 5.693 | [27] -0.011 +- 0.018 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm43_cor(1) | Dkarm43_cor(2) | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | Dagg43_cor(1) | Dagg43_cor(2) | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.2 (+10.6) | +0.4 (+7.2) | +0.4 (+2.1) | +1.0 (+11.6) | +12.3 (+54.1) | +12.5 (+41.5) | +0.1 (+9.5) | +0.4 (+6.5) | +0.4 (+1.8) | -2539608541253.3 (-inf) | -349.9 (-1440.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +2.9 (+7.9) | -0.6 (-1.0) |
| ExactConstants | +0.2 (+10.6) | +0.4 (+7.2) | +0.4 (+4.1) | +1.0 (+11.6) | -0.1 (-0.6) | -0.2 (-1.3) | +0.1 (+9.5) | +0.4 (+6.5) | +0.3 (+3.3) | -2539608541253.3 (-inf) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.2 (+0.7) | -0.6 (-1.0) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -14.2 (-215.7) | -15.3 (-202.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +0.1 (+0.3) | 0.0 (0.0) | +25.8 (+105.5) | +27.4 (+86.4) | 0.0 (0.0) | 0.0 (0.0) | +0.1 (+0.3) | 0.0 (0.0) | -363.3 (-1798.5) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +3.0 (+8.6) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (-1.0) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -0.1 (-1.9) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| AllButCategoriesOnCopy | +0.2 (+10.6) | +0.4 (+7.2) | +0.4 (+2.1) | +1.0 (+11.6) | +12.3 (+54.1) | +12.5 (+41.5) | +0.1 (+9.5) | +0.4 (+6.5) | +0.4 (+1.8) | -2539608541253.3 (-inf) | -349.9 (-1440.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.1 (+0.4) | -0.6 (-1.0) |
| AllButCategoriesLastRowRecomputed | +0.2 (+10.6) | +0.4 (+7.2) | +0.4 (+2.1) | +1.0 (+11.6) | +12.3 (+54.1) | +12.5 (+41.5) | +0.1 (+9.5) | +0.4 (+6.5) | +0.4 (+1.8) | -2539608541253.3 (-inf) | -349.9 (-1440.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +2.9 (+7.9) | -0.6 (-1.0) |
| AllButCategoriesEmptyRowsMerged | +0.2 (+10.6) | +0.4 (+7.2) | +0.4 (+2.1) | +1.0 (+11.6) | +12.3 (+54.1) | +12.5 (+41.5) | +0.1 (+9.5) | +0.4 (+6.5) | +0.4 (+1.8) | -2539608541253.3 (-inf) | -349.9 (-1440.3) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +3.0 (+8.1) | -0.6 (-1.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 0 / 7139 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-HMX.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 1196 / 7624 | fqmkm2 (93), fqdokkarm(25,:) (50), fqdokkarm(24,:) (46), fqdokkarm(26,:) (46), fqdokkarm(23,:) (45), fqdokkarm(27,:) (43), fqdokkarm(22,:) (40), fqdokkarm(35,:) (40), and 43 more |
| ExactConstants | 1 / 7625 | da_coef (1) |
| BridgeFrustum | 3 / 7625 | Zkarm_cor (2), Zkarm (1) |
| WindowLowerEdge | 97 / 7624 | fqmkm2 (93), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 7625 | none |
| ZdoksmallCoarseShare | 0 / 7625 | none |
| GdokleftOnce | 0 / 7625 | none |
| DoksdConsistent | 0 / 7625 | none |
| CategoriesOnCopy | 1095 / 7625 | fqdokkarm(25,:) (51), fqdokkarm(24,:) (46), fqdokkarm(26,:) (46), fqdokkarm(23,:) (45), fqdokkarm(27,:) (43), fqdokkarm(21,:) (40), fqdokkarm(22,:) (40), fqdokkarm(35,:) (40), and 38 more |
| CategoriesLastRowRecomputed | 0 / 7625 | none |
| CategoriesEmptyRowsMerged | 0 / 7551 | none |
| PdoksmallFirstCell | 0 / 7625 | none |
| AllButCategoriesOnCopy | 98 / 7550 | fqmkm2 (93), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1), da_coef (1) |
| AllButCategoriesLastRowRecomputed | 1196 / 7624 | fqmkm2 (93), fqdokkarm(25,:) (50), fqdokkarm(24,:) (46), fqdokkarm(26,:) (46), fqdokkarm(23,:) (45), fqdokkarm(27,:) (43), fqdokkarm(22,:) (40), fqdokkarm(35,:) (40), and 43 more |
| AllButCategoriesEmptyRowsMerged | 1196 / 7624 | fqmkm2 (93), fqdokkarm(25,:) (50), fqdokkarm(24,:) (46), fqdokkarm(26,:) (46), fqdokkarm(23,:) (45), fqdokkarm(27,:) (43), fqdokkarm(22,:) (40), fqdokkarm(35,:) (40), and 43 more |

