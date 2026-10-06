# BOOT.md — Random

## Purpose

The original program's generator GSV=2 (Fortran subroutine `random2`, source lines
1642–1691, seeds on lines 52–63): a multiplicative congruential generator modulo 2¹²⁸
whose state is kept by the original in ten limbs (nine of 13 bits, one of 11 bits),
with the output `u / 2¹²⁸` computed as an ordered sum of ten products. The node also
owns jump-ahead and the derivation of a particle's six streams. It is a node of its
own because it is the one part of the port that can be proven exact against the
original, and because the jump constants must have a single owner.

## Invariants

- The state sequence equals `s_{n+1} = a · s_n mod 2¹²⁸` exactly, with `a` the
  multiplier packed from the limbs (1, 0, 7916, 6769, 8113, 7234, 4142, 5015, 3567,
  1526), little-endian in base 2¹³.
- The six seeds are the limbs written in the source, unchanged: streams 6, 5, 4, 2, 1
  equal a⁰, a¹, a², a⁴, a⁵; stream 3 carries the limb 3784 where a³ has 3748 (a
  transposition in the original, reproduced, AGENTS.md §12 below).
- The `double` output is `Σ limb_i · 2^(13i − 128)` evaluated left to right from i = 0
  in `double`, as the Fortran line 1690 does; the limbs are derived from the state.
- `Advance(state, k)` equals `k` successive steps for any 128-bit `k`, computed by
  multiplication with `a^k mod 2¹²⁸`.
- Kernel-compatible: 128-bit arithmetic on pairs of `ulong`, no `System.UInt128`, no
  allocation.

## Dependencies

None.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- Stream roles, as the original uses them: stream 1 → X (fraction of the base
  particle), 2 → X0 (position inside the fraction), 3 → X1 (distance), 4 → X2 and
  5 → X21 (neighbour size), 6 → X3 (pocket size for a bridge) and X4 (pocket-in-pocket
  and bridge decisions). Stream 6 carrying two roles is itself a defect of the
  original the port reproduces, declared for `## Defects of the original`
  (2026-09-20): X3 and X4 draw from the same stream, so a pocket-size draw and the
  pocket-in-pocket/bridge decision that follows it are never independent of one
  another the way two roles on separate streams would be.
- Per-particle streams: each initial state is advanced by `seed · 2⁸⁰ + ordinal · 2⁴⁰`
  steps; seed 0 with ordinal 0 is the layout's initial states. The multiplier is ≡ 1
  mod 2²⁸, so the low 28 state bits never change and the period is 2¹⁰⁰; 2²⁰ seeds ×
  2⁴⁰ particles × 2⁴⁰ draws fit inside it.
