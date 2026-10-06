# HISTORY.md — Benchmarks

Append-only store of what `BOOT.md` no longer needs to state as current truth
(`AGENTS.md`, §15). Newest entry first. Every entry names the date it was moved, the
`BOOT.md` section it came from, and carries the original text in full, unedited. The
start procedure (`AGENTS.md`, §10) does not read this file; it is reached only by
following a dated pointer left in `BOOT.md` at the place the text used to stand.

---

<a id="ship-window-2026-10-03"></a>
<a id="figures-w2-superseded-2026-10-03"></a>

## 2026-10-03 — from "### W5: launch shape" ("Before a shape ships") and `## Figures` — the ship window of the W5 launch shape: both attempts, the checks, the SHA-256 of what produced them, and the W2 figures section as it stood

**1. The window.** Run by the orchestrator with the GPU booked and the machine quiet, from `ship-build.sh` (outside the window)
and `ship-gpu.sh`, the steps of `BOOT.md`, "### W5", "Before a shape ships": (a) `TierTableTests` on CUDA read against the table of
`src/Execution/BOOT.md`; (b) the seed-0 CUDA `results.m` of the five formulations the seed-0 snapshot covers, SHA-256 of the file
without its time line, parent / commit / parent again, with HMX at seed 1 as the control; (c) an A/B/A of the parent and the commit,
two rounds, `--paths cuda --repeats 3`; (d) only if (a)-(c) all pass, the six-path command of `## Figures`. The parent is `6df81ae`
(the code commit's parent, which differs from `bceb72c`, the head of `claude/wave11` before the branch, in documents only); the
commit is `531d66c` (the code commit `424e3bd` plus documents only). Two attempts, with the same script and settings; the
directories `wave11/w5/ship/shipC-20261003-0458/` and `shipC-20261003-0523/` and the console copies `ship-gpu-run.log`,
`ship-gpu-run2.log` are in the orchestrating session's scratchpad, not in the repository. Neither attempt is dropped.

Stamp of `ship-build.sh` (the trees and assemblies the window verified before starting):

```
code commit 424e3bd1a41197c0fdfa712553ddc28e9af950c0
parent 6df81aed4d87dec79e10759cfe406afe5c35550b
ship 531d66c584e22216b3f244a92e4e53c2ac5bbecf
parent_short 6df81ae
ship_short 531d66c
trees: perf-w5-parent, perf-w5-ship
0dabeeb181b91a3be4a303148b9aefda75d1aee580e760cd6210bfc4e9da0c70 *perf-w5-parent/src/Execution/bin/Release/net10.0/PropStruct.Execution.dll
88e7ba270c622cb2732e0f5da52f13f4e9dbdb77ea94644b562a946284ce69fa *perf-w5-parent/tests/Benchmarks/bin/Release/net10.0/PropStruct.Benchmarks.dll
130ec7470f6a8cf3816214fc9143fd9b1704bfb2ee8afe3e2d6ec8cfbf14ff42 *perf-w5-parent/src/Cli/bin/Release/net10.0/propstruct.dll
beaa1f8885789481c6dd215788db95c51921d03e7e0085f067f97ee7d867c18f *perf-w5-parent/tests/Execution.Tests/bin/Release/net10.0/PropStruct.Execution.Tests.dll
52bf7207f6f74ac7fa00fc8bbb2d456c95772f43f33dcc75d870a09d8e5cd6d7 *perf-w5-ship/src/Execution/bin/Release/net10.0/PropStruct.Execution.dll
b02acfe582b7378f54bd014ee70ba179f85a8cc7f9c3af8ae68b08b832d4ffde *perf-w5-ship/tests/Benchmarks/bin/Release/net10.0/PropStruct.Benchmarks.dll
8b7b00a7de4ac8e11735b28a698e17310f74066190f55785692e9f5eb3d33466 *perf-w5-ship/src/Cli/bin/Release/net10.0/propstruct.dll
76ce09322518d2ae912bc6b5d28a411d51ebe0b3f3011a89aa8c0d0f0076d228 *perf-w5-ship/tests/Execution.Tests/bin/Release/net10.0/PropStruct.Execution.Tests.dll
```

**2. Attempt 1, 04:58-05:07 local: (a) PASS, (b) PASS, (c) VOID, (d) skipped.** (a) and (b) read as in attempt 2 (sections 4 and 5,
identical figures and hashes). (c): the machine was not steady, cause unknown; the two HMX rounds are void by the rule (ν_i > 3 %), so
HMX has no valid round and the comparison is void, not failed and not passed. On HPEPA3 round 2 reads 18 % (parent, 43932 then
36140) and 16 % (commit, 57315 then 48390) below round 1; HMX's parent reads 5038, 4133 ± 1573, 5190 and 4203 over its four calls.
The quiet check before round 2 read 52 % utilisation, taken at
the timestamp the preceding call ended (the guard that follows it passed). Nothing of the attempt is used for a figure.

| round | pos | tree | F | particles/s (± spread, n = 3) | Budget | Launches | Attempts | time | SM clock, °C, W |
|---|---|---|---|---|---|---|---|---|---|
| 1 | P1 | parent | HMX | 5038 ± 35 | 8192 | 2 | 6393938 | 05:04:00 | 2790 MHz, 49, 55.02 W |
| 1 | P1 | parent | HPEPA3 | 43932 ± 465 | 8192 | 2 | 1396212 | 05:04:16 | 2790 MHz, 50, 63.82 W |
| 1 | C | ship | HMX | 6855 ± 34 | 8192 | 2 | 6393938 | 05:04:25 | 2790 MHz, 50, 63.96 W |
| 1 | C | ship | HPEPA3 | 57315 ± 2989 | 8192 | 2 | 1396212 | 05:04:38 | 2790 MHz, 50, 59.78 W |
| 1 | P2 | parent | HMX | 4133 ± 1573 | 8192 | 2 | 6393938 | 05:04:57 | 930 MHz, 48, 30.02 W |
| 1 | P2 | parent | HPEPA3 | 43541 ± 1177 | 8192 | 2 | 1396212 | 05:05:13 | 2790 MHz, 50, 59.62 W |
| 2 | P1 | parent | HMX | 5190 ± 34 | 8192 | 2 | 6393938 | 05:05:37 | 2790 MHz, 50, 60.24 W |
| 2 | P1 | parent | HPEPA3 | 36140 ± 49 | 8192 | 2 | 1396212 | 05:05:55 | 2782 MHz, 51, 65.80 W |
| 2 | C | ship | HMX | 5704 ± 49 | 8192 | 2 | 6393938 | 05:06:05 | 2782 MHz, 52, 69.22 W |
| 2 | C | ship | HPEPA3 | 48390 ± 2923 | 8192 | 2 | 1396212 | 05:06:20 | 2782 MHz, 52, 69.25 W |
| 2 | P2 | parent | HMX | 4203 ± 61 | 8192 | 2 | 6393938 | 05:06:33 | 2782 MHz, 53, 65.36 W |
| 2 | P2 | parent | HPEPA3 | 36009 ± 68 | 8192 | 2 | 1396212 | 05:06:52 | 2782 MHz, 53, 69.92 W |

```
formulation | round | parent | commit | parent | P_i | nu_i % | g = commit / P_i
HMX | 1 | 5038 | 6855 | 4133 | 4585.5 | 19.74 | 1.495  VOID (nu_i > 3 %)
HMX | 2 | 5190 | 5704 | 4203 | 4696.5 | 21.02 | 1.215  VOID (nu_i > 3 %)
HPEPA3 | 1 | 43932 | 57315 | 43541 | 43736.5 | 0.89 | 1.310
HPEPA3 | 2 | 36140 | 48390 | 36009 | 36074.5 | 0.36 | 1.341
ABA: VOID: HMX has 2 void round(s) of 2 and 0 valid
```

Quiet checks of attempt 1 (`nvidia-smi`: utilisation, memory, SM clock, power, temperature; the compute-process list read 27
processes each time, none work-like):

| check | time | GPU util | memory | SM clock | power | °C |
|---|---|---|---|---|---|---|
| before-a | 04:58:26 | 1 % | 1795 MiB | 937 MHz | 28.81 W | 42 |
| before-b | 05:02:34 | 0 % | 1804 MiB | 2790 MHz | 49.83 W | 48 |
| c-round-1 | 05:03:36 | 0 % | 1796 MiB | 2790 MHz | 50.90 W | 48 |
| c-round-2 | 05:05:14 | 52 % | 1819 MiB | 2790 MHz | 59.62 W | 50 |
| after-c | 05:06:52 | 15 % | 1853 MiB | 2782 MHz | 63.42 W | 53 |

**3. Attempt 2, 05:23-05:44 local: (a) PASS, (b) PASS, (c) PASS, (d) PASS** ((c) and (d) as below). The A/B/A, the same rounds, g = commit
over the round's parent mean:

| round | pos | tree | F | particles/s (± spread, n = 3) | Budget | Launches | Attempts | time | SM clock, °C, W |
|---|---|---|---|---|---|---|---|---|---|
| 1 | P1 | parent | HMX | 5187 ± 40 | 8192 | 2 | 6393938 | 05:29:11 | 2790 MHz, 47, 59.84 W |
| 1 | P1 | parent | HPEPA3 | 44784 ± 184 | 8192 | 2 | 1396212 | 05:29:26 | 2790 MHz, 47, 61.16 W |
| 1 | C | ship | HMX | 7039 ± 54 | 8192 | 2 | 6393938 | 05:29:35 | 2790 MHz, 48, 73.54 W |
| 1 | C | ship | HPEPA3 | 60135 ± 371 | 8192 | 2 | 1396212 | 05:29:47 | 2790 MHz, 48, 69.19 W |
| 1 | P2 | parent | HMX | 5190 ± 29 | 8192 | 2 | 6393938 | 05:29:59 | 2790 MHz, 48, 59.63 W |
| 1 | P2 | parent | HPEPA3 | 44804 ± 186 | 8192 | 2 | 1396212 | 05:30:15 | 2790 MHz, 49, 60.79 W |
| 2 | P1 | parent | HMX | 5185 ± 28 | 8192 | 2 | 6393938 | 05:30:38 | 2790 MHz, 48, 62.17 W |
| 2 | P1 | parent | HPEPA3 | 44771 ± 183 | 8192 | 2 | 1396212 | 05:30:53 | 2790 MHz, 49, 63.85 W |
| 2 | C | ship | HMX | 7036 ± 71 | 8192 | 2 | 6393938 | 05:31:03 | 2790 MHz, 49, 62.50 W |
| 2 | C | ship | HPEPA3 | 58566 ± 2238 | 8192 | 2 | 1396212 | 05:31:15 | 2790 MHz, 49, 67.14 W |
| 2 | P2 | parent | HMX | 5190 ± 28 | 8192 | 2 | 6393938 | 05:31:27 | 2790 MHz, 49, 58.43 W |
| 2 | P2 | parent | HPEPA3 | 44794 ± 186 | 8192 | 2 | 1396212 | 05:31:42 | 2790 MHz, 50, 66.10 W |

```
formulation | round | parent | commit | parent | P_i | nu_i % | g = commit / P_i
HMX | 1 | 5187 | 7039 | 5190 | 5188.5 | 0.06 | 1.357
HMX | 2 | 5185 | 7036 | 5190 | 5187.5 | 0.10 | 1.356
HPEPA3 | 1 | 44784 | 60135 | 44804 | 44794.0 | 0.04 | 1.342
HPEPA3 | 2 | 44771 | 58566 | 44794 | 44782.5 | 0.05 | 1.308
HMX: median g 1.356 over valid rounds [1, 2]; every valid g >= 1.10
HPEPA3: median g 1.325 over valid rounds [1, 2]; every valid g >= 1.10
ABA: PASS: g >= 1.10 again on both formulations in every valid round
```

Quiet checks of attempt 2:

| check | time | GPU util | memory | SM clock | power | °C |
|---|---|---|---|---|---|---|
| before-a | 05:23:48 | 1 % | 1752 MiB | 487 MHz | 16.17 W | 37 |
| before-b | 05:27:47 | 90 % | 1752 MiB | 2790 MHz | 50.24 W | 45 |
| c-round-1 | 05:28:48 | 0 % | 1752 MiB | 2790 MHz | 55.90 W | 46 |
| c-round-2 | 05:30:15 | 100 % | 1752 MiB | 2790 MHz | 60.79 W | 49 |
| after-c | 05:31:42 | 6 % | 1752 MiB | 2790 MHz | 59.12 W | 49 |
| before-d-HPEPA3 | 05:31:49 | 0 % | 1752 MiB | 487 MHz | 16.93 W | 47 |
| before-d-HMX | 05:35:58 | 0 % | 1752 MiB | 510 MHz | 16.36 W | 40 |
| after-d | 05:44:10 | 1 % | 1840 MiB | 930 MHz | 29.37 W | 39 |

**4. (a) The tier table, digit for digit** (both attempts, identical; `TierTableTests` at attempt budget 8192, Release, the ship tree,
read from the `.trx`; 3 m 54 s and 3 m 46 s). The recorded column is the table of `src/Execution/BOOT.md` (2026-10-01):

```
formulation | recorded max relative diff | recorded diverged | read max relative diff | read diverged | digit for digit
HMX | 2.187533E-14 | 0/2000 | 2.187533E-014 | 0/2000 | yes
HPEPA3 | 4.425692E-14 | 0/2000 | 4.425692E-014 | 0/2000 | yes
inpt | 1.200943E-14 | 0/1000 | 1.200943E-014 | 0/1000 | yes
P33 | 2.573474E-14 | 0/2000 | 2.573474E-014 | 0/2000 | yes
PSAN02n | 2.495024E-15 | 0/2000 | 2.495024E-015 | 0/2000 | yes
TIER: PASS: the five figures of src/Execution/BOOT.md reproduce digit for digit
```

**5. (b) The seed-0 CUDA hashes** (both attempts, identical: batched mode, `Independent` layout, `Binary64`, seed 0, SHA-256 of
`results.m` without its `Calculation time` line; `parent1`, `ship`, `parent2` equal for each formulation, the control, HMX at seed 1,
differs from seed 0, so the comparison can go red). HPEPA3's hash equals the one the same command printed on the CPU
(`3a2b7878…3075`, `ship-build.sh`'s `parent-batched`).

```
HPEPA3 parent1 3a2b7878bf4062ca6f6c44bf82b9d7167e63e24690d52deb3b012ffd4d8d3075
HPEPA3 ship 3a2b7878bf4062ca6f6c44bf82b9d7167e63e24690d52deb3b012ffd4d8d3075
HPEPA3 parent2 3a2b7878bf4062ca6f6c44bf82b9d7167e63e24690d52deb3b012ffd4d8d3075
inpt parent1 03601c3f95ff59efffc93d670e0f81d8c28bdd158ec4d6a29b00f25e90759f39
inpt ship 03601c3f95ff59efffc93d670e0f81d8c28bdd158ec4d6a29b00f25e90759f39
inpt parent2 03601c3f95ff59efffc93d670e0f81d8c28bdd158ec4d6a29b00f25e90759f39
P33 parent1 f8afeb78c1fbc601d0b03a07c9dea0c16344b3bfff26cea609054c98b7941bea
P33 ship f8afeb78c1fbc601d0b03a07c9dea0c16344b3bfff26cea609054c98b7941bea
P33 parent2 f8afeb78c1fbc601d0b03a07c9dea0c16344b3bfff26cea609054c98b7941bea
PSAN02n parent1 f2db4c0abc5c643447048dbe5e2c11300c679b4a1ec3e7df54b779071935a2ca
PSAN02n ship f2db4c0abc5c643447048dbe5e2c11300c679b4a1ec3e7df54b779071935a2ca
PSAN02n parent2 f2db4c0abc5c643447048dbe5e2c11300c679b4a1ec3e7df54b779071935a2ca
HMX parent1 cafe9c39b2cfeef141254a23998e483536dfebe96e476839ef351c5ee7d0af3e
HMX ship cafe9c39b2cfeef141254a23998e483536dfebe96e476839ef351c5ee7d0af3e
HMX parent2 cafe9c39b2cfeef141254a23998e483536dfebe96e476839ef351c5ee7d0af3e
HMX control fc0029296a56c2b2348c8e4974c43bc1d673951dcafed4542cdc91eb19e35724
```

**6. (d) The six-path rows** (attempt 2, the ship tree `531d66c`, clean; the `cpu` rows are kept here, `## Figures` omits them as W2's
table did; `ship-check.py figures` read PASS for both: six rows, clean commit, Launches 2, Budget 8192 and Attempts as W2 on the
batched rows):

```
# Provenance
Date (UTC): 2026-10-03
Machine: AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200
.NET: .NET 10.0.12
Commit: 531d66c
Command: dotnet run -c Release --project tests/Benchmarks -- --formulation HPEPA3 --paths reference,host1,host16,cpu,cuda,original --repeats 3
Particle-count override: none (each formulation's own N)
Attempts-per-launch override: none (SimulationOptions' own default)

| Date | Formulation | Path | Accepted particles/s | Budget | Launches | Attempts | Machine | Build | Commit |
|---|---|---|---|---|---|---|---|---|---|
| 2026-10-03 | HPEPA3 | reference | 9134 ± 52 (n=3) | 1 | 1399729 | 1399729 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HPEPA3 | host1 | 9219 ± 14 (n=3) | 8192 | 2 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HPEPA3 | host16 | 48981 ± 759 (n=3) | 8192 | 2 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HPEPA3 | cpu | not available (the ILGPU CPU accelerator's kernel-launch path is Execution-internal (InternalsVisibleTo Simulation, Cli and their tests only, src/Execution/API.md); Simulation's public SimulationOptions has no field that reaches it (BOOT.md, "## Escalation: the CPU accelerator oracle path")) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HPEPA3 | cuda | 59727 ± 1480 (n=3) | 8192 | 2 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HPEPA3 | original | 7376 ± 250 (n=3) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |

# Provenance
Date (UTC): 2026-10-03
Machine: AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200
.NET: .NET 10.0.12
Commit: 531d66c
Command: dotnet run -c Release --project tests/Benchmarks -- --formulation HMX --paths reference,host1,host16,cpu,cuda,original --repeats 3
Particle-count override: none (each formulation's own N)
Attempts-per-launch override: none (SimulationOptions' own default)

| Date | Formulation | Path | Accepted particles/s | Budget | Launches | Attempts | Machine | Build | Commit |
|---|---|---|---|---|---|---|---|---|---|
| 2026-10-03 | HMX | reference | 366 ± 2 (n=3) | 1 | 6425301 | 6425301 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | host1 | 381 ± 1 (n=3) | 8192 | 2 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | host16 | 2085 ± 8 (n=3) | 8192 | 2 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | cpu | not available (the ILGPU CPU accelerator's kernel-launch path is Execution-internal (InternalsVisibleTo Simulation, Cli and their tests only, src/Execution/API.md); Simulation's public SimulationOptions has no field that reaches it (BOOT.md, "## Escalation: the CPU accelerator oracle path")) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | cuda | 7100 ± 6 (n=3) | 8192 | 2 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | original | 319 ± 5 (n=3) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
```

**7. SHA-256 of what produced and holds the figures** (`sha256sum`; the files are in the session scratchpad; `ship-gpu.sh` is as at
the window, edited only in its header comment after `ship-build.sh`'s first rehearsal):

```
9486fdf704f876a839cfd2f7fb2ea182b757f8d5a9c5a6ae26a985c471b95d1f *shipC-20261003-0458/shipC.log
876825f869d117581224cc81dcb67dc291790d6441a76088ace3cac8161b290a *shipC-20261003-0458/summary.txt
c095b1c24dd2338a2b18d44a5703e9b08a17482b8cf1d81a3445cfe9b8ffcce1 *shipC-20261003-0458/a/tier.trx
e6f1387b200bc2ecf85315f3b3ffc398c0db814afb851448b558c3cabc3cd539 *shipC-20261003-0458/a/tier.txt
bd963b2663b36ad16b757e9f1ac812b9ea859f1d24be3bf2b42dabca1d6209f1 *shipC-20261003-0458/a/dotnet-test.log
b4c62728a1696401f9c20042c90f7523c4c4ccb5c4ac8de02d635d93c1a31a51 *shipC-20261003-0458/b/hashes.txt
1dd118b8ec1b27d3badf3230119ae69dd455fd62c7fed2e090dcd1531204b851 *shipC-20261003-0458/b/hashes.txt.verdict
f35bdea27f2ab2dc3ff9d1d883eb3aa92bb4b064f8c2d14a945d3eba2a16aa92 *shipC-20261003-0458/c/aba.rows.txt
389464e56a9bbea569caa7c0eb97425fdcbfee32fa363c6d3cf19ba7b9758fe1 *shipC-20261003-0458/c/aba.clocks.txt
570b23ea62a5d0321255c33a98db29a069fea38cc105d33ccc614de99668b845 *shipC-20261003-0458/c/aba.verdict.txt
d841ffe870d2853916a8dbb52d8f0823a69ac1f3129ae9a827395425b060a83f *shipC-20261003-0523/shipC.log
a1042e1dba2907e0c507f3320669d9017475f991c64e764736f8c1f7f0130d23 *shipC-20261003-0523/summary.txt
7d8740af421fbfc4d9838f5baf0c7bcedf6caf60ba7ed7c3082773e724e5d7db *shipC-20261003-0523/a/tier.trx
d921438aecf7dd905cabc9b35c4831727df252033fe02cc2395541f2770bb46c *shipC-20261003-0523/a/tier.txt
dc1492dbbe9f47b658744268ee1c803ff7cdc995fe9655d013f53f8432b06879 *shipC-20261003-0523/a/dotnet-test.log
b4c62728a1696401f9c20042c90f7523c4c4ccb5c4ac8de02d635d93c1a31a51 *shipC-20261003-0523/b/hashes.txt
1dd118b8ec1b27d3badf3230119ae69dd455fd62c7fed2e090dcd1531204b851 *shipC-20261003-0523/b/hashes.txt.verdict
7c6cd02693146baccd4c87edbe9edc8c387f69a03d74e801f48a919eec3a1d96 *shipC-20261003-0523/c/aba.rows.txt
4652577a3c1534d4b36f4460e305d9eadd7cacacce0c91f10b4bb3b47f3d1df4 *shipC-20261003-0523/c/aba.clocks.txt
dac29ac32719671b7deec0a7ef6ca804ed519f5377fcb4caef708a4bd3b6160d *shipC-20261003-0523/c/aba.verdict.txt
c8a0b5ee73c7034753a084bf092bc3da1faa5159065f1a67624cedbd8eb07006 *shipC-20261003-0523/d/HPEPA3.log
f70b144c59893f5107693b15c61bdbf8d0c9986939ede88b89c1d93d29771717 *shipC-20261003-0523/d/HPEPA3.verdict.txt
2af4439b68812a50246ffa547948b7877f7db38ab82d8c7e6721c6b36cd17686 *shipC-20261003-0523/d/HMX.log
8c95fd7bf18c80d1338a95a943e8ed56ac58b58f8bd294cdbc025e03f2734736 *shipC-20261003-0523/d/HMX.verdict.txt
6786ded6a6b0ebc9d6f632d64dcbf8b3fe202a05c0ca1936c2862675575ef70a  manifest of the 12 files shipC-20261003-0458/c/r*.log (sha256sum r*.log, then sha256)
f5dfba49a9fbe400ea1d42a17573e8ac8ced925802d720b84857bc8b08da2184  manifest of the 12 files shipC-20261003-0523/c/r*.log (sha256sum r*.log, then sha256)
9486fdf704f876a839cfd2f7fb2ea182b757f8d5a9c5a6ae26a985c471b95d1f *ship-gpu-run.log
d841ffe870d2853916a8dbb52d8f0823a69ac1f3129ae9a827395425b060a83f *ship-gpu-run2.log
2b8acddec0ffc4ebdec08689c05c85108fcbcf5d2e06672ace52100ad7ce7e91 *ship-build.sh
12c1bc02bdf18c3835667161489baf575f102ae6d0e99609d9d25852663501da *ship-gpu.sh
943db90101bab3ced3f53d2f1ae894a472bab590aed10726e0e49bd0cd6a9552 *ship-check.py
4072b443eac476e5607509df7f637783e8cce69f4380ca3b0933a6856445e3ae *ship-check-test.py
25612f0af9a2ba252d70392e363bcc6efe91f3a4d33e18026b6c32bfabf3a0e8 *ship-proof.sh
4199b92af8463b64c45c9bb0c23d6a919da5e3f2b3c68766142465a7a8a93909 *fake-bin/fake-dotnet.py
c6bcce7534d9554fcc7bdd10bc85a20ab63c3595c4d794e2be384cb1ee8ddc1a *ship-stamp.txt
```

**8. The W2 figures section as it stood** (2026-10-02, commit `7e8f6f7`, from `## Figures`; its pointers and the cycle-cost line stay in
`BOOT.md`):

> W2 (Stage 0', "### Pre-registered before any new figure (2026-10-02)" below), measured
> 2026-10-02 at the shipped defaults: commit `7e8f6f7`, clean tree, Release, .NET 10.0.12,
> each formulation's own `N`, no override (budget 8192), `--repeats 3`, command
> `dotnet run -c Release --project tests/Benchmarks -- --formulation <F> --paths
> reference,host1,host16,cpu,cuda,original --repeats 3`. Every figure: accepted
> particles/s, cycle 1, mean ± sample spread, n = 3. Quiet checks before (21:14), between
> (21:18), after (21:27): `HISTORY.md#figures-w2-2026-10-02`.
>
> | Date | Formulation | Path | Accepted particles/s | Budget | Launches | Attempts | Machine | Build | Commit |
> |---|---|---|---|---|---|---|---|---|---|
> | 2026-10-02 | HPEPA3 | reference | 9252 ± 56 (n=3) | 1 | 1399729 | 1399729 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HPEPA3 | host1 | 9329 ± 37 (n=3) | 8192 | 2 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HPEPA3 | host16 | 47685 ± 178 (n=3) | 8192 | 2 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HPEPA3 | cuda | 43503 ± 88 (n=3) | 8192 | 2 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HPEPA3 | original | 7275 ± 147 (n=3) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HMX | reference | 367 ± 2 (n=3) | 1 | 6425301 | 6425301 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HMX | host1 | 385 ± 2 (n=3) | 8192 | 2 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HMX | host16 | 2071 ± 18 (n=3) | 8192 | 2 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HMX | cuda | 5095 ± 2 (n=3) | 8192 | 2 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HMX | original | 313 ± 4 (n=3) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
>
> `cpu` reads `not available` on both formulations (`## Implementation notes`,
> "Escalation"). `Launches` and `Attempts` cover the whole call, cycle 0 included;
> `Launches` is 2 on every batched row. A `reference` row's Budget is its effective one, 1
> (`RunDiagnostics.AttemptsPerLaunch`). `original` is the whole run's throughput
> (`## Implementation notes`, "Original's particles/s").
>
> **`host1` now reads at `reference`'s level**, the pin's effect (⚠ 2026-10-02,
> "Host-thread counts"): HPEPA3 9329 ± 37 against 9252 ± 56, spreads overlapping; HMX
> 385 ± 2 against 367 ± 2, 5 % above, spreads apart. The unpinned rows it replaces read
> 28491 ± 2466 and 1433 ± 94, three to four times `reference`.
>
> **R0 at the default: met.** CUDA HMX 5095 ± 2 against 0.8 × host16 HMX (0.8 × 2071 ± 18
> = 1657), outside both spreads; HPEPA3 CUDA 43503 ± 88 stands below host16 47685 ± 178.
>
> **The pre-registered stop fired on HMX and was explained before recording**: CUDA HMX
> 5095 ± 2 outside E-B's 5042 ± 29, host16 HMX 2071 ± 18 outside 2085 ± 5 (HPEPA3 inside;
> R0 held). An A/B/A against E-B's commit `2dc74ec` (HMX, budget 8192, n = 3 per
> invocation) read it in today's range, minutes apart, so the shift is not from the
> commits since. Over four invocations CUDA HMX reads 5030-5095 and host16 HMX 1916-2081
> (accepted particles/s, mean ± spread, n = 3 each): the variation between invocations
> exceeds the spread within one (± 2 to ± 31 on CUDA, up to ± 254 on host16); the A/B/A
> as it stood → `HISTORY.md#benchmarks-boot-trims-2026-10-02`.

<a id="w5-prereg-2026-10-03"></a>
<a id="w5-record-2026-10-03"></a>

## 2026-10-03 — from "### W5: launch shape, pre-registered (2026-10-03)" — the pre-registration as it stood, and window C's counted record: P0, P1, P2, the verdict, the SHA-256 of what produced them

**1. The pre-registration as it stood** (the section of `BOOT.md` at `6df81ae`, whose two ⚠ pointers stay in `BOOT.md`;
their full text is in the entry below this one, `HISTORY.md#w5-stop-2026-10-03`):

> ### W5: launch shape, pre-registered (2026-10-03)
>
> Fixed by the W5 design session (arbiter) before any figure; decision thresholds, not
> tolerances. `Kernels.RunAttempts` on CUDA, budget 8192. Arms: A (ILGPU's automatic
> group, 256); G128, G64, G32 (explicit group, same PTX); R (G128 with launch bounds of
> 3 groups per SM, at most 168 registers); P, a control never adopted (A at budget 2048,
> HMX only; E-B read 3909 against 5042, 0.775); Q, the Attempts control (A at
> `--particles 9999`, HMX, once after P1; its Attempts, taken before the window from host
> threads, differ from W2's 6393938). One Release build of an uncommitted hook selects
> the arm by environment; its rows read `-dirty`.
>
> - **P0** (HMX, A, Nsight Systems, the W4b hook, n = 3): T* (p*), T_w (p*'s warp),
>   T_b (p*'s block); each rerun particle's count equals the bulk's. Recorded; nothing
>   is adopted on it.
> - **P1** (HMX, Nsight Systems, one call per arm): block = G (A: 256); 196 registers
>   and no local memory for A and the G arms; at most 168 registers for R; the other
>   three kernels as in W3; every `W5-KERNEL` line of the arm's P2 logs names its group,
>   and 3 groups per SM for R. A mismatch voids the arm.
>
>   ⚠ 2026-10-03, after round 1 was seen: was the `nsys` call's console, which never
>   shows the profiled process's output (every arm void, every trace as designed), now
>   the P2 logs. No threshold or comparison moves → HISTORY.md#w5-stop-2026-10-03
> - **P2**: four rounds, each A, the other arms in a rotated order, A again; per arm HMX
>   then HPEPA3, `--paths cuda --repeats 3`. Per round and formulation: A_i is the mean
>   of its two A figures, ν_i their relative difference, g = arm ÷ A_i.
> - **Stop** on a row not a figure or "warm-up not isolated", n ≠ 3, a wrong Budget,
>   Launches ≠ 2 (P aside), A's Attempts varying, a G arm's unlike A's, Q's unlike its
>   host-thread figure or equal to A's, A's first call > 5 % off W2, an order off the
>   rotation, a red guard. ν_i > 3 % voids the round for that formulation; two void
>   rounds of one formulation void the window; P's median g over HMX's valid rounds
>   outside 0.74-0.81 voids the reading.
>
>   ⚠ 2026-10-03, after round 1 was seen: was "P's equal to A's", now Q's clause: E-B
>   read HMX's Attempts 6393938 at every budget from 8 to 8192, so the stop that fired in
>   round 1 could never pass. No threshold, candidate arm or comparison moves; round 1
>   has no closing A and is not counted → HISTORY.md#w5-stop-2026-10-03
> - **Adopt** an arm if on one formulation g ≥ 1.10 in every valid round and on both the
>   median g ≥ 1 − max(0.02, 2·max ν_i), ν_i over that formulation's valid rounds. R also
>   needs R ÷ G128 ≥ 1.10 the same way and Attempts equal to A's. Several: the highest
>   geometric mean of the two medians; within 0.02, the larger group.
> - **Predicted** under SM sharing: HMX G32 1.43, G64 1.34, G128 1.06, R ≤ G128; HPEPA3
>   1.00-1.05, R 0.90-1.02 of G128; 1.00 for all if the stretch is in the warp or device.
> - **None qualifies:** recorded; no change; the next lever is a design session's.
> - **Before a shape ships:** it is `Execution`'s constant in a commit; the tier table
>   reproduces digit for digit; seed-0 CUDA `results.m` hashes, time line aside, equal
>   the parent's; an A/B/A of parent and commit shows g ≥ 1.10 again; then this node's
>   command replaces `## Figures` (Taboos).

**2. The counted window.** 2026-10-03 03:05-03:18 local time (UTC 00:05-00:18, so the tool's UTC date reads 2026-10-03 in every
row; the first attempt, 01:49-01:53 local, read 2026-10-02), run by the orchestrator with the GPU booked, from `windowC.sh`
after `windowC-build.sh`; the output directory `wave11/w5/windowC-20261003-0304/` and the log `wave11/w5/windowC-run2.log`
of the orchestrating session's scratchpad, not in the repository. The hook worktrees were at `6df81ae` with an uncommitted
patch to `KernelCache.cs` only (`w5-hook.patch`, an environment-selected group size and launch-bounds hook that prints a
`W5-KERNEL` line) and to `Engine.cs` (`w4c-hook.patch`, the W4b dump and reruns with the groups `2654:1,2624:32,2560:256`),
so every row reads `6df81ae-dirty`. The timed code of the clean tree (`src`, the projects, the build properties) is
that of `bceb72c`, the head of `claude/wave11` the branch was cut from, which differs from `6df81ae` in documents only.
Build stamp (`windowC-build.sh`, the first lines of the log):

```
prereg commit 6df81aed4d87dec79e10759cfe406afe5c35550b
branch perf-w5 head 6df81aed4d87dec79e10759cfe406afe5c35550b
q expected 6393346
hook build dirs: C:/Projects/CompositePropellantMicrostructure/.claude/worktrees/perf-w5-hook (= commit + w5-hook.patch), C:/Projects/CompositePropellantMicrostructure/.claude/worktrees/perf-w4-hook (= commit + w4c-hook.patch), dirty by design
patches: 9d6a993b9d79d83a1f10090e4a404ac4d77aff9ef1fdc27eda236613abd0d2ec w5-hook.patch; f62580dbc1ff9908fa3313282cb1ac08537ec48ffb7f8c5be579c39f8b326909 w4c-hook.patch
72429aa8a633d8ff2f1f7e37174c0dfaa3495249f55c8f78baaab87850191db0 *C:/Projects/CompositePropellantMicrostructure/.claude/worktrees/perf-w5-hook/src/Execution/bin/Release/net10.0/PropStruct.Execution.dll
ef897aaef2378f6718a2d7a600a44c05760fccd5a04808a7105bd379f963cc55 *C:/Projects/CompositePropellantMicrostructure/.claude/worktrees/perf-w5-hook/tests/Benchmarks/bin/Release/net10.0/PropStruct.Benchmarks.dll
dacb8322678674d0ace0c5361427a74ec901faa7e8942cb10deea57a65a101a1 *C:/Projects/CompositePropellantMicrostructure/.claude/worktrees/perf-w4-hook/src/Execution/bin/Release/net10.0/PropStruct.Execution.dll
83d355421ce25187ff05279fc4d331e4407005ccf1cf89ab5668d653cc3451d8 *C:/Projects/CompositePropellantMicrostructure/.claude/worktrees/perf-w4-hook/tests/Benchmarks/bin/Release/net10.0/PropStruct.Benchmarks.dll
W4a: p* = 2654, A* = 3926; P0 groups 2654:1,2624:32,2560:256 (p*-only, p*'s warp, p*'s block)
Q: expected Attempts 6393346 (host threads, HMX, --particles 9999)
```

SHA-256 of what produced and what holds the figures (`sha256sum`, the files are in the session scratchpad):

```
ce803508828c14ca75b040cbe419d2ff668e8d1f3658e42c371973c1b5160ef8 *w5-rule.py
c0aed7865071483aaa1d59f3542ba0c97b284ac8d5b4f9db4d67be55e9f5ca6c *w5-nsys.py
d3f884a833458c5ef2866344c526284b66af54b4e7cfb2b955c4a66a73122f68 *w5-rule-test.py
4fc6bb58a3d39122cc8bd9e35ef03f4ef982f632edf8a8a6d392e92ddb968a13 *w5-window-proof.py
d53931c677a8dec70f511b1619fba6f81f9a05e9e41f5f4b81c4d117bd229283 *windowC.sh
8b4b2d10d81b894c28a0b2f47d30ca4c6e554e633f8db44f56596c78f55640c1 *windowC-build.sh
9d6a993b9d79d83a1f10090e4a404ac4d77aff9ef1fdc27eda236613abd0d2ec *w5-hook.patch
f62580dbc1ff9908fa3313282cb1ac08537ec48ffb7f8c5be579c39f8b326909 *w4c-hook.patch
26e4283853577386bd4c5eb20881d102c66729d019c0e78083b1215f7d11b7b2 *fake-bin/fake-dotnet.py
b3f84cef9958a4fdca5d8efcccf857d1984cbc1d58215eadf70d87af5f104757 *fake-bin/nsys
190ce28e63191930a009f61b58bc7c372b9876c765633ec775f0709b9a97c228 *build-stamp.txt
110e4f3d32fb47b4331f79adbf5661744204a16ab65f8ee97f6f5c9a3b2915cf *windowC-run2.log
e626ec23ffd90c6e39adaadc9e0805cd6030b0130b269211621101112dc58c17 *windowC-run.log
110e4f3d32fb47b4331f79adbf5661744204a16ab65f8ee97f6f5c9a3b2915cf *windowC-20261003-0304/windowC.log
6941a235b080d7f7a0d924498395f8aeeb47416353debdd819afa18ba6d01228 *windowC-20261003-0304/p0p1.txt
76927ed59326c3d5028b6b4d92788deeeb7ef50bd4087f12dea4c0173e201645 *windowC-20261003-0304/p1-summary.json
82bbc4ff8cc3f72eeb50922c487df3c35bf4510057baa5a50bdcb1781e9ee20a *windowC-20261003-0304/p2.verdict.txt
7f93ce4210589ec2547d1c0da5642d6348ba22eaf255a834cf72cd114a0675c9 *windowC-20261003-0304/p2.rows.txt
f48cb3dbda2dd946f8ec8e8047c5f09840df7baf684dd7527a1824a0d906cfe7 *windowC-20261003-0304/p2.clocks.txt
3915414d22d737f88645f5914543b541baec7f4de7aa009e2ba14e8706acae43 *windowC-20261003-0304/decomp.nsys-rep
d484185f494f9a370683a60347160ab8c20b8ae6eb6b2e1c7f6ef3e5996a0fb6 *windowC-20261003-0304/decomp-dump.call1
c5ec2e81648308082894cacf1e58f7747acf2b42caacda2f716c8ebfcb2d1048 *windowC-20261003-0304/decomp-dump.call2
ce6014d7792b8315890d6d8c4e6e17f5cfe5ce93ec08b4a53112fe6d2fa74936 *windowC-20261003-0304/decomp-dump.control
3dd48803cf7f76c924d79b4e36c3af932f3d8ff8843f9db615809aacebce80c3 *windowC-20261003-0304/probe-A.nsys-rep
c083adab88fe33b7d72f932b1eb4064517813d1dc89267256a4e434eee4ad1d9 *windowC-20261003-0304/probe-G128.nsys-rep
f059b966b768c0fcb65a034752654838b37a173a4f5a5a1530d822f8196af64f *windowC-20261003-0304/probe-G64.nsys-rep
192a26ab31cdc8b69f33396a69367527223057f98bd123ed3cd0425e45d2f138 *windowC-20261003-0304/probe-G32.nsys-rep
3ecbc0d0a2fbe53f2f75485522dd072ebcc5eb9374f891499c711f354280da50 *windowC-20261003-0304/probe-R.nsys-rep
60cde127fe68bafa1b5e542aa47b29bddb0950dfa2936ddd2cd3e4524d12a01d  manifest of the 53 files p2/*.log (sha256sum *.log | sha256sum)
```

**3. Quiet checks** (`nvidia-smi`: utilisation, memory, SM clock, power, temperature; the compute-process list read 27 processes
each time, none work-like; then three `Get-Counter` readings 2 s apart, every one under 10 % CPU). The two readings of 100 % are
taken at the timestamp at which the preceding call ended; the rule's guard, no work-like process, held at every check:

| check | time | GPU util | memory | SM clock | power | °C |
|---|---|---|---|---|---|---|
| before-P0 | 03:04:58 | 0 % | 1798 MiB | 922 MHz | 28.80 W | 43 |
| before-P1-A | 03:05:26 | 1 % | 1758 MiB | 2790 MHz | 58.83 W | 45 |
| before-P1-G128 | 03:05:45 | 4 % | 1758 MiB | 2790 MHz | 63.07 W | 45 |
| before-P1-G64 | 03:06:03 | 0 % | 1758 MiB | 2790 MHz | 54.62 W | 45 |
| before-P1-G32 | 03:06:21 | 0 % | 1758 MiB | 2790 MHz | 49.03 W | 45 |
| before-P1-R | 03:06:39 | 4 % | 1758 MiB | 2790 MHz | 66.49 W | 46 |
| before-Q | 03:06:57 | 0 % | 1758 MiB | 2790 MHz | 55.52 W | 46 |
| round-1 | 03:07:21 | 100 % | 1758 MiB | 2790 MHz | 57.53 W | 47 |
| round-2 | 03:10:10 | 0 % | 1758 MiB | 2790 MHz | 53.96 W | 50 |
| round-3 | 03:12:59 | 8 % | 1758 MiB | 2790 MHz | 58.53 W | 52 |
| round-4 | 03:15:46 | 100 % | 1758 MiB | 2790 MHz | 62.86 W | 53 |
| after | 03:18:35 | 0 % | 1758 MiB | 2790 MHz | 56.51 W | 53 |

**4. Q** (the Attempts control, HMX, `--particles 9999`, once after P1): 5063 ± 30 (n = 3), Budget 8192, Launches 2, Attempts
6393346; the expected figure, taken before the window from host threads, was 6393346 (`windowC-build.sh`, `q expected`), A's is
6393938.

**5. P0 and P1 as read** (`p0p1.txt`; P0's D is n = 1, each T the median of three reruns; P1's D/T* is that arm's one call over
P0's median T*. P0 stands as a record, nothing is adopted on it):

```
# P0 (HMX, arm A, W4 hook): the decomposition of D/T*
control rerunsEqualBulk: PASS
control bulkEqualsExpected: PASS
control pstar: PASS
control bulkAttemptsAtPstar: PASS
control allGroupsEqualBulk: PASS
control one W4C-CONTROL line per group: PASS
control group 2654:1 groupEqualsBulk and its mismatches: PASS
control group 2624:32 groupEqualsBulk and its mismatches: PASS
control group 2560:256 groupEqualsBulk and its mismatches: PASS
W4B-CONTROL accelerator=Cuda hostThreads=False pstar=2654 bulkAttemptsAtPstar=3926 reruns=[3926,3926,3926] expectedAstar=3926 rerunsEqualBulk=True bulkEqualsExpected=True ownArgmax=2654 ownArgmaxAttempts=3926 particlesWhoseCountMovedInLastRerun=256 groups=2654:1,2624:32,2560:256 allGroupsEqualBulk=True
bulk launch: 40 x 256 threads, 196 registers, local 0 B/thread; D = 1891.736 ms
T* (group 2654:1, launch grid x block x regs [(1, 256, 196)]): 583.804, 583.705, 584.047 ms; median 583.804
T_w (group 2624:32, launch grid x block x regs [(1, 256, 196)]): 720.366, 720.882, 720.192 ms; median 720.366
T_b (group 2560:256, launch grid x block x regs [(1, 256, 196)]): 1521.954, 1522.290, 1521.440 ms; median 1521.954
D/T* = 3.240
T_w/T* = 1.234   (predicted under 'the SM': <= 1.3; information only)
T_b/T_w = 2.113   (predicted under 'the SM': 2.5-3.2; information only)
D/T_b = 1.243   (predicted under 'the SM': <= 1.1; information only)
T_b/T* = 2.607
Recorded; nothing is adopted on P0.
# P1 (HMX, one call per arm, W5 hook, Nsight Systems)
A: ok: block 256, grid 40, 196 registers, local 0 B/thread, groups per SM 1; D = 1891.167 ms; D/T* = 3.239
G128: ok: block 128, grid 79, 196 registers, local 0 B/thread, groups per SM 2; D = 1617.321 ms; D/T* = 2.770
G64: ok: block 64, grid 157, 196 registers, local 0 B/thread, groups per SM 4; D = 1368.269 ms; D/T* = 2.344
G32: ok: block 32, grid 313, 196 registers, local 0 B/thread, groups per SM 8; D = 1380.721 ms; D/T* = 2.365
R: ok: block 128, grid 79, 168 registers, local 0 B/thread, groups per SM 3; D = 1617.844 ms; D/T* = 2.771
```

**6. P2, all 53 rows**: Q as round 0, then the 52 rows of the four rounds (`--paths cuda --repeats 3`, every row's machine and commit as in section 2: RTX 5070
Ti, 16 logical CPUs, Windows 10.0.26200, Release, `6df81ae-dirty`; the quiet and clock readings are taken after the call). The W5 hook's three `W5-KERNEL` lines per call (group / minGroupsPerSm / groups per SM: A 0/0/1, G128 128/0/2, G64
64/0/4, G32 32/0/8, R 128/3/3) are in every `p2/*.log` of the run directory:

| round | slot | arm | F | particles/s (± spread, n = 3) | Budget | Launches | Attempts | time | SM clock | temp °C | W |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 0 | Q | HMX | 5063 ± 30 | 8192 | 2 | 6393346 | 03:07:21 | 2790 MHz | 47 | 57.53 |
| 1 | 0 | A | HMX | 5070 ± 30 | 8192 | 2 | 6393938 | 03:07:44 | 2790 MHz | 47 | 62.83 |
| 1 | 0 | A | HPEPA3 | 43683 ± 158 | 8192 | 2 | 1396212 | 03:08:00 | 2790 MHz | 47 | 60.36 |
| 1 | 1 | G128 | HMX | 5881 ± 41 | 8192 | 2 | 6393938 | 03:08:10 | 2790 MHz | 47 | 54.62 |
| 1 | 1 | G128 | HPEPA3 | 59866 ± 460 | 8192 | 2 | 1396212 | 03:08:23 | 2790 MHz | 48 | 61.34 |
| 1 | 2 | G64 | HMX | 6878 ± 72 | 8192 | 2 | 6393938 | 03:08:32 | 2790 MHz | 49 | 68.55 |
| 1 | 2 | G64 | HPEPA3 | 60470 ± 1070 | 8192 | 2 | 1396212 | 03:08:44 | 2790 MHz | 49 | 65.75 |
| 1 | 3 | G32 | HMX | 6831 ± 57 | 8192 | 2 | 6393938 | 03:08:53 | 2790 MHz | 49 | 72.30 |
| 1 | 3 | G32 | HPEPA3 | 60679 ± 1687 | 8192 | 2 | 1396212 | 03:09:05 | 2790 MHz | 50 | 66.07 |
| 1 | 4 | R | HMX | 5861 ± 32 | 8192 | 2 | 6393938 | 03:09:16 | 2790 MHz | 50 | 57.60 |
| 1 | 4 | R | HPEPA3 | 56626 ± 3658 | 8192 | 2 | 1396212 | 03:09:29 | 2790 MHz | 50 | 59.90 |
| 1 | 5 | P | HMX | 3928 ± 22 | 2048 | 3 | 6393938 | 03:09:42 | 2790 MHz | 50 | 53.01 |
| 1 | 6 | A | HMX | 5069 ± 31 | 8192 | 2 | 6393938 | 03:09:54 | 2790 MHz | 50 | 57.46 |
| 1 | 6 | A | HPEPA3 | 43679 ± 169 | 8192 | 2 | 1396212 | 03:10:10 | 2790 MHz | 51 | 63.61 |
| 2 | 0 | A | HMX | 5070 ± 29 | 8192 | 2 | 6393938 | 03:10:34 | 2790 MHz | 50 | 56.60 |
| 2 | 0 | A | HPEPA3 | 43681 ± 168 | 8192 | 2 | 1396212 | 03:10:49 | 2790 MHz | 51 | 62.55 |
| 2 | 1 | G64 | HMX | 6887 ± 57 | 8192 | 2 | 6393938 | 03:10:59 | 2790 MHz | 51 | 62.34 |
| 2 | 1 | G64 | HPEPA3 | 58818 ± 3825 | 8192 | 2 | 1396212 | 03:11:11 | 2790 MHz | 51 | 63.56 |
| 2 | 2 | G32 | HMX | 6832 ± 58 | 8192 | 2 | 6393938 | 03:11:20 | 2790 MHz | 51 | 63.45 |
| 2 | 2 | G32 | HPEPA3 | 57387 ± 3482 | 8192 | 2 | 1396212 | 03:11:33 | 2790 MHz | 51 | 60.81 |
| 2 | 3 | R | HMX | 5883 ± 38 | 8192 | 2 | 6393938 | 03:11:43 | 2790 MHz | 51 | 54.70 |
| 2 | 3 | R | HPEPA3 | 59123 ± 3402 | 8192 | 2 | 1396212 | 03:11:55 | 2790 MHz | 52 | 65.24 |
| 2 | 4 | P | HMX | 3928 ± 22 | 2048 | 3 | 6393938 | 03:12:09 | 2790 MHz | 52 | 54.35 |
| 2 | 5 | G128 | HMX | 5882 ± 39 | 8192 | 2 | 6393938 | 03:12:19 | 2790 MHz | 52 | 59.48 |
| 2 | 5 | G128 | HPEPA3 | 59177 ± 220 | 8192 | 2 | 1396212 | 03:12:31 | 2790 MHz | 53 | 68.76 |
| 2 | 6 | A | HMX | 5068 ± 30 | 8192 | 2 | 6393938 | 03:12:43 | 2790 MHz | 52 | 61.34 |
| 2 | 6 | A | HPEPA3 | 43685 ± 162 | 8192 | 2 | 1396212 | 03:12:58 | 2790 MHz | 53 | 66.12 |
| 3 | 0 | A | HMX | 5067 ± 30 | 8192 | 2 | 6393938 | 03:13:22 | 2790 MHz | 52 | 60.21 |
| 3 | 0 | A | HPEPA3 | 43666 ± 168 | 8192 | 2 | 1396212 | 03:13:38 | 2790 MHz | 53 | 65.29 |
| 3 | 1 | G32 | HMX | 6823 ± 52 | 8192 | 2 | 6393938 | 03:13:47 | 2790 MHz | 52 | 59.56 |
| 3 | 1 | G32 | HPEPA3 | 60616 ± 1161 | 8192 | 2 | 1396212 | 03:13:59 | 2790 MHz | 53 | 70.61 |
| 3 | 2 | R | HMX | 5882 ± 40 | 8192 | 2 | 6393938 | 03:14:10 | 2790 MHz | 53 | 64.46 |
| 3 | 2 | R | HPEPA3 | 59702 ± 3443 | 8192 | 2 | 1396212 | 03:14:22 | 2790 MHz | 53 | 63.49 |
| 3 | 3 | P | HMX | 3928 ± 21 | 2048 | 3 | 6393938 | 03:14:35 | 2790 MHz | 53 | 54.55 |
| 3 | 4 | G128 | HMX | 5882 ± 42 | 8192 | 2 | 6393938 | 03:14:46 | 2790 MHz | 53 | 57.99 |
| 3 | 4 | G128 | HPEPA3 | 59595 ± 3016 | 8192 | 2 | 1396212 | 03:14:58 | 2790 MHz | 53 | 65.43 |
| 3 | 5 | G64 | HMX | 6890 ± 55 | 8192 | 2 | 6393938 | 03:15:07 | 2790 MHz | 53 | 67.01 |
| 3 | 5 | G64 | HPEPA3 | 61062 ± 211 | 8192 | 2 | 1396212 | 03:15:19 | 2790 MHz | 53 | 63.74 |
| 3 | 6 | A | HMX | 5067 ± 31 | 8192 | 2 | 6393938 | 03:15:31 | 2790 MHz | 53 | 58.53 |
| 3 | 6 | A | HPEPA3 | 43676 ± 156 | 8192 | 2 | 1396212 | 03:15:46 | 2790 MHz | 53 | 62.86 |
| 4 | 0 | A | HMX | 5067 ± 29 | 8192 | 2 | 6393938 | 03:16:10 | 2790 MHz | 52 | 58.05 |
| 4 | 0 | A | HPEPA3 | 43675 ± 175 | 8192 | 2 | 1396212 | 03:16:26 | 2790 MHz | 53 | 63.15 |
| 4 | 1 | R | HMX | 5879 ± 43 | 8192 | 2 | 6393938 | 03:16:36 | 2790 MHz | 53 | 57.06 |
| 4 | 1 | R | HPEPA3 | 60126 ± 3399 | 8192 | 2 | 1396212 | 03:16:48 | 2790 MHz | 53 | 62.87 |
| 4 | 2 | P | HMX | 3927 ± 21 | 2048 | 3 | 6393938 | 03:17:02 | 2790 MHz | 53 | 53.86 |
| 4 | 3 | G128 | HMX | 5882 ± 38 | 8192 | 2 | 6393938 | 03:17:12 | 2790 MHz | 53 | 58.55 |
| 4 | 3 | G128 | HPEPA3 | 59679 ± 1638 | 8192 | 2 | 1396212 | 03:17:24 | 2790 MHz | 53 | 64.91 |
| 4 | 4 | G64 | HMX | 6888 ± 57 | 8192 | 2 | 6393938 | 03:17:34 | 2790 MHz | 54 | 63.19 |
| 4 | 4 | G64 | HPEPA3 | 58880 ± 484 | 8192 | 2 | 1396212 | 03:17:46 | 2790 MHz | 54 | 62.66 |
| 4 | 5 | G32 | HMX | 6830 ± 56 | 8192 | 2 | 6393938 | 03:17:55 | 2790 MHz | 54 | 66.92 |
| 4 | 5 | G32 | HPEPA3 | 58847 ± 237 | 8192 | 2 | 1396212 | 03:18:07 | 2790 MHz | 54 | 63.90 |
| 4 | 6 | A | HMX | 5064 ± 29 | 8192 | 2 | 6393938 | 03:18:19 | 2790 MHz | 54 | 60.12 |
| 4 | 6 | A | HPEPA3 | 43669 ± 160 | 8192 | 2 | 1396212 | 03:18:35 | 2790 MHz | 54 | 65.10 |

**7. The reader's verdict** (`w5-rule.py`, `p2.verdict.txt`, exit 0; the tables are the rule's per-round readings):

```
== HMX (particles/s, cycle 1; g = arm / A_i, A_i = mean of the round's two A figures, nu_i = their relative difference)
round | A1 | A2 | A_i | nu_i % | G128 g | G64 g | G32 g | R g | P g | R/G128
1 | 5070 | 5069 | 5069.5 | 0.02 | 1.160 | 1.357 | 1.347 | 1.156 | 0.775 | 0.997
2 | 5070 | 5068 | 5069.0 | 0.04 | 1.160 | 1.359 | 1.348 | 1.161 | 0.775 | 1.000
3 | 5067 | 5067 | 5067.0 | 0.00 | 1.161 | 1.360 | 1.347 | 1.161 | 0.775 | 1.000
4 | 5067 | 5064 | 5065.5 | 0.06 | 1.161 | 1.360 | 1.348 | 1.161 | 0.775 | 0.999
   predicted G128 on HMX: 1.06 (information only)
   predicted G64 on HMX: 1.34 (information only)
   predicted G32 on HMX: 1.43 (information only)
== HPEPA3 (particles/s, cycle 1; g = arm / A_i, A_i = mean of the round's two A figures, nu_i = their relative difference)
round | A1 | A2 | A_i | nu_i % | G128 g | G64 g | G32 g | R g | R/G128
1 | 43683 | 43679 | 43681.0 | 0.01 | 1.371 | 1.384 | 1.389 | 1.296 | 0.946
2 | 43681 | 43685 | 43683.0 | 0.01 | 1.355 | 1.346 | 1.314 | 1.353 | 0.999
3 | 43666 | 43676 | 43671.0 | 0.02 | 1.365 | 1.398 | 1.388 | 1.367 | 1.002
4 | 43675 | 43669 | 43672.0 | 0.01 | 1.367 | 1.348 | 1.347 | 1.377 | 1.007
   predicted G128 on HPEPA3: 1.00 (information only)
   predicted G64 on HPEPA3: 1.00 (information only)
   predicted G32 on HPEPA3: 1.00 (information only)
G128: medians HMX 1.161, HPEPA3 1.366; QUALIFIES (g >= 1.10 in every valid round on HMX, HPEPA3)
G64: medians HMX 1.359, HPEPA3 1.366; QUALIFIES (g >= 1.10 in every valid round on HMX, HPEPA3)
G32: medians HMX 1.348, HPEPA3 1.368; QUALIFIES (g >= 1.10 in every valid round on HMX, HPEPA3)
R: medians HMX 1.161, HPEPA3 1.360; R/G128 medians HMX 1.000, HPEPA3 1.000; does not qualify (g >= 1.10 in every valid round on HMX, HPEPA3; R / G128: no formulation has g >= 1.10 in every valid round)
VERDICT: ADOPT: adopt G64 (geometric mean of the two medians 1.363; qualified G128 1.259, G64 1.363, G32 1.358; within 0.02 of the top: G64, G32; the larger group wins)
```

<a id="w5-stop-2026-10-03"></a>
<a id="eb-preconditions-2026-09-28"></a>
<a id="w2-rule-2026-10-02"></a>

## 2026-10-03 — from "### W5: launch shape, pre-registered (2026-10-03)" (P1, Stop), "### E-B: budget sweep (2026-09-28)" (the preconditions paragraph) and "### Pre-registered before any new figure (2026-10-02)" (the W2 rule) — window C's first attempt, the two defects of the pre-registration it showed, and two spent texts moved to make room

**1. Window C, first attempt (2026-10-03 01:49-01:53 local; the rows say 2026-10-02 because the tool dates in UTC).** The
run stopped in round 1, slot 5, on the stop "P's Attempts equal to A's". Hook worktrees at `90c059c`, rows read
`90c059c-dirty`. Directory `wave11/w5/windowC-20261003-0149/` of the orchestrating session's scratchpad, not in the
repository. Round 1 has no closing A, so it has no A_i, ν_i or g; it is **seen and not counted**. The counted window is a
full rerun of P0-P3 at the amendment commit.

**2. The two defects** (arbiter's ruling, 2026-10-03):

- **Stop: the control could never pass.** `HISTORY.md#eb-budget-sweep-2026-09-28` reads HMX Attempts 6393938 and HPEPA3
  1396212 at every budget from 8 to 8192, on host16 and cuda ("Attempts is budget-invariant"). The pre-registration's
  premise, that P at budget 2048 changes the Attempts, was refuted before the design, and the proofs assumed it
  (`w5-rule-test.py`, `fake-dotnet.py` gave P A's Attempts + 120000). P read 6393938 with 3 Launches. **Now:** Q, the Attempts
  control (A at `--particles 9999`, HMX, once after P1), whose expected Attempts come from host threads before the window
  (host threads and CUDA give equal Attempts in every recorded batched row) and differ from W2's 6393938; P stays the
  throughput control, band unchanged, its Attempts only recorded.
- **P1: the probe could not be read.** The six `nsys` console logs of the run contain no hook line and no figure row: under
  `nsys profile` the profiled process's stdout never reaches the console. Every P2 log has three `W5-KERNEL` lines as
  designed (group / minGroupsPerSm / groups per SM: A 0/0/1, G128 128/0/2, G64 64/0/4, G32 32/0/8, R 128/3/3). The first
  reading of P1 (`p0p1.txt` of the run) therefore voids every arm ("0 W5-KERNEL lines in the console log, expected 1"),
  although every trace was as designed; had the window finished, the verdict would have been a false "none qualifies".
  **Now:** the hook line is read from the arm's P2 logs; the trace checks stay. The fake `nsys` of the proofs passed the target's
  output through, the real one does not.

⚠ 2026-10-03, after round 1 was seen, P1 (full text): was the `nsys` call's console (where `windowC.sh` read the hook's
line), which never shows the profiled process's output (every arm void, every trace as designed), now the P2 logs. No
threshold or comparison moves.

⚠ 2026-10-03, after round 1 was seen, Stop (full text): was "P's equal to A's", now Q's clause: E-B read HMX's Attempts 6393938
at every budget from 8 to 8192, so the stop that fired in round 1 could never pass. No threshold, candidate arm or comparison
moves; round 1 has no closing A and is not counted.

P1 and Stop as they stood in `90c059c` (the text of the `BOOT.md` bullets, before the amendment):

> - **P1** (HMX, Nsight Systems, one call per arm): block = G (A: 256); 196 registers
>   and no local memory for A and the G arms; at most 168 registers and 3 groups per SM
>   for R; the other three kernels as in W3. A mismatch voids the arm.

> - **Stop** on a row not a figure or "warm-up not isolated", n ≠ 3, a wrong Budget,
>   Launches ≠ 2 (P aside), A's Attempts varying, a G arm's unlike A's, P's equal to
>   A's, A's first call > 5 % off W2, an order off the rotation, a red guard. ν_i > 3 %
>   voids the round for that formulation; two void rounds of one formulation void the
>   window; P's median g over HMX's valid rounds outside 0.74-0.81 voids the reading.

**3. Round 1's eleven rows, seen and not counted** (`--paths cuda --repeats 3`, HMX then HPEPA3 per arm; the machine column of
every row is the W2 table's: RTX 5070 Ti, 16 logical CPUs, Windows 10.0.26200, Release):

| round | slot | arm | F | figure (particles/s, cycle 1) | Budget | Launches | Attempts | Commit |
|---|---|---|---|---|---|---|---|---|
| 1 | 0 | A | HMX | 5069 ± 31 (n=3) | 8192 | 2 | 6393938 | 90c059c-dirty |
| 1 | 0 | A | HPEPA3 | 43680 ± 194 (n=3) | 8192 | 2 | 1396212 | 90c059c-dirty |
| 1 | 1 | G128 | HMX | 5882 ± 40 (n=3) | 8192 | 2 | 6393938 | 90c059c-dirty |
| 1 | 1 | G128 | HPEPA3 | 61235 ± 1016 (n=3) | 8192 | 2 | 1396212 | 90c059c-dirty |
| 1 | 2 | G64 | HMX | 6883 ± 64 (n=3) | 8192 | 2 | 6393938 | 90c059c-dirty |
| 1 | 2 | G64 | HPEPA3 | 58606 ± 1117 (n=3) | 8192 | 2 | 1396212 | 90c059c-dirty |
| 1 | 3 | G32 | HMX | 6834 ± 54 (n=3) | 8192 | 2 | 6393938 | 90c059c-dirty |
| 1 | 3 | G32 | HPEPA3 | 58556 ± 4944 (n=3) | 8192 | 2 | 1396212 | 90c059c-dirty |
| 1 | 4 | R | HMX | 5880 ± 42 (n=3) | 8192 | 2 | 6393938 | 90c059c-dirty |
| 1 | 4 | R | HPEPA3 | 58688 ± 1514 (n=3) | 8192 | 2 | 1396212 | 90c059c-dirty |
| 1 | 5 | P | HMX | 3928 ± 23 (n=3) | 2048 | 3 | 6393938 | 90c059c-dirty |

Nothing is derived from these rows: the A bracket is open (no second A), so there is no A_i, ν_i or g.

**4. P0 and P1 as read by the first `w5-nsys.py`** (`p0p1.txt` of the run; P0 stands as a record, nothing is adopted on it; the
P1 part is the old, voiding reading of defect 2; in P0, D is n = 1 and each T is the median of 3). P0's T_b/T_w 2.113 (predicted
2.5-3.2) and D/T_b 1.243 (predicted ≤ 1.1) are findings for the next design session:

```
# P0 (HMX, arm A, W4 hook): the decomposition of D/T*
control rerunsEqualBulk: PASS
control bulkEqualsExpected: PASS
control pstar: PASS
control bulkAttemptsAtPstar: PASS
control allGroupsEqualBulk: PASS
control one W4C-CONTROL line per group: PASS
control group 2654:1 groupEqualsBulk and its mismatches: PASS
control group 2624:32 groupEqualsBulk and its mismatches: PASS
control group 2560:256 groupEqualsBulk and its mismatches: PASS
W4B-CONTROL accelerator=Cuda hostThreads=False pstar=2654 bulkAttemptsAtPstar=3926 reruns=[3926,3926,3926] expectedAstar=3926 rerunsEqualBulk=True bulkEqualsExpected=True ownArgmax=2654 ownArgmaxAttempts=3926 particlesWhoseCountMovedInLastRerun=256 groups=2654:1,2624:32,2560:256 allGroupsEqualBulk=True
bulk launch: 40 x 256 threads, 196 registers, local 0 B/thread; D = 1891.702 ms
T* (group 2654:1, launch grid x block x regs [(1, 256, 196)]): 584.274, 583.852, 584.022 ms; median 584.022
T_w (group 2624:32, launch grid x block x regs [(1, 256, 196)]): 720.233, 720.801, 720.245 ms; median 720.245
T_b (group 2560:256, launch grid x block x regs [(1, 256, 196)]): 1522.114, 1522.224, 1521.334 ms; median 1522.114
D/T* = 3.239
T_w/T* = 1.233   (predicted under 'the SM': <= 1.3; information only)
T_b/T_w = 2.113   (predicted under 'the SM': 2.5-3.2; information only)
D/T_b = 1.243   (predicted under 'the SM': <= 1.1; information only)
T_b/T* = 2.606
Recorded; nothing is adopted on P0.
# P1 (HMX, one call per arm, W5 hook, Nsight Systems)
A: VOID: 0 W5-KERNEL lines in the console log, expected 1
G128: VOID: 0 W5-KERNEL lines in the console log, expected 1
G64: VOID: 0 W5-KERNEL lines in the console log, expected 1
G32: VOID: 0 W5-KERNEL lines in the console log, expected 1
R: VOID: 0 W5-KERNEL lines in the console log, expected 1
```

**5. The two texts moved out of `BOOT.md`** to make room for the amendment, verbatim, in the order of `BOOT.md`.

The preconditions paragraph of "### E-B: budget sweep (2026-09-28)" (2026-09-28; superseded by the 2026-10-01 adoption,
which the section keeps):

> **Before any default moves**, all four preconditions this plan's own E-B rule lists
> must hold first, none started by this task: `RunDiagnostics` records
> `AttemptsPerLaunch` (a public surface change); link 3 is green at the new budget, CPU
> and CUDA, five formulations, eight seeds; the tier table is re-measured at the new
> budget; `src/Simulation/BOOT.md` "## Budget measurement" is re-based.

The W2 rule body of "### Pre-registered before any new figure (2026-10-02)" (spent: it ran 2026-10-02 and is recorded in
`## Figures`; the ⚠ 2026-10-02 below it stays in `BOOT.md`):

> - **W2, Stage 0' (re-measure at the shipped default).** After a Release build, `dotnet
>   run -c Release --no-build --project tests/Benchmarks -- --formulation <F> --paths
>   reference,host1,host16,cpu,cuda,original --repeats 3` for HPEPA3 and HMX, no
>   overrides, Stage 0's quiet checks before, between and after, provenance without
>   `-dirty`. It replaces `## Figures`, applies R0 at the default, re-dates criterion 1
>   and the root speed criterion. The `host1` row is expected to fall (it is one CPU now).
>   **Stop and explain before recording** if: a batched row's Launches is not 2 (E-B, one
>   per cycle at 8192); CUDA HPEPA3 has its mean outside 43530 ± 162, CUDA HMX outside
>   5042 ± 29, or host16 HMX outside 2085 ± 5 (E-B at 8192; `874d81c` put
>   `CycleStatistics.Compute` inside the window since); R0 fails at the default (bisect
>   `2dc74ec..HEAD`).

<a id="stage1-section-2026-09-28"></a>
<a id="bisection-pointer-2026-09-28"></a>

## 2026-10-03 — from "### Stage 1: profile (2026-09-28)" and "### HPEPA3 CUDA regression bisection (2026-09-28)" — the two bodies as they stood, moved to make room for the W5 pre-registration

Moved to keep `BOOT.md` inside its limit of 400 non-blank lines with text A of W5
(`### W5: launch shape, pre-registered (2026-10-03)`); each section leaves its decision
and its deciding figure in place. The text as it stood, in the order of `BOOT.md`:

**1. The body of "### Stage 1: profile (2026-09-28)"** (including the ⚠ 2026-10-02 it
carried; the per-launch figures it cites are in `#stage1-profile-details-2026-09-28`):

> Nsight Systems 2026.3.2 and Nsight Compute 2026.3.0 are both installed on the
> reference machine (`Program Files\NVIDIA Corporation`). `nsys profile --stats=true`
> ran cleanly on one HPEPA3 and one HMX `cuda` call (`--paths cuda --repeats 1`, cycle 0
> + cycle 1). `ncu` refused every launch with `ERR_NVGPUCTRPERM`: GPU performance
> counters are restricted to an elevated account on this machine's driver, and granting
> that is a system-settings change outside this task's reach — a permissions gap, not a
> missing tool.
>
> Per-launch kernel-trace figures (HPEPA3: 7 launches, active-thread collapse ≥97× while
> duration falls only 4.2-4.5×; HMX: 187 launches, active-thread collapse 10× while
> duration falls only 1.28-2.29×; host-side API time under 1 % of the run on both) are
> in `HISTORY.md#stage1-profile-details-2026-09-28`.
>
> **Rule verdicts:**
> - **R1 (E-B first): triggered.** HMX cycle 1's own figures satisfy the rule's literal
>   bound: duration falls only 1.28× while active threads fall exactly 10×. Cycle 0
>   shows the same shape at a slightly wider margin (2.29×). HPEPA3 shows the identical
>   disproportion at a softer ratio (4.2-4.5× duration drop against a ≥97× active-count
>   collapse, since HPEPA3 has far fewer launches per cycle to spread the tail over).
>   The rule's second clause (under-10 %-active launches over 50 % of the cycle) does
>   not itself fire: measured at 18.4-19.5 % (HPEPA3) and 29.8-39.5 % (HMX), all under
>   50 %. The first clause is what selects E-B.
> - **R2 (fix the host loop): not triggered.** Host-side time outside
>   `cuCtxSynchronize` is under 1 % of the run on both formulations (measured above), so
>   no host-loop fix is selected.
> - **R3, R4, R5, R6: not measured this round.** Each needs a hardware performance
>   counter (warp execution efficiency and occupancy for R3, DRAM/L2 throughput for R4,
>   `qks1`'s own share of global loads for R5, atomic stall reasons for R6) that only
>   `ncu` supplies here, and `ncu` is blocked by `ERR_NVGPUCTRPERM` without an elevated
>   account. Left open for a session with that permission granted; recorded as
>   unmeasured, not guessed at or recorded as a verdict.
>
> ⚠ 2026-10-02: was "29.8-39.5 % (HMX)", the upper figure the inclusive (at most 1024
> threads) comparison and the rest strict, now 29.8-34.4 % (strict; all under 50 %, the
> verdict stands) → HISTORY.md#stage1-profile-details-2026-09-28 (two more figures there)
>
> R1 selected E-B without waiting on R3-R6 (its own rule text: "run E-B first"); E-B ran
> this task, below. R3-R6 stay unmeasured, open for a session with `ncu` access.

**2. The body of "### HPEPA3 CUDA regression bisection (2026-09-28)"** (already a pointer
to `#bisection-section-2026-09-28` and `#hpepa3-cuda-bisect-2026-09-28`, with its
decision and the ⚠ 2026-10-02; the ⚠'s full text is also in
`#benchmarks-boot-trims-2026-10-02`):

> Section moved 2026-10-02 → `HISTORY.md#bisection-section-2026-09-28`; the commit list
> and the probe log → `HISTORY.md#hpepa3-cuda-bisect-2026-09-28`. Decision: the step lies
> between `51e66c9` (45322 ± 276, n=3) and `9c8a161` (35264 ± 122, n=3), and is `d735f3e`
> flipping `SimulationOptions.Streams`' default from `Original` to `Independent`, which
> this node's `Program.cs` inherits (API.md names no `--layout` flag). The deciding
> figure, a positive control: `51e66c9` rebuilt with `Streams = Independent` forced read
> 35263 ± 118 (n=3), the slow band, on the same commit. So it is not an inefficiency in
> any commit of the four watched paths.
>
> ⚠ 2026-10-02: was "the `Independent` layout's per-particle stream derivation costs
> measurably more" with a lead to make it cheaper, now the mechanism is not measured and
> the derivation refuted: `Kernel_DeriveStreams` 0.13-0.14 ms per cycle on HPEPA3 against
> a step of 0.63 s per cycle; lead dropped → HISTORY.md#bisection-section-2026-09-28 and
> `#benchmarks-boot-trims-2026-10-02`

<a id="benchmarks-boot-trims-2026-10-02"></a>

## 2026-10-02 — from `## Implementation notes` (Host-thread counts, Provenance's commit), `## Figures` and "### HPEPA3 CUDA regression bisection" — four passages moved to make room for the W3/W4 record

Moved to keep `BOOT.md` inside its limit of 400 non-blank lines with the W3/W4 record
(`#w34-2026-10-02`); each leaves its decision and a pointer in place. The text as it
stood, in the order of `BOOT.md`:

**1. The proof in the ⚠ 2026-10-02 of "Host-thread counts"** (the sentence between "Stage
0)." and "`host1` now pins its child"):

> Proof, the same child command
>   (HPEPA3, `--particles 5000`, `PROPSTRUCT_NO_CUDA=1`, no figure kept), child CPU time ÷
>   wall time: unpinned 2.42, 2.55, 2.60, 2.48 (n = 4, wall 1.85-1.96 s); pinned to one
>   logical CPU 0.996, 1.001, 0.996 (n = 3, wall 3.8-4.25 s). 

**2. The proof in "Provenance's commit"** (from "Proof," to the end of the bullet):

> cannot answer. Proof, `--paths cpu` (measures nothing): `47ddf16-dirty` with two
>   modified files, `4b48e27` on the clean commit, `4b48e27-dirty` with one untracked
>   file, `4b48e27` again once removed. Stage 0's rows read `ce30f62`, whose
>   `FigureRow.cs` has no Launches column; `13ef9f1`, which adds it, is not an ancestor of
>   `ce30f62`: they came from a dirty tree, the case the suffix now shows.

**3. The paragraph "The pre-registered stop fired on HMX" in `## Figures`**; the A/B/A's
own table is in `#figures-w2-2026-10-02`:

> **The pre-registered stop fired on HMX and was explained before recording**: CUDA HMX
> 5095 ± 2 outside E-B's 5042 ± 29, host16 HMX 2071 ± 18 outside 2085 ± 5 (HPEPA3 inside,
> 43503 ± 88 against 43530 ± 162; R0 held). An A/B/A on HMX, `host16` and `cuda`,
> `--attempts-per-launch 8192`, n = 3 per invocation, quiet checks between, A = `7e8f6f7`,
> B = `2dc74ec` (E-B's commit, Release, detached worktree), 21:29-21:31: CUDA 5053 ± 27,
> 5030 ± 24 (B), 5059 ± 31; host16 2081 ± 18, 1916 ± 254 (B), 1932 ± 242. E-B's commit
> reads in today's range, minutes apart, so the shift does not come from the commits since
> E-B. Over four invocations CUDA HMX reads 5030-5095 and host16 HMX 1916-2081 (accepted
> particles/s, mean ± spread, n = 3 each): the variation between invocations exceeds the
> spread within one (± 2 to ± 31 on CUDA, up to ± 254 on host16).

**4. The ⚠ 2026-10-02 under "### HPEPA3 CUDA regression bisection"**:

> ⚠ 2026-10-02: was "the `Independent` layout's per-particle stream derivation costs
> measurably more on this branch-divergent, tail-heavy kernel (Stage 1 above: 'bound by
> divergence and tails')" and a lead "whether `Independent`'s own derivation could be made
> cheaper", now the mechanism is not measured and the derivation is refuted as the cause:
> the Stage 1 trace has `Kernel_DeriveStreams` at 0.13-0.14 ms per cycle on HPEPA3
> (0.03 ms on HMX) against a step of 0.63 s per cycle (100000 particles at 35264 against
> 45322 per second). The lead is dropped; which part of the layout costs the 0.63 s is not known
> → HISTORY.md#bisection-section-2026-09-28

<a id="w34-2026-10-02"></a>

## 2026-10-02 — W3 (nsys timeline at budget 8192) and W4 (HMX discriminator): the window, the readings, the reader's outputs, where the scripts live

The window was run by the orchestrator on 2026-10-02, 23:45-23:46 local time, booked
with the neighbouring session that shares the GPU. This entry and the `BOOT.md` record
(`### W3/W4`) were written afterwards, documents only: no GPU, no timing, no profiler
in the recording session. Nothing here is asserted by a test (this node's first
invariant).

**What was timed.** Commit `d499816348`, Release, .NET 10, Nsight Systems
`2026.3.2.313-263238521929v0`, machine as `BOOT.md`'s figures (AMD64 Family 25 Model
97, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti). `git diff --stat d499816 09664f9 --
src`, the head this record was written at, lists eleven files, all under
`src/Statistics`: documents, a map and a generated listing as `.txt`, and the Python
that reads the executable's listing. `git diff --name-only d499816 09664f9 -- '*.cs'
'*.csproj' '*.props' '*.targets' global.json` lists only
`tests/Statistics.Tests/CyclePlaneListingMapTests.cs` and `CyclePlaneListingTests.cs`,
tests. The timed code (every `.cs` of `src`, the projects and the build properties) is
therefore identical at `09664f9`. Profiled runs: `nsys profile` of the
`--paths cuda --repeats 1` call, HPEPA3 and HMX, each formulation's own `N` (100000
and 10000), budget 8192 (the default), cycle 0 and cycle 1; and the same HMX call from
a detached worktree at the same commit with `w4b-hook.patch` applied (a dump and a
p*-only rerun inside `Engine`; the hook is not committed anywhere). `Execution.dll` as
built: SHA-256 `f5928308...a7365` (clean) and `09748016...eca5` (hook build); the full
sums are in `windowB.log`.

**Quiet checks** (`windowB.sh`: `nvidia-smi` utilisation, memory, SM clock, power and
temperature; the compute-process list, 27 processes each time, none work-like; then
three `Get-Counter` `\Processor(_Total)\% Processor Time` readings, 2 s apart):

| Check | Utilisation | Memory | SM clock | Power | `Get-Counter` readings |
|---|---|---|---|---|---|
| before, 23:45:39 | 0 % | 1887 MiB | 922 MHz | 28.95 W | 4.5, 2.6, 6.6 |
| between 1, 23:45:56 | 0 % | 1909 MiB | 2790 MHz | 46.76 W | 3.4, 4.2, 6.8 |
| between 2, 23:46:10 | 0 % | 1903 MiB | 2790 MHz | 52.96 W | 5.6, 2.2, 8.2 |
| after, 23:46:26 | 1 % | 1908 MiB | 2790 MHz | 49.79 W | 3.6, 3.8, 4.1 |

The three profiled runs started at 23:45:47 (HPEPA3), 23:46:03 (HMX) and 23:46:17 (HMX
with the hook). The clocks log of the hook run (`HMX-8192-w4b.clocks.csv`, 37 samples
about 0.26 s apart) reads 2790 MHz in every sample at utilisation 99-100 %: no
throttling visible at that resolution.

**The W4b run and the reader.** The hook's `W4B-CONTROL` line went to the dump's
`.control` file and not to the profiled console, so the window script's reader call on
the W4b run failed (`READER FAILED on the W4b run`, then `NO W4B-CONTROL LINE: the hook
did not run` in `windowB.log`; the hook had run). The orchestrator re-ran the reader on
the same traces with `--control-log HMX-8192-w4b-dump.control`; the W4b output below is
that re-run's. The two other outputs are the window script's own.

**W4a** (host threads, outside any timing window; `w4a.txt`): HMX seed 0, cycle 1,
`p* = 2654` (the launch index, which is the kernel's thread index; the stream ordinal
is 10000 + 2654), `A* = 3926` against a mean of 393.18 and a median of 272 (9.99 times
the mean); the next three are 3426, 3421, 3086. Sum of attempts 3931843 over the cycle
(call 2) and 2462095 over cycle 0 (call 1), together 6393938 = the run's own `Attempts`
in `BOOT.md`'s W2 HMX row. Attempt-level SIMT efficiency, the sum of attempts over lane
count times the sum of group maxima: 0.2454 for 32-particle groups, 0.1592 for
256-particle groups (cycle 0: 0.2556 and 0.1658). It assumes every attempt costs the
same and counts attempts, not instructions.

**W4 as the hook printed it** (`HMX-8192-w4b-dump.control`; the reader's `## W4` block
is below): control `PASS`, with the bulk launch's count at p* equal to A* (3926), the
three reruns each 3926, and CUDA's own argmax equal to p* (2654, 3926 attempts). D is
1900.417 ms in the hook run and 1899.210 ms in the clean run (0.06 % apart, two
profiled processes); T* = 585.643, 585.295, 586.451 ms (n = 3, median 585.643); D over
the median T* is 3.245 (3.247 to 3.241 over the range of T*). Time per attempt of the
p*-only launch: 585.643 / 3926 = 0.149 ms. In the W4b reader's cycle 1 the three
reruns are launches 2 to 4 of 256 threads, which is why its "launches under 10 %" line
reads 48.0 %: that is the hook's reruns, not a property of the cycle.

**R3 arithmetic as the reader prints it** (65536 registers per SM, allocation unit
256): 196 registers per thread is 6400 per warp, 8 warps per 256-thread block, one
resident block per SM. The SM count (70) is the performance review's figure, not read
from a trace; with it the HMX grid of 40 blocks leaves 30 SMs without a block.

**Where the scripts and patches live.** In the orchestrating session's scratchpad, not
in this repository: `scratchpad/wave11/w34/` of session
`5fb70b6c-2930-4552-90e9-860978d1766d`. SHA-256 of what produced the figures; the
traces are in its `windowB-20261002-2345/`:

```text
ce9009f95af64ce1a9c31dc58c8ce8cde64f191143d6cc0ac9b2c20fd2f72979  w34-reader.py
235293e149473a2d98b10ee28d7e746dcb59140fe1973421c5e1ed5996745687  w34-reader-proof.py
a40e0f5dce4bf8a38f60b798fbadc6fd588eac7b7c9371493501dc5ed647b910  w34-reader-proof-w4.py
b67a3c2ac56a9d3ca3b3087aee02934fcce201768698d00a84326d5af30c047a  reader-proof.txt
2cb641d28c03febb240a1dc269d26cfdbe8f1afce52195754349754c48a08ef2  reader-proof-w4.txt
95005887bde866a1b6b2d6b0ec5cbb1ce0bdfe34cbddbcc7920dd92f4400ce2d  w4a-hook.patch
ec11d78256923799a8b72000412560ce2f8780ba3bee1ddf8810c83241d593ee  w4b-hook.patch
3bab2ed59884c5c71ad3cffbf92d96395708485d63facacf717a36024116f082  w4a-analyze.py
0e1ada59314958ce3e0b67dfb55371428659adbe79291cd3bbd0a317dfcd94ca  w4-pstar.py
ef87136d67820508b2e357279ba94989c5f51a886eebeab1cf7113d25d3d96d3  w4a.txt
86cd92d859f3d8f2bca499f338d2c2b67d108d8b676757c8a7f5ce3676d150e0  w4a-dump.call1
17b58f719aad479270801e488aa6f61796ae064e5ba001ef0fb09f9b10cfd0f3  w4a-dump.call2
b8fc03fbbe1dd64b75ceb84c68b0ac196637d01607a346a655347085506f64ca  windowB-build.sh
fe36a849ee07da0c3081f5532bec407e6e9faf11e251777721f7964c6aa8ba7c  windowB.sh
35803168528db5b83dd1f6837360081a32266f10619311abef5309b9e911ca2b  windowB.log
5fc9f51f9d462998ec15da87ebbf6cdc9f9ef8d69fcd71fd779229d10e38732a  HMX-8192-w4b-dump.control
546f77a9695ebd0ec15e68741777e4fb3e539c0050b2171766673d5dcf074069  HMX-8192-w4b.clocks.csv
88fe5b7988bfe9a1cda51ec33b20e402716d88f6aada0bfefb94de25563ff2f0  HPEPA3-8192_cuda_gpu_trace.csv
4898af000e9dbff1d0a44bb6d8249525f817b4428dd084b0df43217dbdf44b6c  HPEPA3-8192_cuda_api_trace.csv
4792a3c8fb7877687052225ad85afd6f51ad3b3f8c2a30f14cc2b5fd3b8b4c87  HMX-8192_cuda_gpu_trace.csv
64e601c48b9ab97b1711e70ed060bc4d7a895bdf2223e95ad95614c7144c0b81  HMX-8192_cuda_api_trace.csv
f76f6d7ef882c961c364db38b5cabc0b37bba3c55ef87a62960e092325ff9362  HMX-8192-w4b_cuda_gpu_trace.csv
6f2f4c21c06eae42cb04385cfc16ded6b6b99725989a52266057e0068d21f00a  HMX-8192-w4b_cuda_api_trace.csv
```

The reader's own proof, `reader-proof.txt`, is the reader run over the Stage 1 traces
(2026-09-28) against Stage 1's recorded figures, with five red controls (a renamed
column, a missing one, an unknown time unit, an unknown size unit, a wrong cycle
count), `ALL PASS`; the three recorded figures it could not reproduce are in the last
paragraph below. `reader-proof-w4.txt` is the W4 branch over synthetic inputs with
known answers: D/T* of 2.5 (band at least 2), 1.071 (band at most 1.25), 1.25 exactly
(in the lower band) and 1.818 (between), and four red controls (a failed control voids
T*, a missing control line, two reruns instead of three, a bulk launch of one block),
`ALL PASS`.

**The reader's three outputs, verbatim.**

HPEPA3, `HPEPA3-8192.reader.txt`:

```text
# HPEPA3-8192 (W3)
## kernels (all launches of the run)
name | launches | total ms | mean ms | median ms | min ms | max ms | regs/thread | block | grid
Kernel_DeriveStreams | 2 | 0.273 | 0.136 | 0.136 | 0.135 | 0.138 | [255] | [256] | [391]
Kernel_FoldField | 2 | 67.585 | 33.793 | 33.793 | 33.791 | 33.794 | [26] | [768] | [2]
Kernel_NormalizeQks1 | 3 | 0.125 | 0.042 | 0.056 | 0.007 | 0.062 | [31] | [768] | [1]
Kernel_RunAttempts | 2 | 4310.391 | 2155.195 | 2155.195 | 2075.131 | 2235.260 | [196] | [256] | [391]
STOP CHECK (review W3: stop if registers != 196 or block != 256): Kernel_RunAttempts regs [196] block [256] -> ok, R3 arithmetic below stands
R3 arithmetic (65536 registers/SM, per-warp allocation unit 256): 196 regs -> 6400/warp, 8 warps/block -> 1 resident block(s) per SM = 8 warps/SM at most (information only: SM count and register file are the review's figures, not read from the trace)
## cycle 0: 1 Kernel_RunAttempts launches, run total 2075.1 ms, span 2476.7 ms
launch(1-based) | threads (grid x block) | duration ms | regs
1 | 100096 (391 x 256) | 2075.131 | 196
10x point: none (no launch at or under a tenth of the full launch)
launches under 10 % of the full launch (strict <): 0.0 ms of 2075.1 ms = 0.0 %
launches under 10 % of the full launch (inclusive <=): 0.0 ms of 2075.1 ms = 0.0 %
host gaps (R2): GPU idle between consecutive kernels 367.6 ms in 3 gaps (max 316.7 ms), 14.84 % of the cycle's kernel span (R2 fires over 20 %: no)
host API time in this cycle's window outside cuCtxSynchronize and one-time setup: 5.91 ms (0.24 % of the span)
## cycle 1: 1 Kernel_RunAttempts launches, run total 2235.3 ms, span 2287.8 ms
launch(1-based) | threads (grid x block) | duration ms | regs
1 | 100096 (391 x 256) | 2235.260 | 196
10x point: none (no launch at or under a tenth of the full launch)
launches under 10 % of the full launch (strict <): 0.0 ms of 2235.3 ms = 0.0 %
launches under 10 % of the full launch (inclusive <=): 0.0 ms of 2235.3 ms = 0.0 %
host gaps (R2): GPU idle between consecutive kernels 18.5 ms in 3 gaps (max 15.1 ms), 0.81 % of the cycle's kernel span (R2 fires over 20 %: no)
host API time in this cycle's window outside cuCtxSynchronize and one-time setup: 7.02 ms (0.31 % of the span)
NOTE: cycle 0's first launch follows a long gap on a cold process: ILGPU compiles the kernel on its first use, so cycle 0's R2 figure is not the host loop's; cycle 1's is.
NOTE: 'threads' is grid x block, a launch rounded up to a whole block; it is an upper bound of the active count (the trace does not carry the exact count). The 10 % comparison therefore depends on strict vs inclusive; both are printed.
## host API (whole run), ms
cuCtxSynchronize 4393.63 | cuCtxCreate_v2 117.89 | cuCtxDestroy_v2 47.23 | cuModuleLoadDataEx 26.57 | cuMemFree_v2 9.41 | cuMemAlloc_v2 3.95 | cuMemcpyAsync 3.38 | cuModuleUnload 0.42 | cuLaunchKernel 0.32 | cuStreamSynchronize 0.30 | cuMemsetD8Async 0.16 | cuCtxSetCurrent 0.03
outside cuCtxSynchronize, excluding ctx create/destroy/set and module load/unload: 17.53 ms; module load/unload 27.00 ms; cuCtxSynchronize 4393.6 ms; sum of all kernel durations 4378.4 ms
## memory operations (whole run)
name | count | total ms | total MB
[CUDA memcpy Device-to-Host] | 10 | 2.554 | 25.748
[CUDA memcpy Host-to-Device] | 11 | 0.023 | 0.948
[CUDA memset] | 7 | 1.940 | 1579.274
```

HMX, `HMX-8192.reader.txt`:

```text
# HMX-8192 (W3)
## kernels (all launches of the run)
name | launches | total ms | mean ms | median ms | min ms | max ms | regs/thread | block | grid
Kernel_DeriveStreams | 2 | 0.057 | 0.029 | 0.029 | 0.028 | 0.029 | [255] | [256] | [40]
Kernel_FoldField | 2 | 6.539 | 3.270 | 3.270 | 3.269 | 3.270 | [26] | [768] | [3]
Kernel_NormalizeQks1 | 3 | 0.280 | 0.093 | 0.133 | 0.013 | 0.133 | [31] | [768] | [1]
Kernel_RunAttempts | 2 | 3076.538 | 1538.269 | 1538.269 | 1177.328 | 1899.210 | [196] | [256] | [40]
STOP CHECK (review W3: stop if registers != 196 or block != 256): Kernel_RunAttempts regs [196] block [256] -> ok, R3 arithmetic below stands
R3 arithmetic (65536 registers/SM, per-warp allocation unit 256): 196 regs -> 6400/warp, 8 warps/block -> 1 resident block(s) per SM = 8 warps/SM at most (information only: SM count and register file are the review's figures, not read from the trace)
## cycle 0: 1 Kernel_RunAttempts launches, run total 1177.3 ms, span 1570.5 ms
launch(1-based) | threads (grid x block) | duration ms | regs
1 | 10240 (40 x 256) | 1177.328 | 196
10x point: none (no launch at or under a tenth of the full launch)
launches under 10 % of the full launch (strict <): 0.0 ms of 1177.3 ms = 0.0 %
launches under 10 % of the full launch (inclusive <=): 0.0 ms of 1177.3 ms = 0.0 %
host gaps (R2): GPU idle between consecutive kernels 389.8 ms in 3 gaps (max 314.3 ms), 24.82 % of the cycle's kernel span (R2 fires over 20 %: YES)
host API time in this cycle's window outside cuCtxSynchronize and one-time setup: 1.50 ms (0.10 % of the span)
## cycle 1: 1 Kernel_RunAttempts launches, run total 1899.2 ms, span 1946.7 ms
launch(1-based) | threads (grid x block) | duration ms | regs
1 | 10240 (40 x 256) | 1899.210 | 196
10x point: none (no launch at or under a tenth of the full launch)
launches under 10 % of the full launch (strict <): 0.0 ms of 1899.2 ms = 0.0 %
launches under 10 % of the full launch (inclusive <=): 0.0 ms of 1899.2 ms = 0.0 %
host gaps (R2): GPU idle between consecutive kernels 44.0 ms in 3 gaps (max 42.8 ms), 2.26 % of the cycle's kernel span (R2 fires over 20 %: no)
host API time in this cycle's window outside cuCtxSynchronize and one-time setup: 2.30 ms (0.12 % of the span)
NOTE: cycle 0's first launch follows a long gap on a cold process: ILGPU compiles the kernel on its first use, so cycle 0's R2 figure is not the host loop's; cycle 1's is.
NOTE: 'threads' is grid x block, a launch rounded up to a whole block; it is an upper bound of the active count (the trace does not carry the exact count). The 10 % comparison therefore depends on strict vs inclusive; both are printed.
## host API (whole run), ms
cuCtxSynchronize 3088.36 | cuCtxCreate_v2 117.57 | cuCtxDestroy_v2 48.53 | cuModuleLoadDataEx 3.73 | cuMemFree_v2 2.53 | cuMemcpyAsync 1.07 | cuMemAlloc_v2 0.88 | cuModuleUnload 0.34 | cuLaunchKernel 0.34 | cuStreamSynchronize 0.32 | cuMemsetD8Async 0.15 | cuCtxSetCurrent 0.03
outside cuCtxSynchronize, excluding ctx create/destroy/set and module load/unload: 5.28 ms; module load/unload 4.07 ms; cuCtxSynchronize 3088.4 ms; sum of all kernel durations 3083.4 ms
## memory operations (whole run)
name | count | total ms | total MB
[CUDA memcpy Device-to-Host] | 10 | 0.070 | 3.028
[CUDA memcpy Host-to-Device] | 11 | 0.016 | 0.550
[CUDA memset] | 7 | 0.358 | 338.635
```

HMX with the hook, `HMX-8192-w4b.reader.txt` (the re-run with `--control-log`):

```text
# HMX-8192-w4b (W4b)
## kernels (all launches of the run)
name | launches | total ms | mean ms | median ms | min ms | max ms | regs/thread | block | grid
Kernel_DeriveStreams | 2 | 0.055 | 0.027 | 0.027 | 0.027 | 0.028 | [255] | [256] | [40]
Kernel_FoldField | 2 | 6.799 | 3.400 | 3.400 | 3.270 | 3.529 | [26] | [768] | [3]
Kernel_NormalizeQks1 | 3 | 0.281 | 0.094 | 0.133 | 0.014 | 0.134 | [31] | [768] | [1]
Kernel_RunAttempts | 5 | 4834.802 | 966.960 | 586.451 | 585.295 | 1900.417 | [196] | [256] | [1, 40]
STOP CHECK (review W3: stop if registers != 196 or block != 256): Kernel_RunAttempts regs [196] block [256] -> ok, R3 arithmetic below stands
R3 arithmetic (65536 registers/SM, per-warp allocation unit 256): 196 regs -> 6400/warp, 8 warps/block -> 1 resident block(s) per SM = 8 warps/SM at most (information only: SM count and register file are the review's figures, not read from the trace)
## cycle 0: 1 Kernel_RunAttempts launches, run total 1177.0 ms, span 1572.8 ms
launch(1-based) | threads (grid x block) | duration ms | regs
1 | 10240 (40 x 256) | 1176.996 | 196
10x point: none (no launch at or under a tenth of the full launch)
launches under 10 % of the full launch (strict <): 0.0 ms of 1177.0 ms = 0.0 %
launches under 10 % of the full launch (inclusive <=): 0.0 ms of 1177.0 ms = 0.0 %
host gaps (R2): GPU idle between consecutive kernels 392.4 ms in 3 gaps (max 311.7 ms), 24.95 % of the cycle's kernel span (R2 fires over 20 %: YES)
host API time in this cycle's window outside cuCtxSynchronize and one-time setup: 1.68 ms (0.11 % of the span)
## cycle 1: 4 Kernel_RunAttempts launches, run total 3657.8 ms, span 3715.1 ms
launch(1-based) | threads (grid x block) | duration ms | regs
1 | 10240 (40 x 256) | 1900.417 | 196
2 | 256 (1 x 256) | 585.643 | 196
3 | 256 (1 x 256) | 585.295 | 196
4 | 256 (1 x 256) | 586.451 | 196
10x point: launch 2 (256 threads against 10240): duration 1900.4 -> 585.6 ms = 3.25x
launches under 10 % of the full launch (strict <): 1757.4 ms of 3657.8 ms = 48.0 %
launches under 10 % of the full launch (inclusive <=): 1757.4 ms of 3657.8 ms = 48.0 %
host gaps (R2): GPU idle between consecutive kernels 53.6 ms in 6 gaps (max 41.9 ms), 1.44 % of the cycle's kernel span (R2 fires over 20 %: no)
host API time in this cycle's window outside cuCtxSynchronize and one-time setup: 6.43 ms (0.17 % of the span)
NOTE: cycle 0's first launch follows a long gap on a cold process: ILGPU compiles the kernel on its first use, so cycle 0's R2 figure is not the host loop's; cycle 1's is.
NOTE: 'threads' is grid x block, a launch rounded up to a whole block; it is an upper bound of the active count (the trace does not carry the exact count). The 10 % comparison therefore depends on strict vs inclusive; both are printed.
## host API (whole run), ms
cuCtxSynchronize 4846.76 | cuCtxCreate_v2 120.23 | cuCtxDestroy_v2 47.00 | cuModuleLoadDataEx 3.58 | cuMemFree_v2 3.46 | cuStreamSynchronize 1.99 | cuMemcpyAsync 1.80 | cuMemAlloc_v2 1.57 | cuLaunchKernel 0.43 | cuModuleUnload 0.36 | cuMemsetD8Async 0.23 | cuCtxSetCurrent 0.05
outside cuCtxSynchronize, excluding ctx create/destroy/set and module load/unload: 9.48 ms; module load/unload 3.93 ms; cuCtxSynchronize 4846.8 ms; sum of all kernel durations 4841.9 ms
## memory operations (whole run)
name | count | total ms | total MB
[CUDA memcpy Device-to-Host] | 14 | 0.169 | 8.148
[CUDA memcpy Host-to-Device] | 17 | 0.092 | 4.390
[CUDA memset] | 10 | 0.900 | 842.395
## W4
W4B-CONTROL accelerator=Cuda hostThreads=False pstar=2654 bulkAttemptsAtPstar=3926 reruns=[3926,3926,3926] expectedAstar=3926 rerunsEqualBulk=True bulkEqualsExpected=True ownArgmax=2654 ownArgmaxAttempts=3926 particlesWhoseCountMovedInLastRerun=1
control (rerunsEqualBulk and bulkEqualsExpected): PASS
bulk launch: 10240 threads, 196 regs, block 256; rerun launches regs [196]
D (bulk RunAttempts of cycle 1, this run) = 1900.417 ms; D (clean W3 run) = 1899.210 ms
T* (p*-only reruns, n = 3) = 585.643, 585.295, 586.451 ms; median 585.643
D/T* = 3.245 (median T*); range 3.247 (min T*) .. 3.241 (max T*)
band of the pre-registered W4 rule: >= 2: contention; group size and registers are the levers (each candidate pre-registered in a design session)
```

**The three figures of Stage 1 that the reader's proof did not reproduce**, with the
`DIFFERS` lines of `reader-proof.txt`: HMX cycle 1's "9.95 s = 39.5 %" under-10 % share
is the inclusive comparison (at most 1024 threads), strict it is 8.67 s = 34.4 %, while
cycle 0's recorded 4.14 s = 29.8 % is the strict one (inclusive 4.78 s = 34.4 %);
the host-side API time "about 31 ms" on HPEPA3 reproduces only with module load and
unload counted (30.77 ms; 26.59 ms without) and "about 49 ms" on HMX only without it
(48.73 ms; 52.81 ms with), so the two figures were computed under different
conventions; "a 39.3 s run" on HMX is none of the trace's sums (kernels 39.07 s,
`cuCtxSynchronize` 39.08 s). The corrections stand where the figures stand: appended
under `#stage1-profile-details-2026-09-28`, and in `BOOT.md`, "### Stage 1: profile".

<a id="eb-verdict-2026-09-28"></a>

## 2026-10-02 — from "### E-B: budget sweep (2026-09-28)" — the verdict paragraph as it stood

Moved to keep `BOOT.md` inside its limit with the W3/W4 record; the decision and its
deciding figure stay there. The text as it stood:

> **Verdict: adopt.** The rule ("CUDA HMX improves at least 2× beyond both spreads and
> no HPEPA3 or host16 row regresses beyond its spread") holds at every tested budget
> from 128 up: HMX CUDA's low end already clears 2× the default row's high end at 128
> (883 > 794) and keeps rising through 8192; no HPEPA3 or host16 row's low end falls
> below the default row's own low end at any tested budget (every one overlaps it or
> sits above). Budget 8 fails outright (HMX CUDA falls to 0.47×), consistent with H1,
> not a refutation of it: fewer attempts per launch means more relaunches, which cost
> more, not less, confirming budget genuinely drives launch count rather than being
> inert. The 1.5×-refutation branch does not apply. Which value among
> {128, 512, 2048, 8192} becomes the new default is decided by
> `src/Simulation/BOOT.md`'s own selection rule (`## Budget selection rule
> (2026-09-28)`), not part of E-B's own rule: HMX CUDA only clears R0's own 0.8×host16 bar
> starting at budget 512 (1782 ≥ 0.8 × 1920 = 1536), so R0's "collapse" is not gone at
> the shipped default but is gone from 512 upward.

<a id="figures-w2-2026-10-02"></a>

## 2026-10-02 — from `## Figures`, `### Pre-registered before any new figure (2026-10-02)` and `## Acceptance criteria` (now `ACCEPTANCE.md`) — W2: the readings, the stop's explanation, the Stage 0 section and criterion 1 as they stood

The run was made by the orchestrator, then recorded here and in `BOOT.md`. Raw logs: the
W2 run (`w2.log`, 21:14-21:27) and the A/B/A run (`aba.log`, 21:29-21:31), both kept in
the orchestrating session's scratchpad; what follows is their content, the process lists
reduced to the finding.

**W2 run.** Commit `7e8f6f7`, clean tree (no `-dirty`), Release, .NET 10.0.12, date
(UTC) 2026-10-02, machine `AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16
logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows
10.0.26200`. Commands, as the tool printed them: `dotnet run -c Release --project
tests/Benchmarks -- --formulation HPEPA3 --paths
reference,host1,host16,cpu,cuda,original --repeats 3`, then the same with
`--formulation HMX`; particle-count override: none (each formulation's own N); attempts-per-launch override: none (`SimulationOptions`' own
default, 8192). The two `cpu` rows as printed (the other ten rows are in `BOOT.md`'s
table):

> | 2026-10-02 | HPEPA3 | cpu | not available (the ILGPU CPU accelerator's kernel-launch path is Execution-internal (InternalsVisibleTo Simulation, Cli and their tests only, src/Execution/API.md); Simulation's public SimulationOptions has no field that reaches it (BOOT.md, "## Escalation: the CPU accelerator oracle path")) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |
> | 2026-10-02 | HMX | cpu | not available (the ILGPU CPU accelerator's kernel-launch path is Execution-internal (InternalsVisibleTo Simulation, Cli and their tests only, src/Execution/API.md); Simulation's public SimulationOptions has no field that reaches it (BOOT.md, "## Escalation: the CPU accelerator oracle path")) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e8f6f7 |

**Quiet checks of the W2 run**, `nvidia-smi` GPU utilisation and memory, then the three
`Get-Counter` readings the log holds after the process list (unlabelled lines, CPU
`% Processor Time` as in Stage 0):

| Check | Utilisation | Memory | `Get-Counter` readings |
|---|---|---|---|
| before, 21:14:40 | 1 % | 1886 MiB | 3.5, 4.2, 11.6 |
| between, 21:18:51 | 0 % | 1841 MiB | 1.3, 1.5, 3.6 |
| after, 21:27:03 | 0 % | 1823 MiB | 1.4, 3.3, 2 |

Process list of the `--query-compute-apps` check: 27 processes at each of the three
checks, every one with memory `[N/A]`: desktop and shell hosts, the NVIDIA overlay,
WebView2, Firefox, Claude, a few store apps. The set of executable names is identical
before and after (compared by name); no `dotnet`, `propstruct` or `python` among them.

**Stop and its explanation.** The W2 bullet's stop fired on HMX: CUDA 5095 ± 2 (n = 3)
outside E-B's 5042 ± 29; host16 2071 ± 18 outside 2085 ± 5. HPEPA3 inside (CUDA 43503 ±
88 against 43530 ± 162), Launches 2 on every batched row, R0 holding (5095 against 0.8 ×
2071 = 1657). The A/B/A, HMX, paths host16 and cuda, `--repeats 3`,
`--attempts-per-launch 8192` on both, A = `7e8f6f7`, B = `2dc74ec` (E-B's commit, built
Release in a detached worktree), A again. As printed (accepted particles/s, cycle 1,
mean ± sample spread, n = 3; Budget 8192, Launches 2, Attempts 6393938 on every row):

