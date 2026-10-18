## P33

reference formulation, the original's defaults. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..15 (16 seeds); 1 cycle(s) of 20000 particles; `Dmin` 10 um, `Di` 10 um, `Dj` 10 um.

Work: 75 s over 14 arms (Original 5, All 5, ExactConstants 6, BridgeFrustum 6, Fmkarm2OnAcceptance 6, WindowLowerEdge 5, VdokTotalOnCopy 5, ZdoksmallCoarseShare 5, GdokleftOnce 6, DoksdConsistent 6, CategoriesOnCopy 5, CategoriesLastRowRecomputed 6, CategoriesEmptyRowsMerged 5, PdoksmallFirstCell 5 s).

Failed runs: none.

Builds behind the runs: 1 distinct (a rebuild of unchanged source is a new one).

### Original arm: mean, standard error and standard deviation over the seeds

| quantity | mean | standard error | standard deviation |
|---|---|---|---|
| Dkarm43 | 140.231 | 1.701 | 6.805 |
| Dkarm10 | 58.255 | 0.019 | 0.076 |
| Zkarm | 0.505 | 1.974e-3 | 7.897e-3 |
| Zkarm_cor | 6.408e-3 | 6.083e-4 | 2.433e-3 |
| Dagg43 | 110.815 | 1.344 | 5.377 |
| da_coef | 0.790 | 2.867e-17 | 1.147e-16 |
| Dqmkm2 | 0.649 | 1.106e-4 | 4.425e-4 |
| Dok43sd | 75.450 | 3.669e-15 | 1.468e-14 |
| fineoxy_fr | 0 | 0 | 0 |
| pdoksmall[0] | 0 | 0 | 0 |
| DPRow | 6.000 | 0 | 0 |

### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift

`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. A quantity the seed does not touch (`da_coef`, `Dok43sd`, whatever the setup computes) has a standard error at rounding size, so its t is meaningless and its star says only that the shift is exact. A relative shift of a small base (`Zkarm_cor` on P33) is large by construction. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.117 +- 1.714 | +0.051 +- 0.046 | +5.426 +- 0.569* | +614.528 +- 14.054* | +0.111 +- 1.714 | -9.288e-3 +- 8.111e-15* | -4.372 +- 0.023* | +49.317 +- 1.087e-14* | abs 0 +- 0 | abs 0 +- 0 | +83.333 +- 0* | [9] +0.110 +- 0.124 |
| ExactConstants | +0.117 +- 1.714 | +0.051 +- 0.046 | +0.059 +- 0.587 | -0.777 +- 13.514 | +0.111 +- 1.714 | -9.288e-3 +- 8.111e-15* | +5.780e-3 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [9] +0.110 +- 0.124 |
| BridgeFrustum | 0 +- 1.716 | 0 +- 0.046 | -7.033 +- 0.560* | -1068.139 +- 13.779* | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.161 |
| Fmkarm2OnAcceptance | 0 +- 1.716 | 0 +- 0.046 | 0 +- 0.553 | 0 +- 13.424 | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.161 |
| WindowLowerEdge | 0 +- 1.716 | 0 +- 0.046 | +11.609 +- 0.528* | +1574.335 +- 13.521* | 0 +- 1.716 | 0 +- 5.130e-15 | -4.377 +- 0.024* | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.161 |
| VdokTotalOnCopy | 0 +- 1.716 | 0 +- 0.046 | 0 +- 0.553 | 0 +- 13.424 | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.161 |
| ZdoksmallCoarseShare | 0 +- 1.716 | 0 +- 0.046 | 0 +- 0.553 | 0 +- 13.424 | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.161 |
| GdokleftOnce | 0 +- 1.716 | 0 +- 0.046 | 0 +- 0.553 | 0 +- 13.424 | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.161 |
| DoksdConsistent | 0 +- 1.716 | 0 +- 0.046 | 0 +- 0.553 | 0 +- 13.424 | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | +49.317 +- 1.087e-14* | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.161 |
| CategoriesOnCopy | 0 +- 1.716 | 0 +- 0.046 | 0 +- 0.553 | 0 +- 13.424 | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | +83.333 +- 0* | [2] 0 +- 0.161 |
| CategoriesLastRowRecomputed | 0 +- 1.716 | 0 +- 0.046 | 0 +- 0.553 | 0 +- 13.424 | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | -2.083 +- 1.423 | [2] 0 +- 0.161 |
| CategoriesEmptyRowsMerged | 0 +- 1.716 | 0 +- 0.046 | 0 +- 0.553 | 0 +- 13.424 | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.161 |
| PdoksmallFirstCell | 0 +- 1.716 | 0 +- 0.046 | 0 +- 0.553 | 0 +- 13.424 | 0 +- 1.716 | 0 +- 5.130e-15 | 0 +- 0.024 | 0 +- 6.877e-15 | abs 0 +- 0 | abs 0 +- 0 | 0 +- 0 | [2] 0 +- 0.161 |

### Welch t, with the paired t over the shared seeds in brackets

| arm | Dkarm43 | Dkarm10 | Zkarm | Zkarm_cor | Dagg43 | da_coef | Dqmkm2 | Dok43sd | fineoxy_fr | pdoksmall[0] | DPRow | pdoksmall, cell moving most |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| All | +0.1 (+5.9) | +1.1 (+49444461054921.3) | +9.5 (+76.3) | +43.7 (+253.8) | +0.1 (+5.6) | -1145108022942.4 (-inf) | -186.2 (-1281.8) | +4535238157117550.0 (+inf) | 0.0 (0.0) | 0.0 (0.0) | +inf (+inf) | +0.9 (+1.5) |
| ExactConstants | +0.1 (+5.9) | +1.1 (+49444461054921.3) | +0.1 (+0.9) | -0.1 (-1.2) | +0.1 (+5.6) | -1145108022942.4 (-inf) | +0.2 (+3.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +0.9 (+1.5) |
| BridgeFrustum | 0.0 (0.0) | 0.0 (0.0) | -12.6 (-292.2) | -77.5 (-664.4) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| Fmkarm2OnAcceptance | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| WindowLowerEdge | 0.0 (0.0) | 0.0 (0.0) | +22.0 (+224.2) | +116.4 (+1911.4) | 0.0 (0.0) | 0.0 (0.0) | -182.1 (-1270.1) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| VdokTotalOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| ZdoksmallCoarseShare | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| GdokleftOnce | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| DoksdConsistent | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +4535238157117550.0 (+inf) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| CategoriesOnCopy | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | +inf (+inf) | 0.0 (0.0) |
| CategoriesLastRowRecomputed | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | -1.5 (-1.5) | 0.0 (0.0) |
| CategoriesEmptyRowsMerged | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |
| PdoksmallFirstCell | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) | 0.0 (0.0) |

### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3

The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.

Floor of the comparison, `Original` even seeds (8) against odd seeds (8): 2 / 1506 cells differ, in cells one half prints and the other does not or whose print resolution is coarser than their spread (`control-negative-P33.md`); an arm's count means something only above it.

| arm | differing / compared | quantities with a differing cell (count) |
|---|---|---|
| All | 154 / 1526 | fqmkm2 (86), fqdokkarm(6,:) (16), fmkarm_cor2 (12), dokkarm10 (5), fmkarm (5), fqdokkarm(1,:) (4), fqdokkarm(2,:) (4), dokkarm43 (3), and 14 more |
| ExactConstants | 8 / 1527 | fmkarm (5), coef (1), da_coef (1), fqkarm (1) |
| BridgeFrustum | 3 / 1527 | Zkarm_cor (2), Zkarm (1) |
| Fmkarm2OnAcceptance | 15 / 1527 | fmkarm_cor2 (12), Dagg43_cor(2) (1), Dkarm43_cor(2) (1), Dkarm43sd_cor(2) (1) |
| WindowLowerEdge | 90 / 1526 | fqmkm2 (86), Zkarm_cor (2), Dqmkm2 (1), Zkarm (1) |
| VdokTotalOnCopy | 0 / 1527 | none |
| ZdoksmallCoarseShare | 0 / 1527 | none |
| GdokleftOnce | 0 / 1527 | none |
| DoksdConsistent | 1 / 1527 | Dok43sd (1) |
| CategoriesOnCopy | 40 / 1527 | fqdokkarm(6,:) (16), dokkarm10 (5), fqdokkarm(1,:) (4), fqdokkarm(2,:) (4), dokkarm43 (3), fqdokkarm(3,:) (3), fqdokkarm(4,:) (3), Dkarmcat (1), and 1 more |
| CategoriesLastRowRecomputed | 22 / 1527 | fqdokkarm(4,:) (20), dokkarm10 (1), dokkarm43 (1) |
| CategoriesEmptyRowsMerged | 0 / 1527 | none |
| PdoksmallFirstCell | 0 / 1527 | none |