- **Orbits.** `⟨a⟩` is exactly the group of residues ≡ 1 mod 2²⁸ (order 2¹⁰⁰), so the
  orbit of a state `s` under every jump-ahead is the class `{x ≡ s mod 2²⁸}`: two
  streams can meet only if their states agree in the low 28 bits. The six original
  seeds (stream 3's transposed limb included) all lie on the orbit of 1.
- **`Independent` layout** (decided 2026-09-17). Role `r = 1..6` starts at
  `H_r · 2²⁸ + (2r + 1)`, where `H_r` is the first 100 bits of
  `SHA-256("PropStruct.Random.Independent/r")` (ASCII, `r` in decimal), tabulated as
  constants. Distinct low bits make the six orbits disjoint, and hash-derived high bits
  keep the pairwise ratio `s_r · s_q⁻¹ mod 2¹²⁸` far from small integers (a small ratio
  would make one stream a scaled copy of another). The low bits never reach the output.
- **Replica jumps** (for `tests/Fixtures`): replica `k ≥ 1` of either layout jumps all
  six initial states by `k · 2⁸⁰ + 2⁷⁹`, an exponent no run of the port with seed `k`
  addresses below 2³⁹ particles.
- **Seed and replica jumps are rigid shifts** (derived and measured 2026-09-23). The
  multiplier is ≡ 1 mod 2²⁸ exactly, so `a^(2^j) − 1` has 2-adic valuation `28 + j`:
  68 for the ordinal jump 2⁴⁰, 66 for the batched offset 2³⁸, but 108 for 2⁸⁰
  (`0x38fbb · 2¹⁰⁸`) and 107 for 2⁷⁹ (`0x38fbb · 2¹⁰⁷ + 2¹²⁷`). A jump whose valuation is
  at least 100 multiplies only the invariant low bits, so it **adds one constant to every
  state of the stream**: every output moves by the same `δ` mod 1. For a stream whose low
  20 state bits are `t`, seed `s` moves it by `frac(s · t · c / 2²⁰)`, `c = 0x38fbb`, and
  replica `k` by `frac((k + ½) · t · c / 2²⁰ + t / 2)`. So two seeds, or two replicas, of
  one stream are the **same sequence shifted**, not independent samples of it. Per unit
  seed: `Original` layout `t = 1` on all six streams, `δ = 0.222590`, so seeds recur
  near-periodically every nine; `Independent` layout `t = 3, 5, …, 13`, `δ = 0.667771,
  0.112952, 0.558133, 0.003314, 0.448495, 0.893676`, so stream 4 moves by a third of a
  per cent per seed and 32 seeds cover 10.6 % of the circle. The replica half-step `2⁷⁹`
  adds `t / 2`, i.e. one half for every odd `t`: replicas sit on the arc opposite the
  port's seeds of the same number. The ordinal and batched jumps are not shifts, since
  their valuation is below 100. What this changes downstream is `tests/Fixtures`'s
  (`BOOT.md`, "Seed-patched replicas are shifts").
- **Batched derivation** (decided 2026-09-19, root `BOOT.md`, "Execution model", ⚠ of
  the same day; regrouped the same day, ⚠ below). A batched particle's streams come
  from `StreamSeeds.ForBatchedParticle(layout, seed, ordinal)`. For the `Independent`
  layout it equals `ForParticle`. For the `Original` layout role `r` is jumped by
  `seed · 2⁸⁰ + ordinal · 2⁴⁰ + g(r) · 2³⁸`, with `g = 0` for streams 1 and 2, `1` for
  3, 4 and 5, `2` for 6. Streams of one group keep their common offset, so the three
  lags the original keeps for a whole run survive in every particle: X0 of attempt k+1
  = X of attempt k (streams 1, 2); X21 of neighbour k+1 = X2 of neighbour k (streams 4,
  5); and X1 of neighbour n tied to X2 of neighbour n+1 by the constant near-`a`
  multiplier `S3 · S4⁻¹` (streams 3, 4 — the two draw in lockstep, one distance draw
  per neighbour pair, ⚠ below; the model-level reason is root `BOOT.md`'s, not
  re-derived here, AGENTS.md §3). The remaining group (stream 6)
  starts 2³⁸ apart from the rest, far beyond any particle's draws, so the start-of-run
  lag between it and stream 4 (two draws behind) does not recur in every particle.
  `ForParticle` is unchanged and keeps `ForParticle(0, 0)` = the layout's initial
  states, which reference mode continues.

  ⚠ 2026-09-19: the groups were first `{1, 2}, {3}, {4, 5}, {6}` — stream 3 alone,
  because its seed `a³′` is not a small power of `a` (`## Invariants` above: a
  transposed limb) and so did not look like it belonged with streams 4/5's clean
  `a¹`/`a²`. The first run of the port's own `results.m` through the statistical
  criterion under that grouping (`tests/Simulation.Tests/BOOT.md`) found batched
  `Original` failing far *more* than the pre-regrouping common-jump derivation on every
  reference formulation checked (HPEPA3 563 against 110 cells; PSAN02n 565 against 13),
  and its own bulk statistics (attempts/particle, bridges/particle) drifting toward the
  `Independent` layout's, not staying near reference mode's — although the two
  originally-identified persistent lags were verified, by direct engine read-back, to
  hold bit for bit (`tests/Execution.Tests`, wave-8 diagnostic, not committed). The
  cause, identified by the coordinator owning root `BOOT.md`'s "Execution model"
  (this is not something `Random` reads `Particle`'s own code to confirm, AGENTS.md
  §3): stream 3 (X1) and stream 4 (X2) draw in lockstep too — every distance draw is
  followed by exactly one neighbour pair — so `S3 · S4⁻¹` is itself a lag that
  persists for the whole run, not merely `S1 · S2⁻¹` and `S4 · S5⁻¹`.
  `S3 = a³′ = a³ + 36 · 2¹⁰⁴` (the
  transposed limb 8, 3784 against 3748, as a literal integer difference), so
  `S3 · S4⁻¹ = a + 36 · 2¹⁰⁴ · a⁻²`: a fixed multiplier close enough to `a` that X1 of
  neighbour n predicts X2 of neighbour n+1 with correlation 0.999994 over 10⁶ draws
  from the original seeds (`tests/Random.Tests`, wave-8 measurement, not committed as a
  permanent test — the permanent test is the exact ratio, not the correlation). This
  matches two earlier findings about the executable (root `BOOT.md`, "Statistical
  reference criterion", ⚠ 2026-09-17): making streams 4 and 5 independent removed the
  whole bias shift (it also cuts stream 3 from stream 4, since `Independent` gives every
  role its own disjoint orbit), and replacing `a³′` by the exact `a³` changed nothing
  (`Δ = 36 · 2¹⁰⁴` is the same kind of tie to stream 4 regardless of its value). Grouping
  stream 3 with streams 4 and 5 instead landed far closer to reference mode: HPEPA3 44
  cells against replicas-lagged (down from 563; the pre-regrouping common-jump baseline
  was 110), PSAN02n 3 cells (down from 565; baseline 13); attempts/particle and
  bridges/particle close to both reference mode and the lagged-replica means on both
  formulations (`tests/Simulation.Tests`, wave-8 measurement, not committed).
  Coordinator's decision the same day, following the design session's own review of
  these measurements.

⚠ AGENTS.md §12 deviation, declared: the `Original` layout reproduces two defects of
the original instead of an independent stream design. The streams are one sequence
offset by a power of `a`, so X0 of attempt k+1 equals X of attempt k and X21 of
neighbour k+1 equals X2 of neighbour k; stream 3 differs from a³ by a transposed digit.
The second identity biases the model (root `BOOT.md`, "Known bias of the original's
seeds"). The `Original` layout stays the default because every archived result was
produced with these seeds.

⚠ 2026-09-26: was "the `Original` layout stays the default", now `Independent`, the
default since 2026-09-21 (root `BOOT.md`, "Two stream layouts";
`src/Simulation/SimulationOptions.cs`). Batched mode is the default mode and refuses
`Original`, so an `Original` default would refuse every bare run.

⚠ 2026-09-17: this deviation first said it would be lifted by a later design session
introducing independent role streams. The measurement of the bias moved that into
version 1 as the `Independent` layout; the deviation now concerns the default layout
only.

## Acceptance criteria

- [x] 10⁶ draws of each stream equal a `BigInteger` implementation bit for bit (state
      and `double` output): 2026-09-17, `tests/Random.Tests`,
      `DrawStreamTests.MillionDrawsMatchTheBigIntegerStepper` (all six streams).
- [x] The packed seeds equal the limbs of the source lines 52–63, and streams 6, 5, 4,
      2, 1 equal the powers a⁰…a⁵ named above: 2026-09-17, `tests/Random.Tests`,
      `SourceLimbsTests.SeedMatchesItsSourceLines`,
      `SourceLimbsTests.MultiplierMatchesSourceLines1649To1650AndStream5`,
      `SourceLimbsTests.Streams6To1EqualPowersOfTheMultiplierExceptStream3sTransposedLimb`.
- [x] `Advance` by random 128-bit `k` equals the `BigInteger` modular power: 2026-09-17,
      `tests/Random.Tests`, `ArithmeticTests.AdvanceMatchesTheBigIntegerModularPower`
      (200 generated operand pairs).
- [x] `A40` and `A80` equal `BigInteger.ModPow(a, 2⁴⁰, 2¹²⁸)` and `(a, 2⁸⁰, 2¹²⁸)`:
      2026-09-17, `tests/Random.Tests`, `ArithmeticTests.A40EqualsTheBigIntegerModularPower`,
      `ArithmeticTests.A80EqualsTheBigIntegerModularPower`.
- [x] `ForParticle(0, 0)` equals the original seeds; `ForParticle(s, o)` equals the
      `BigInteger` jump for generated `(s, o)` including `2²⁰ − 1` and `2⁴⁰ − 1`:
      2026-09-17, `tests/Random.Tests`,
      `ArithmeticTests.ForParticleOfSeedZeroOrdinalZeroEqualsTheOriginalSeeds`,
      `ArithmeticTests.ForParticleEqualsTheBigIntegerJump`.
- [x] `Next` and `ForParticle` give bit-identical results on the host and on the ILGPU
      CPU accelerator: 2026-09-17, `tests/Random.Tests`,
      `KernelEqualityTests.NextMatchesOnHostAndTheCpuAccelerator`,
      `KernelEqualityTests.ForParticleMatchesOnHostAndTheCpuAccelerator`.
- [x] The `Independent` constants equal the SHA-256 rule; the six low-28-bit classes
      are distinct; every pairwise ratio `s_r · s_q⁻¹ mod 2¹²⁸` has its highest set bit
      at position ≥ 90: 2026-09-17, `tests/Random.Tests`,
      `IndependentSeedsTests.SeedMatchesTheSha256Rule` (all six roles, the constants
      recomputed from `SHA-256` independently of `IndependentSeeds`'s literals),
      `IndependentSeedsTests.LowTwentyEightBitClassesAreDistinct`,
      `IndependentSeedsTests.PairwiseRatiosStayFarFromASmallInteger` (both against the
      tabulated production constants, not the SHA-256 oracle).
- [x] `IndependentSeeds.ForParticle` equals the `BigInteger` jump and matches on the host
      and the ILGPU CPU accelerator; its first draws are held in a bit snapshot:
      2026-09-17, `tests/Random.Tests`,
      `ArithmeticTests.IndependentForParticleOfSeedZeroOrdinalZeroEqualsIndependentSeeds`,
      `ArithmeticTests.IndependentForParticleEqualsTheBigIntegerJump` (five generated
      operand pairs), `KernelEqualityTests.IndependentForParticleMatchesOnHostAndTheCpuAccelerator`,
      `DrawStreamTests.FirstThousandDrawsOfIndependentStreamsMatchTheApprovedBitSnapshot`
      (all six streams, `tests/Random.Tests/Snapshots/independent-draws.approved.txt`).
- [x] The first draws of every stream are held in a bit snapshot: 2026-09-17,
      `tests/Random.Tests/Snapshots/draws.approved.txt`,
      `DrawStreamTests.FirstThousandDrawsMatchTheApprovedBitSnapshot`.
- [x] `OriginalSeeds.ForBatchedParticle` equals the `BigInteger` jump with the
      role-group offset `g(r)·2³⁸` for generated `(seed, ordinal)` including the
      extremes, and matches on the host and the ILGPU CPU accelerator;
      `StreamSeeds.ForBatchedParticle` dispatches to it for `Original` and to
      `IndependentSeeds.ForParticle` for `Independent` (BOOT.md, "Batched
      derivation"): 2026-09-19, `tests/Random.Tests`,
      `ArithmeticTests.A38EqualsTheBigIntegerModularPower`,
      `ArithmeticTests.ForBatchedParticleEqualsTheBigIntegerJumpWithTheRoleGroupOffset`
      (five generated operand pairs),
      `KernelEqualityTests.ForBatchedParticleMatchesOnHostAndTheCpuAccelerator`,
      `StreamSeedsTests.ForBatchedParticleOriginalLayoutMatchesOriginalSeedsForBatchedParticle`,
      `StreamSeedsTests.ForBatchedParticleIndependentLayoutMatchesIndependentSeedsForParticle`.
- [x] Inside one derived batched particle, the three persistent lags survive (X0 of
      attempt k+1 = X of attempt k; X21 of neighbour k+1 = X2 of neighbour k, read
      through the raw stream draws; `S3 · S4⁻¹` constant across the particle's own
      draws and equal to the original's own ratio), and stream 6 and stream 2 no
      longer replay stream 4 within the first thousand draws — while the
      pre-regrouping common-jump derivation (`OriginalSeeds.ForParticle`) still shows
      that replay, proving the check is not vacuous: 2026-09-19, `tests/Random.Tests`,
      `BatchedDerivationTests.PersistentLagsHoldInsideOneDerivedParticle`,
      `BatchedDerivationTests.PersistentRatioS3TimesS4InverseIsConstantAndEqualsTheOriginals`,
      `BatchedDerivationTests.RoleGroupOffsetRemovesTheSpuriousCrossGroupReplay`,
      `BatchedDerivationTests.WithoutTheRoleGroupOffsetTheCrossGroupReplayIsPresent`.

  ⚠ 2026-09-19: this criterion first named only two persistent lags. Stream 3 moving
  into streams 4/5's group ("Batched derivation" above) adds the third (`S3 · S4⁻¹`)
  and its own test, `PersistentRatioS3TimesS4InverseIsConstantAndEqualsTheOriginals`;
  "stream 6 and stream 2 no longer replay stream 4" is unchanged from the first version
  and stays true under the new grouping too, since streams 2 and 6 are both still in a
  different group from stream 4 — `RoleGroupOffsetRemovesTheSpuriousCrossGroupReplay`
  itself did not need to change.

⚠ 2026-09-24: cited test names renamed for CA1707, meaning unchanged, no criterion
re-verified and no date moved (`tests/test-renames-2026-09-24.txt`).

## Defects of the original

| Fortran | Kind | What the original does | Consequence | The port | Differing cells |
|---|---|---|---|---|---|
| 52–63 | statistics | The six seeds are the source's own limbs, unchanged: streams 6, 5, 4, 2, 1 start at a⁰, a¹, a², a⁴, a⁵ — one sequence offset by a power of `a` rather than six independent streams (`## Invariants`, `## Constraints`, "Batched derivation" ⚠). | measured: biases the pocket and bridge quantities by an amount and sign that depend on the formulation, per-formulation table in `root BOOT.md`, "Known bias of the original's seeds". | reproduced | none |
| 52–63 | statistics | Stream 3 carries the limb 3784 where a³ has 3748, a transposition in the original (`## Invariants`); consumed in lockstep with streams 4 and 5, `S3 = a³ + 36·2¹⁰⁴`. | measured: X1 of neighbour n predicts X2 of neighbour n+1 with Pearson correlation 0.999994 over 10⁶ draws from the original seeds (`tests/Random.Tests`, wave-8 measurement, not committed as a permanent test — the permanent test is the exact ratio, not the correlation). | reproduced | none |
| stream 6 (`## Constraints`, "Stream roles") | statistics | Two roles, X3 (pocket size for a bridge) and X4 (pocket-in-pocket and bridge decisions), draw from the same stream 6 rather than from streams of their own (`## Constraints`); the measured bias table (`root BOOT.md`, "Known bias of the original's seeds") covers the seed lags, not this sharing. | not measured | reproduced | none |

## Taboos

- No "correction" of the original seeds or the transposed limb in the `Original`
  layout.
- No other generator in this node.

## Design

- **State.** `Mcg128State { ulong Low; ulong High; }` holds `s = High·2⁶⁴ + Low`. A step
  is the 128-bit product `a · s mod 2¹²⁸` written with 32-bit halves on `ulong`
  (schoolbook multiplication; `Math.BigMul` is not relied upon in kernel code).
- **Output.** After the step, the ten limbs are `limb_i = (s >> 13i) & 8191` for
  `i = 0..8` and `limb_9 = s >> 117`; the output is
  `((…((limb_0·x_0 + limb_1·x_1) + limb_2·x_2) …) + limb_9·x_9)` with
  `x_i = 2^(13i − 128)` as `double` constants (exact powers of two), each `limb_i`
  converted to `double` exactly. This is line 1690 evaluated in `double`. The step
  precedes the output, as in lines 1661–1690.
- **Seeds.** The six seeds are `const ulong` pairs packed from the source limbs, each
  with a comment naming its Fortran line; streams are numbered 1..6 as the Fortran
  `u01…u06`.
- **Jump-ahead.** `Advance(s, k)` multiplies by `a^k mod 2¹²⁸` computed by
  square-and-multiply over the bits of `k` (at most 128 squarings). Two constants are
  tabulated as `const ulong` pairs and proven by test against `BigInteger`:
  `A40 = a^(2⁴⁰)`, `A80 = a^(2⁸⁰)`. The per-particle jump `A80^seed · A40^ordinal`
  (square-and-multiply over the 64-bit `seed` and `ordinal`) is one formula,
  `ParticleJump.Factor`, applied by both `OriginalSeeds.ForParticle` and
  `IndependentSeeds.ForParticle` to their own six streams, so every layout's lag or
  disjointness relation holds inside every particle the same way.
- **Independent constants.** The six `const ulong` pairs of `IndependentSeeds` are
  `H_r·2²⁸ + (2r+1)` (`## Constraints`, "Independent layout"), each with a comment
  naming the SHA-256 label it was derived from; the derivation itself is not
  reproduced in code, only its result, because the constants are fixed once the
  design is decided and the test recomputes them independently with
  `System.Security.Cryptography` to prove the tabulation right.
- **Dispatch.** `StreamSeeds.ForParticle(layout, seed, ordinal)` is the one call site
  `Particle` and `Execution` use; it holds no formula of its own, only the choice
  between `OriginalSeeds.ForParticle` and `IndependentSeeds.ForParticle`, written
  without a `throw` so the method stays kernel-compatible.
- **Batched jump-ahead.** A third constant is tabulated the same way as `A40`/`A80`:
  `A38 = a^(2³⁸)`, proven by test against `BigInteger`. `OriginalSeeds.ForBatchedParticle`
  computes `ParticleJump.Factor(seed, ordinal)` once, then reaches each of the three
  role-group offsets by `g` successive multiplications by `A38` (`g = 0..2`) rather than
  tabulating `A38^g` separately — the group count is fixed at three by the role table of
  `## Constraints`, "Batched derivation", so this is not a second jump-ahead formula,
  only `Mcg128.Multiply` chained a fixed number of times. `StreamSeeds.ForBatchedParticle`
  is `Kernels.DeriveStreams`'s one call site (`../Execution/BOOT.md`, "A batch"); it holds no
  formula of its own either, dispatching to `OriginalSeeds.ForBatchedParticle` for
  `Original` and to `IndependentSeeds.ForParticle` for `Independent` (unchanged: its six
  orbits are already disjoint, so grouping adds nothing there).
- **Placement.** `ForParticle`/`ForBatchedParticle` (of every seed source) and
  `StreamSeeds.ForParticle`/`StreamSeeds.ForBatchedParticle` are kernel-compatible;
  `Execution` decides whether they run on the host or in the kernel.