| Run | Start | Commit | host16 HMX | cuda HMX |
|---|---|---|---|---|
| A1 | 21:29:10 | `7e8f6f7` | 2081 ± 18 | 5053 ± 27 |
| B | 21:29:54 | `2dc74ec` | 1916 ± 254 | 5030 ± 24 |
| A2 | 21:30:37 | `7e8f6f7` | 1932 ± 242 | 5059 ± 31 |

Quiet checks between the runs, as logged: GPU utilisation, memory, `clocks.sm`,
temperature, then the `Get-Counter` readings.

| Check | Utilisation | Memory | `clocks.sm` | Temperature | `Get-Counter` readings |
|---|---|---|---|---|---|
| A1, 21:29:05 | 1 % | 1848 MiB | 937 MHz | 42 | 6.1, 3 |
| B, 21:29:49 | 56 % | 1808 MiB | 2790 MHz | 44 | 5.6, 4.8 |
| A2, 21:30:32 | 100 % | 1860 MiB | 2790 MHz | 45 | 6.1, 5.4 |
| end, 21:31:14 | 100 % | 1813 MiB | 2790 MHz | 45 | 9.4, 4.4 |

The utilisation of 56 % and 100 % at B, A2 and the end is the tail of the CUDA run that
had just finished, sampled within seconds of it, not a foreign load. Over the four
invocations of the day CUDA HMX reads 5095 ± 2 (W2), 5053 ± 27, 5030 ± 24, 5059 ± 31 and
host16 HMX 2071 ± 18, 2081 ± 18, 1916 ± 254, 1932 ± 242 (accepted particles/s, cycle 1,
mean ± sample spread, n = 3 per invocation). E-B's commit reads in the range of the
current commit's figures minutes apart, so the shift against E-B's table does not come
from the commits since; the between-invocation variation exceeds the within-invocation
spread, and E-B's n = 3 bands were too narrow to serve as a stop.

**Criterion 1 as it stood** (`ACCEPTANCE.md`, re-dated 2026-10-02):

> - [x] 2026-09-28: HPEPA3 and HMX are measured for reference mode, the host-thread path
>       (1 and 16 threads) and CUDA, with the original's own figure on the same machine,
>       and every row carries its provenance (`## Figures` above, the Stage 0
>       re-measurement; the machine was confirmed quiet, `nvidia-smi` and `Get-Counter`
>       both quoted there). The CPU accelerator's own kernel-launch path (`cpu`) stays
>       out of scope for this criterion, unresolved (the escalation above): its row
>       reads `not available`, not a blank, for both formulations.

**`## Figures` as it stood** (the Stage 0 re-measurement and its pointers, up to the
cycle-cost pointer, which stays in `BOOT.md`):

> ## Figures
>
> ⚠ 2026-09-28: the 2026-09-20 measurement (commit `7e703a9`) and its repeatability check
> moved to `HISTORY.md#figures-2026-09-20-superseded`, superseded by the Stage 0
> re-measurement below: twelve commits touched `Attempt.cs`, `Engine.cs` or `Kernels.cs`
> since then, the `Original` precision kind among them, and this node's own taboo says
> "re-measure or delete the row".
>
> Stage 0 re-measurement (`## Post-acceptance plan`), measured 2026-09-28, commit
> `ce30f62`, budget 32 (the default then), n = 3, quiet machine: the two runs' command,
> machine, quiet checks and the whole table moved 2026-10-02 →
> `HISTORY.md#figures-stage0-2026-09-28`. Not the shipped configuration (the default is
> 8192 since 2026-10-01) and stale under the taboo below: to be re-measured. The figure
> that decided its verdict: CUDA HMX 397 ± 0 against host16 HMX 1822 ± 210.
>
> The `reference` rows' own "32" in that table is the option the run passed, not
> the budget the run actually used: reference mode's *effective* budget is always 1
> (`RunDiagnostics.AttemptsPerLaunch`, added 2026-09-28, `src/Simulation/BOOT.md`, "##
> Budget selection rule"), and those rows' own Launches already equal their Attempts —
> the same identity a budget of 1 implies. Not re-measured: no figure there moves, only
> what the Budget column of a `reference` row means from here on (`API.md`, ⚠ 2026-09-28).
>
> **R0 (`## Post-acceptance plan`): CUDA HMX (397 ± 0) against 0.8 × host16 HMX
> (0.8 × 1822 ± 210 = 1458, outside its own spread) — 397 is far below 1458, so the
> collapse is not gone. Stage 1 follows.**
>
> ⚠ 2026-10-02: was "the collapse is not gone", unqualified, now true at budget 32, the
> default when Stage 0 ran, and not at the shipped default 8192: CUDA HMX 5042 ± 29
> against 0.8 × host16 2085 ± 5 = 1668 (E-B, `2dc74ec`) → HISTORY.md#eb-table-2026-09-28
>
> **HPEPA3 CUDA** fell from 47405 ± 249 (2026-09-20) to 35315 ± 28 (Stage 0) with host16
> inside its own spread, and that is the benchmark's stream-layout default, not code
> (bisection below).
>
> ⚠ 2026-09-28: was "cause not measured" and then a regression of the code, now the stream
> layout default → HISTORY.md#benchmarks-figures-hpepa3-drop-2026-09-28

<a id="benchmarks-purpose-cpu-row-2026-09-20"></a>
<a id="benchmarks-dependencies-benchmarkdotnet-2026-09-20"></a>
<a id="benchmarks-figures-hpepa3-drop-2026-09-28"></a>
<a id="benchmarks-criterion-1-cpu-2026-09-20"></a>

## 2026-10-02 — from `## Purpose`, `## Dependencies`, `## Figures`, `## Acceptance criteria` — four old ⚠ corrections

Moved, oldest first, to bring `BOOT.md` inside its 400 lines after this task's additions
(`AGENTS.md`, §15). Each of the four texts below is a pointer's target; the anchors above
name them in the order printed. The text as it stood.

**Purpose** (`#benchmarks-purpose-cpu-row-2026-09-20`; the sentence about the unit that
shared its paragraph is current truth and stays in `BOOT.md`):

> ⚠ 2026-09-20: this list first also named the ILGPU CPU accelerator, "the kernel oracle,
> for the record only", and the coding session found it unreachable from here — the
> accelerator's kernel-launch path is chosen by a parameter internal to `Execution` that
> `Simulation` never sets, so a figure for it would need a new public option or a widened
> `InternalsVisibleTo`. Decided the same day, by the orchestrator, against reaching for
> either: the root `BOOT.md` keeps that accelerator as a test oracle of the kernel path
> and explicitly not a performance path, so its throughput is not a figure this node owes
> anyone. `--paths cpu` stays accepted and answers "not available", naming this note, so
> that a reader who expects the row learns why there is none instead of wondering.

**Dependencies** (`#benchmarks-dependencies-benchmarkdotnet-2026-09-20`):

> ⚠ 2026-09-20: this line first also named BenchmarkDotNet, an anticipated tool from the
> design session. The command line this node's own API.md settled on in the same
> session (`--formulation`/`--paths`/`--repeats`, one process relaunch per repeat for a
> forced processor count) does not fit BenchmarkDotNet's iteration/warmup model, and the
> coding session that implemented it found no use for the package. Dropped rather than
> carried as an unused reference (root `BOOT.md` taboo: no dead weight is stated as such,
> but an unused dependency line is exactly the kind of claim AGENTS.md §8 asks to keep
> honest).

**Figures** (`#benchmarks-figures-hpepa3-drop-2026-09-28`):

> **HPEPA3 CUDA dropped, by configuration, not by code.** HPEPA3 CUDA fell from 47405 ± 249
> (2026-09-20) to 35315 ± 28 now, far outside both spreads; HPEPA3 host16 fell from
> 49346 to 44211, inside its own spread, so this is not a general slowdown of the host
> path. HMX host16 fell from 1954 to 1822, inside its own spread; HMX CUDA stayed
> within its old spread (394 ± 0 then, 397 ± 0 now) — the collapse itself is unchanged.
> No figure here is a bound or a hypothesis; every one is a mean ± sample spread over
> the three repeats named above.
>
> ⚠ 2026-09-28: was "cause not measured", naming twelve commits touching `Attempt.cs`,
> `Engine.cs`, `Kernels.cs` as the unsearched range; now isolated and confirmed by a
> positive control, "### HPEPA3 CUDA regression bisection (2026-09-28)" below.
>
> ⚠ 2026-09-28, later: was read as a regression of the code; the bisection finds the
> benchmark's stream layout, which rides `SimulationOptions`' default and moved from
> `Original` to `Independent` in d735f3e. The 2026-09-20 row measured the other layout.

**Acceptance criteria, criterion 1** (`#benchmarks-criterion-1-cpu-2026-09-20`):

>   ⚠ 2026-09-20: was "the CPU accelerator" among the four measured paths, without
>   qualification; narrowed the same day this criterion was first written; once the
>   coding session found the `cpu` (ILGPU kernel-launch) path unreachable from this node
>   (AGENTS.md §11), reformulating rather than deleting the row it names (AGENTS.md §6:
>   "a criterion that cannot be met is not deleted but reformulated").

<a id="bisection-section-2026-09-28"></a>

## 2026-10-02 — from "### HPEPA3 CUDA regression bisection (2026-09-28)" — the section as it stood

Moved to give `BOOT.md` room for the next measurement window (`AGENTS.md`, §15: the
decision and the one figure that decided it stay). The commit list and the probe log
are older and stay in `#hpepa3-cuda-bisect-2026-09-28`. The text as it stood:

> ### HPEPA3 CUDA regression bisection (2026-09-28)
>
> Full commit list (`git log --oneline 7e703a9..ce30f62 -- src/Particle src/Execution
> src/Simulation src/Random`, 50 commits) and the complete bisection log (every probe
> commit, its throwaway worktree, quietness check and CUDA HPEPA3 figure) are in
> `HISTORY.md#hpepa3-cuda-bisect-2026-09-28`. Bisecting the first-parent mainline
> filtered to those four paths (8 merge points) found a step, not a gradual drift,
> between the last-fast probe `51e66c9` (2026-09-21, 45322 ± 276, n=3) and the
> first-slow probe `9c8a161` (2026-09-21, 35264 ± 122, n=3); the only path-filtered
> code change between them is `d735f3e` ("feat(execution,simulation): refuse the
> Original stream layout for batched execution"), whose own diff is two cheap
> early-return checks in `Engine.RunBatch`/`RunContinuedBatch` plus a new unused
> parameter — not plausibly a 22 % kernel cost on its own.
>
> **Isolated and confirmed by a positive control, not guessed.** `d735f3e` also flips
> `SimulationOptions.Streams`' own default from `Original` to `Independent` (root
> BOOT.md, "Two stream layouts", decided 2026-09-21) — the very default this node's
> `Program.cs` relies on, since `API.md` names no `--layout` flag. Probe: rebuilt
> `51e66c9` (pre-refusal, default still `Original`) with one throwaway line forcing
> `Streams = StreamLayout.Independent` in `MeasureInProcess`, never committed, its
> worktree deleted after. Result: 35263 ± 118 (n=3) — the same slow band, on the *same*
> commit, changing only the stream layout option. So the step is not a code
> inefficiency introduced by any commit in the four watched paths: it is this node's
> own benchmark silently inheriting a deliberate default-value change, and the
> `Independent` layout's per-particle stream derivation costs measurably more on this
> branch-divergent, tail-heavy kernel (Stage 1 above: "bound by divergence and tails")
> than `Original`'s did. Neither isolated diff (`c507b08`, the other single-commit step
> this bisection passed through; `d735f3e`) is, by itself, a plausible mechanism for a
> CUDA-specific slowdown — reading them is what motivated the default-value probe
> instead of stopping at the diff.
>
> Not explored further (out of this task's scope): whether `Independent`'s own
> derivation could be made cheaper, or whether the divergence this exposes is R1's own
> tail or a distinct one.

<a id="eb-table-2026-09-28"></a>

## 2026-10-02 — from "### E-B: budget sweep (2026-09-28)" — the summary table

Moved for the same reason. The full table with launches, attempts and the quiet-check
log is the older `#eb-budget-sweep-2026-09-28`; this is the summary `BOOT.md` carried,
unedited. The text as it stood:

> | Budget | HPEPA3 host16 | HPEPA3 cuda | HMX host16 | HMX cuda | HMX cuda gain |
> |---|---|---|---|---|---|
> | 8 | 44176 ± 5590 | 19548 ± 42 | 1748 ± 4 | 186 ± 0 | 0.47× |
> | 32 (current default) | 47122 ± 1642 | 35156 ± 82 | 1825 ± 220 | 397 ± 0 | 1× |
> | 128 | 44624 ± 7249 | 43515 ± 165 | 2030 ± 13 | 884 ± 1 | 2.23× |
> | 512 | 47116 ± 1142 | 43508 ± 158 | 1920 ± 239 | 1782 ± 4 | 4.49× |
> | 2048 | 47608 ± 2046 | 43496 ± 193 | 1913 ± 224 | 3909 ± 22 | 9.85× |
> | 8192 | 45938 ± 6413 | 43530 ± 162 | 2085 ± 5 | 5042 ± 29 | 12.70× |
>
> Every figure: mean ± sample spread, n = 3, accepted particles/s, cycle 1 only, gain
> against the budget-32 row.

<a id="figures-stage0-2026-09-28"></a>

## 2026-10-02 — from "## Figures" — the Stage 0 re-measurement and its table

Moved for the same reason. Budget 32 was the default then; it is 8192 now (E-B,
adopted 2026-10-01), so these rows are not the shipped configuration and, under the
node's own taboo, are to be re-measured, not cited as current. The text as it stood:

> Stage 0 re-measurement (`## Post-acceptance plan` above), measured 2026-09-28. Machine
> confirmed quiet before, during and after both invocations: `nvidia-smi` read 0-1 % GPU
> utilization and a steady 1710 MiB (desktop compositing only — Explorer, shell, the
> NVIDIA overlay — no compute process holding the device, confirmed by
> `--query-compute-apps`) before, between and after; `Get-Counter
> '\Processor(_Total)\% Processor Time'` read 3-15 % outside this node's own runs, at
> every one of the four checks (before HPEPA3, during HMX, after HMX). Two full runs,
> same machine, commit `ce30f62`, `.NET 10.0.12`,
> `AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce
> RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200`; commands
> `dotnet run -c Release --project tests/Benchmarks -- --formulation HPEPA3 --paths
> reference,host1,host16,cpu,cuda,original --repeats 3` and the same with `--formulation
> HMX`; `--repeats` at its default (3), no `--particles`/`--attempts-per-launch`
> override, so every port row below is the formulation's own shipped `N` at the budget
> default (32) and the `original` row is the formulation's own shipped `KXX`/`N` (both
> ship `KXX = 0`, normalized to `Cycles = 1`, `src/Input`'s own reproduced defect, root
> `BOOT.md` "Fidelity to the original"): HPEPA3 `ParticlesPerCycle = 100000`, `original`
> row `200000` accepted particles (cycle 0 + cycle 1); HMX `ParticlesPerCycle = 10000`,
> `original` row `20000`. `Budget`/`Launches`/`Attempts` are this session's own new
> columns (Decision B, Edits item 2); the latter two cover the whole call (cycle 0 +
> cycle 1), not the isolated cycle-1 window the particles/s figure uses, and are `n/a`
> for `original` (not a `Simulation` run) and for `cpu` (`not available`).
>
> | Date | Formulation | Path | Accepted particles/s | Budget | Launches | Attempts | Machine | Build | Commit |
> |---|---|---|---|---|---|---|---|---|---|
> | 2026-09-28 | HPEPA3 | reference | 9291 ± 19 (n=3) | 32 | 1399729 | 1399729 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HPEPA3 | host1 | 28491 ± 2466 (n=3) | 32 | 7 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HPEPA3 | host16 | 44211 ± 6047 (n=3) | 32 | 7 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HPEPA3 | cpu | not available (the ILGPU CPU accelerator's kernel-launch path is Execution-internal, see "## Escalation" above) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HPEPA3 | cuda | 35315 ± 28 (n=3) | 32 | 7 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HPEPA3 | original | 7399 ± 312 (n=3) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HMX | reference | 374 ± 0 (n=3) | 32 | 6425301 | 6425301 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HMX | host1 | 1433 ± 94 (n=3) | 32 | 187 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HMX | host16 | 1822 ± 210 (n=3) | 32 | 187 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HMX | cpu | not available (the ILGPU CPU accelerator's kernel-launch path is Execution-internal, see "## Escalation" above) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HMX | cuda | 397 ± 0 (n=3) | 32 | 187 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |
> | 2026-09-28 | HMX | original | 318 ± 12 (n=3) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | ce30f62 |

<a id="hpepa3-cuda-bisect-2026-09-28"></a>

## 2026-09-28 — bisection log — the HPEPA3 CUDA regression (Decision B, task E-B/bisect)

Full path-filtered commit list, oldest last as `git log` prints it (newest first):
`git log --oneline 7e703a9..ce30f62 -- src/Particle src/Execution src/Simulation
src/Random`, 50 commits:

```
7ae2d23 efa3ef2 8431bef 9e88a5b 497d79b b337861 a3dab40 013cbf2 f0906fd 28f6235
00dc4f6 15d231c bbee3fd a82a879 1acb360 d33516f 4d410f4 131133a ed337e9 f4d8163
3a6407d b8ef22f 0200276 119f4ed d2c3022 4c62ef1 7762524 995bf00 1f77f3d 8bc87af
3a3a03f 23c77a5 9470ed8 ed61f00 a36e17a 3c1cabe d6da4e6 5b0495a 05b3ef1 8d5cd94
d735f3e c507b08 6f1683c 4972337 09e70e0 2e10a7a ad775ef 4baf47c f75928e f5a1515
```

First-parent mainline, filtered to the same four paths, 8 merges (oldest first):
`8bf5680` (2026-09-20), `a39fdf6`, `b648690`, `35e412f`, `4962e5c` (all 2026-09-21),
`c23d44b`, `1290a43` (both 2026-09-27), `0c5a2d6` (2026-09-28). `7e703a9` and
`8bf5680` share ancestor `8a502a3`; `7e703a9` merges into this mainline at `a39fdf6`
(`git merge-base --is-ancestor 7e703a9 a39fdf6` exits 0, the same check against
`8bf5680` exits 1).

Every probe: a throwaway `git worktree add <path> <commit> --detach` under this
session's scratch directory, `dotnet build PropStruct.sln -c Release` (0 warnings
every time), a quietness check (`nvidia-smi --query-gpu=utilization.gpu,memory.used`
and `--query-compute-apps`, PowerShell `(Get-Counter '\Processor(_Total)\% Processor
Time').CounterSamples.CookedValue`) immediately before the run, then `dotnet run -c
Release --project tests/Benchmarks -- --formulation HPEPA3 --paths cuda --repeats 3`.
Every quietness check read 0-1 % GPU utilization, no foreign compute process, and
3.5-17.7 % CPU outside this session's own build/run — inside the band Stage 0 itself
used. All worktrees removed after (`rm -rf`, then `git worktree prune`) since `git
worktree remove` failed with "Filename too long" on this machine's deep scratch path.

| Commit | Date | Level | HPEPA3 cuda particles/s (n=3) | Note |
|---|---|---|---|---|
| `4962e5c` | 2026-09-21 | wave7-mainline idx0 | 45385 ± 200 | fast |
| `c23d44b` | 2026-09-27 | wave7-mainline idx∞ (the merge itself) | 35165 ± 93 | slow |
| `4c62ef1` | 2026-09-24 | wave7-branch idx16/31 | 35227 ± 131 | slow |
| `00347bb` | 2026-09-23 | wave7-branch idx8/31 | 35286 ± 126 | slow |
| `05b3ef1` | 2026-09-23 | wave7-branch idx4/31 | 35316 ± 102 | slow |
| `9c8a161` | 2026-09-21 | wave7-branch idx2/31 | 35264 ± 122 | slow |
| `8e22a54` | 2026-09-21 | `51e66c9^1` (mainline side) | 45375 ± 225 | fast |
| `51e66c9` | 2026-09-21 | wave7-branch idx1/31 | 45322 ± 276 | fast |

Recursive narrowing: `4962e5c..c23d44b` (path-filtered) contains only `c23d44b`
itself on the mainline side — the entire diff is the second-parent branch
`claude/wave7`, tip `bc14ee6`, 40 path-filtered commits, first-parent-reduced to the
31-entry list BOOT.md's own bisection section counts (`51e66c9` idx1 through
`ff8e0a8` idx31). Within that: idx16 (`4c62ef1`) slow, idx8 (`00347bb`) slow, idx4
(`05b3ef1`) slow, idx2 (`9c8a161`) slow, idx1 (`51e66c9`) fast — isolating the step to
`(51e66c9, 9c8a161]`. `git log --oneline 51e66c9..9c8a161 -- src/Particle
src/Execution src/Simulation src/Random` names exactly one commit, `d735f3e`;
`9c8a161`'s first parent `3c4a7d6` (root `BOOT.md` docs only, confirmed by `git show
--stat`) carries no change in the four paths relative to `51e66c9` at all. `8e22a54`
(`51e66c9^1`, confirmed identical to `4962e5c` in the four paths by `git log
4962e5c..8e22a54 -- <paths>` returning empty) measured fast, closing the loop: the
step is real and lands exactly on `d735f3e`/`51e66c9`, not on measurement noise
(non-monotonic with respect to run order: fast, slow ×5, fast, fast).

`d735f3e`'s only src diff in the four paths: `src/Execution/Engine.cs`, two
early-return `if (layout == StreamLayout.Original) { counters = default; return
BatchStatus.OriginalLayoutRequiresReferenceMode; }` blocks in `RunBatch` and
`RunContinuedBatch`, plus a new unused-by-caller `StreamLayout` parameter on the
latter — both before any attempt runs, so no plausible 22 % kernel cost. The same
commit's `src/Simulation/SimulationOptions.cs` diff (4 lines) flips `Streams { get;
init; }`'s default from `StreamLayout.Original` to `StreamLayout.Independent` (root
`BOOT.md`, "Two stream layouts", decided 2026-09-21); `tests/Benchmarks/Program.cs`
builds `SimulationOptions` with no `Streams` set (confirmed by reading the file at
`51e66c9`), and `API.md` names no `--layout` flag, so this node's own benchmark rides
whichever default `Simulation` ships.

**Positive control.** In the `51e66c9` worktree (pre-refusal, default still
`Original`), added one line to `MeasureInProcess`'s `SimulationOptions` initializer,
`Streams = StreamLayout.Independent,` — a throwaway edit, never committed, the
worktree deleted afterward — rebuilt (0 warnings), quietness re-checked (1 % GPU,
6.9 % CPU), ran the same command. Result: 35263 ± 118 (n=3), inside the slow band
(35165-35316 across the five slow probes above) and outside the fast band's spread
(45322-45385 ± ≤276). Same commit, same binary but for one option value, throughput
moves the full amount. This is the isolation: the regression is the benchmark's own
un-pinned default inheriting `Simulation`'s deliberate default-value change, not an
inefficiency in any commit's code.

Not measured: why `Independent`'s per-particle stream derivation costs more on the
CUDA batched kernel specifically (host16 shows no comparable step across the same
commits — Stage 0's own table, `HPEPA3 host16` 49346→44211, inside its own spread);
Stage 1's own finding ("bound by divergence and tails") is the standing explanation
this task did not re-open.

⚠ 2026-10-02 (appended; the entry above is the original text): "bound by divergence and
tails" was never a Stage 1 finding, it is a hypothesis (`BOOT.md`, "## Post-acceptance
plan"), and the explanation given for the layout step is refuted: `Kernel_DeriveStreams`
costs 0.13-0.14 ms per cycle on HPEPA3 against a step of 0.63 s per cycle →
`#bisection-section-2026-09-28` and `BOOT.md`, "### HPEPA3 CUDA regression bisection".

<a id="eb-budget-sweep-2026-09-28"></a>

## 2026-09-28 — full table — the E-B budget sweep (Decision B, task E-B)

Twelve invocations, `dotnet run -c Release --project tests/Benchmarks --
--formulation <F> --paths host16,cuda --repeats 3 --attempts-per-launch <B>`, commit
`2dc74ec` (branch `claude/perf-eb`), same machine as Stage 0
(`AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce
RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200`, `.NET 10.0.12`).
Quietness: `nvidia-smi` read 1 % GPU utilization and no foreign `--query-compute-apps`
entry before the sweep; `Get-Counter '\Processor(_Total)\% Processor Time'` read
2.5-16.8 % at every one of the fourteen checks (before the sweep, after each of the
twelve invocations, after the sweep) — inside Stage 0's own 3-15 % band, the one
16.8 % reading a transient (the next check read 4.4 %). The `nvidia-smi` reading
taken immediately after each invocation shows 19-100 % utilization: this is the tail
of that invocation's own last kernel launch finishing, not foreign load — no entry
ever appeared in `--query-compute-apps` beyond this machine's own desktop processes
(Explorer, shell, the NVIDIA overlay), matching the "0-1 % / 1710-1783 MiB, desktop
only" reading `## Figures`' own Stage 0 entry already recorded.

| Formulation | Budget | Path | Accepted particles/s (n=3) | Launches | Attempts |
|---|---|---|---|---|---|
| HPEPA3 | 8 | host16 | 44176 ± 5590 | 25 | 1396212 |
| HPEPA3 | 8 | cuda | 19548 ± 42 | 25 | 1396212 |
| HPEPA3 | 32 | host16 | 47122 ± 1642 | 7 | 1396212 |
| HPEPA3 | 32 | cuda | 35156 ± 82 | 7 | 1396212 |
| HPEPA3 | 128 | host16 | 44624 ± 7249 | 2 | 1396212 |
| HPEPA3 | 128 | cuda | 43515 ± 165 | 2 | 1396212 |
| HPEPA3 | 512 | host16 | 47116 ± 1142 | 2 | 1396212 |
| HPEPA3 | 512 | cuda | 43508 ± 158 | 2 | 1396212 |
| HPEPA3 | 2048 | host16 | 47608 ± 2046 | 2 | 1396212 |
| HPEPA3 | 2048 | cuda | 43496 ± 193 | 2 | 1396212 |
| HPEPA3 | 8192 | host16 | 45938 ± 6413 | 2 | 1396212 |
| HPEPA3 | 8192 | cuda | 43530 ± 162 | 2 | 1396212 |
| HMX | 8 | host16 | 1748 ± 4 | 744 | 6393938 |
| HMX | 8 | cuda | 186 ± 0 | 744 | 6393938 |
| HMX | 32 | host16 | 1825 ± 220 | 187 | 6393938 |
| HMX | 32 | cuda | 397 ± 0 | 187 | 6393938 |
| HMX | 128 | host16 | 2030 ± 13 | 47 | 6393938 |
| HMX | 128 | cuda | 884 ± 1 | 47 | 6393938 |
| HMX | 512 | host16 | 1920 ± 239 | 12 | 6393938 |
| HMX | 512 | cuda | 1782 ± 4 | 12 | 6393938 |
| HMX | 2048 | host16 | 1913 ± 224 | 3 | 6393938 |
| HMX | 2048 | cuda | 3909 ± 22 | 3 | 6393938 |
| HMX | 8192 | host16 | 2085 ± 5 | 2 | 6393938 |
| HMX | 8192 | cuda | 5042 ± 29 | 2 | 6393938 |

Launches/attempts cover the whole call (cycle 0 + cycle 1), identical across the two
paths of the same (formulation, budget) row since both run the same formulation and
budget through the same particle program; `Attempts` is budget-invariant (it is the
formulation's own total attempt count), `Launches` falls as the budget rises exactly
as `## Budget measurement`'s own per-particle attempt distribution predicts.

Regression-adoption arithmetic (verbatim rule: "CUDA HMX improves at least 2× beyond
both spreads and no HPEPA3 or host16 row regresses beyond its spread", against the
budget-32 row as the current default): gain = new mean / 397; low-end check = (new
mean − new spread) vs 2×(397 + 0) = 794. Budget 8: gain 0.47×, low end 186 < 794,
fails (and is itself a regression, not merely a non-improvement). Budget 128: gain
2.23×, low end 883 > 794, passes; HPEPA3 host16 44624±7249 (range 37375-51873)
overlaps the default row's 45480-48764; HPEPA3 cuda 43515±165 far above the default's
35074-35238 (improvement); HMX host16 2030±13 (range 2017-2043) above the default's
1605-2045 (improvement, no regression). Budget 512, 2048, 8192: same pattern, gains
4.49×/9.85×/12.70×, no row's low end falls under the default row's own low end.

<a id="stage1-profile-details-2026-09-28"></a>

## 2026-09-28 — from "### Stage 1: profile (2026-09-28)" — the per-launch kernel-trace figures

Moved to make room for the E-B and bisection sections this task adds, within the
node's own 400-line limit; the rule verdicts these figures support stay in `BOOT.md`.
The text as it stood:

> **HPEPA3** (`nsys`, `cuda_gpu_kern_sum` and the per-launch kernel trace):
> `Kernel_RunAttempts` is 7 launches over both cycles, 5.30 s total, mean 757.1 ms,
> median 460.3 ms, min 2.66 ms, max 2.226 s. Cycle 0's 3 launches run 100096, 768, 256
> threads over 2068.7, 460.3, 4.7 ms; cycle 1's 4 run 100096, 1024, 256, 256 threads
> over 2226.3, 530.9, 5.8, 2.7 ms. Launches under 10 % of the full 100096 threads total
> 465.0 ms of cycle 0's 2533.7 ms (18.4 %) and 539.4 ms of cycle 1's 2765.7 ms (19.5 %).
> Host-side API time outside `cuCtxSynchronize` (memcpy, alloc/free, `cuLaunchKernel`,
> excluding one-time module load/context setup) sums to about 31 ms against a 5.4 s run
> (under 1 %).
>
> **HMX**: 187 `Kernel_RunAttempts` launches, 39.05 s total, mean 208.8 ms, median
> 119.6 ms, min 1.78 ms, max 802.5 ms. Cycle 0 (64 launches, 13.88 s): active threads
> fall from 10240 to 1024 (10×, at launch 19) while duration falls only from 537.0 to
> 234.5 ms (2.29×); launches under 10 % active total 4.14 s of 13.88 s (29.8 %). Cycle 1
> (123 launches, 25.17 s): active falls 10× (10240 → 1024) at launch 30 while duration
> falls only 588.2 → 459.4 ms (1.28×); under-10 %-active launches total 9.95 s of
> 25.17 s (39.5 %). Host-side API time outside `cuCtxSynchronize` sums to about 49 ms
> against a 39.3 s run (under 0.2 %).

⚠ 2026-10-02 (appended; the entry above is the original text): three of its figures
do not stand as written, found by reading the same traces with the W3/W4 reader
(`#w34-2026-10-02`): (1) HMX cycle 1's "under 10 %-active launches total 9.95 s of
25.17 s (39.5 %)" is the inclusive comparison (at most 1024 threads); strict it is
8.67 s = 34.4 %, and cycle 0's 4.14 s = 29.8 % is the strict one (inclusive 4.78 s =
34.4 %); both stay under 50 %, so R1's second clause is unchanged; (2) host-side API
time outside `cuCtxSynchronize`, "about 31 ms" on HPEPA3, reproduces only with module
load and unload counted (30.77 ms, 26.59 ms without), and "about 49 ms" on HMX only
without (48.73 ms, 52.81 ms with): two conventions, both far under 1 % of the run; (3)
"a 39.3 s run" on HMX is none of the trace's own sums (kernels 39.07 s,
`cuCtxSynchronize` 39.08 s).

<a id="cycle-cost-measurement-2026-09-20"></a>

## 2026-09-28 — from "### Cycle cost measurement (2026-09-20)" — the full run tables

Moved for the same reason as the Stage 1 figures above; the finding sentence stays in
`BOOT.md`'s `## Figures`. The text as it stood:

> The "one cycle is enough" decision above needed a measured check, not a permanent
> addition to this node's own command line (`## Constraints`: "measured once,
> separately, and noted"). Done with a throwaway internal flag added to `Program.cs` for
> the session and removed again afterward (`git diff` before the commit that carries
> this section shows no change to `Program.cs`): it runs a formulation with its own
> shipped `Cycles` unmodified (no `Cycles = 1` override), on the batched CPU path at the
> process's default thread count, and records the timestamp of the last progress report
> seen for every cycle number, giving a per-cycle elapsed time and throughput.
>
> Run on `inpt` (`N = 1000`, `KXX = 20`): the cheapest reference formulation whose own
> `KXX` is genuinely greater than 1, so it is the one this check needs and HPEPA3/HMX are
> not (both ship `KXX = 0`, one real cycle only). Two independent runs of the check:
>
> | Run | Cycle 0 | Cycle 1 | Cycles 2-5 | Cycles 6-20 (plateau) |
> |---|---|---|---|---|
> | A | 6526/s | 9595/s | 9324-55291/s | 51057-58418/s (mean ≈ 53200/s) |
> | B | 6525/s | 9523/s | 1594-12933/s (cycle 4 an outlier) | 49838-66041/s (mean ≈ 60700/s) |
>
> Finding: cycle 1's own throughput is **not** representative of the plateau a longer run
> settles into — it measured 5-6× below the cycles-6-and-later mean in both runs, a
> tiered-JIT/warm-up effect still resolving as late as cycle 3-5 on this small a
> formulation, not gone by cycle 1. The excess cost behaves as a roughly constant
> *absolute* overhead per cycle (tens to a few hundred milliseconds, one outlier near
> 0.6 s), not a per-particle one, so its *relative* weight shrinks as a formulation's own
> `N` grows: HPEPA3's and HMX's cycle-1 windows are seconds to tens of seconds long
> (100000 and 10000 particles respectively, against `inpt`'s 1000), so the same
> millisecond-scale overhead is a small fraction of the window there, consistent with
> the repeatability check above finding no run that fell outside its own recorded spread.
>
> So the shortcut above is now a measured decision for the two formulations this node's
> own acceptance criterion names: it costs a bias, always conservative (cycle 1 reads at
> or below the eventual plateau, never above it), small enough on HPEPA3 and HMX that two
> full runs did not turn it into irreproducibility. It is not a general property of every
> formulation — `inpt`'s own small `N` is exactly why the same effect dominates its
> figure instead of hiding inside it, which is why `inpt` and not HPEPA3/HMX carries this
> measurement. This check used the CPU batched path only; CUDA's own warm-up (kernel
> compilation, libdevice link) is a different mechanism and is not covered by this
> figure — the CUDA rows above already carry it, unmeasured separately, inside their own
> `± spread`.

<a id="figures-2026-09-20-superseded"></a>

## 2026-09-28 — from "## Figures" — the 2026-09-20 measurement and its repeatability check

Moved when Decision B's Stage 0 (`## Post-acceptance plan`) re-measured the same
command on the same machine: twelve commits had touched `Attempt.cs`, `Engine.cs` or
`Kernels.cs` since commit `7e703a9` that produced this table, the `Original` precision
kind among them, so this node's own taboo ("No figure carried over from an earlier
commit after a change to `Particle`, `Random` or `Execution`: re-measure or delete the
row") applied. The text as it stood:

> Measured 2026-09-20. Machine confirmed quiet before, during and after both runs:
> `nvidia-smi` read 0 % GPU utilization and 0 MiB used both before and after (no other
> process holding the device); `Get-Counter '\Processor(_Total)\% Processor Time'`
> read 0-2 % outside this node's own runs both before and after. Two full runs, same
> machine, same commit `7e703a9`, `.NET 10.0.12`,
> `AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce
> RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200`; commands
> `dotnet run -c Release --project tests/Benchmarks -- --formulation HPEPA3 --paths
> reference,host1,host16,cpu,cuda,original --repeats 3` and the same with `--formulation
> HMX`; `--repeats` at its default (3), no `--particles` override, so every port row
> below is the formulation's own shipped `N` and the `original` row is the formulation's
> own shipped `KXX`/`N` (both ship `KXX = 0`, normalized to `Cycles = 1`, `src/Input`'s
> own reproduced defect, root `BOOT.md` "Fidelity to the original"): HPEPA3
> `ParticlesPerCycle = 100000`, `original` row `200000` accepted particles (cycle 0 +
> cycle 1); HMX `ParticlesPerCycle = 10000`, `original` row `20000`. The table below is
> the first of the two runs; the second, identical in machine and command, is the
> repeatability check below it, not a second set of rows (AGENTS.md §6, ticks want a
> place, not a duplicate table).
>
> | Date | Formulation | Path | Accepted particles/s | Machine | Build | Commit |
> |---|---|---|---|---|---|---|
> | 2026-09-20 | HPEPA3 | reference | 10079 ± 105 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HPEPA3 | host1 | 31294 ± 4721 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HPEPA3 | host16 | 49346 ± 7455 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HPEPA3 | cpu | not available (the ILGPU CPU accelerator's kernel-launch path is Execution-internal, see "## Escalation" above) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HPEPA3 | cuda | 47405 ± 249 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HPEPA3 | original | 7041 ± 543 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HMX | reference | 370 ± 0 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HMX | host1 | 1388 ± 113 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HMX | host16 | 1954 ± 4 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HMX | cpu | not available (the ILGPU CPU accelerator's kernel-launch path is Execution-internal, see "## Escalation" above) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HMX | cuda | 394 ± 0 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
> | 2026-09-20 | HMX | original | 321 ± 12 (n=3) | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 7e703a9 |
>
> ### Repeatability check (2026-09-20)
>
> The same two commands run again, same machine, same commit, immediately after the
> table above. Every figure reproduced within the spread recorded with it: HPEPA3
> reference 10154 ± 15 (was 10079 ± 105), host1 32967 ± 865 (was 31294 ± 4721), host16
> 48499 ± 7104 (was 49346 ± 7455), cuda 47557 ± 18 (was 47405 ± 249), original 7508 ± 151
> (was 7041 ± 543); HMX reference 371 ± 2 (was 370 ± 0), host1 1297 ± 34 (was 1388 ± 113),
> host16 1957 ± 12 (was 1954 ± 4), cuda 394 ± 0 (was 394 ± 0), original 315 ± 15 (was
> 321 ± 12); `cpu` read `not available` both times for both formulations. Every
> second-run mean fell inside the first run's own `mean ± spread` band (the widest gaps,
> HPEPA3 `original` and `host1`, are still under one spread each), so the node's own
> repeats already capture the run-to-run variation this machine produces.

Superseded by the Stage 0 re-measurement (`## Figures`, dated 2026-09-28, commit
`ce30f62`): a single run per formulation at `--repeats 3`, not a second full-command
repeatability check, since the node's own `--repeats` already records a spread per
row.
