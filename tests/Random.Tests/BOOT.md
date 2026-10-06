# BOOT.md — Random.Tests

## Purpose

The definition of what "Random is ready" means.

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | packed multiplier and seeds | limbs of Fortran lines 52–63 and 1649–1650, read from `tests/Fixtures/cases/random/source_limbs.json` (below) | ✅ |
| L0 | seeds 6, 5, 4, 2, 1 are a⁰, a¹, a², a⁴, a⁵; seed 3 differs from a³ in limb 8 only | `BigInteger` | ✅ |
| L0 | `Multiply`, `Advance`, `A40`, `A80`, `OriginalSeeds.ForParticle` | `BigInteger` modular arithmetic on generated operands | ✅ |
| L0 | `IndependentSeeds`' six constants | `SHA-256` recomputed independently of the tabulated literals (root BOOT.md, "Independent layout") | ✅ |
| L0 | the six low-28-bit classes are distinct; every pairwise ratio `s_r·s_q⁻¹ mod 2¹²⁸` has its highest set bit ≥ 90 | the tabulated `IndependentSeeds.Streams` constants, `BigInteger` | ✅ |
| L0 | `IndependentSeeds.ForParticle`, `StreamSeeds.ForParticle` | `BigInteger` modular arithmetic on generated operands | ✅ |
| L1 | 10⁶ draws per Original stream, state and `double` | a `BigInteger` stepper with the ten-term sum in `double` | ✅ |
| L1 | host = ILGPU CPU accelerator, `Original` and `Independent` layouts | the same code on both | ✅ |
| Snapshot | first 1000 draws per Original stream | approved bit snapshot `draws.approved.txt` | ✅ |
| Snapshot | first 1000 draws per Independent stream | approved bit snapshot `independent-draws.approved.txt` | ✅ |
| L0 | `A38`; `OriginalSeeds.ForBatchedParticle`'s role-group offset `g(r)·2³⁸` | `BigInteger` modular arithmetic on generated operands (root BOOT.md, "Execution model", ⚠ 2026-09-19; `src/Random/BOOT.md`, "Batched derivation") | ✅ |
| L0 | `StreamSeeds.ForBatchedParticle` dispatches by `StreamLayout`, no formula of its own | `OriginalSeeds.ForBatchedParticle`, `IndependentSeeds.ForParticle` | ✅ |
| L1 | host = ILGPU CPU accelerator, `OriginalSeeds.ForBatchedParticle` | the same code on both | ✅ |
| L1 | the three persistent lags survive inside one derived batched particle (`S2·a==S1`, `S5·a==S4`, `S3·S4⁻¹` constant and equal to the original's); stream 6 and stream 2 no longer replay stream 4, unlike the pre-regrouping common jump | read directly through the raw draws/states of a `ForBatchedParticle`-derived `StreamSet`, against `OriginalSeeds.ForParticle` as the pre-fix control | ✅ |
| L0 | a jump of 2-adic valuation ≥ 100 (2⁸⁰, 2⁷⁹) is a rigid shift across 1000 consecutive states of every Independent stream; the ordinal jump 2⁴⁰ (valuation 68) is not | `state·a^J − state`, `BigInteger` | ✅ |
| L1 | `epsx(4)` recomputed from `Mcg128`/`IndependentSeeds`/the replica jump | every HPEPA3 independent replica's own printed `epsx(4)` and `NFY` (`tests/Fixtures`) | ✅ |
| L1 | the 32-replica arc's own sd of `epsx(4)` is less than half the whole circle's, at the replicas' median `NFY` | a one-pass histogram over `2¹⁶` candidate shifts of the same base sequence | ✅ |
| L0 | `tests/Fixtures`' own Python seed arithmetic (`generate.py`, `run_original.py`), base states, replica states and reference-mode states | `OriginalSeeds`/`IndependentSeeds.Streams`/`.ForParticle`, `Mcg128.Advance`, invoked through the scripts' `print-states` entry points (`tests/Fixtures/API.md`) | ✅ |

The two snapshot families are 128-bit integer states and `double` sums of terms scaled
by powers of two; `src/Random` calls no `System.Math` function, so they are not tied to
the platform's C runtime. Their approved files carry the `# platform:`
header of `tests/Harness/API.md`, "## Repository paths and bit snapshots", only so that
every snapshot file of the tree reads the same (2026-10-02).

## Invariants

- The oracle is `System.Numerics.BigInteger`, not a second 128-bit implementation.
- Every comparison is bit-exact.
- The 10⁶-draw level is marked long and excluded from the fast set.
- The Python cross-check (last levels-table row) invokes `tests/Fixtures`' scripts as
  subprocesses and reads only their documented `print-states` JSON output (`API.md`);
  it does not read their source beyond what that dependency already licenses (this
  node already depends on `Fixtures` for the committed limbs), and it never runs
  `PropStructV3.exe`.

## Dependencies

- [Random](../../src/Random/API.md) — what is being checked.
- [Harness](../Harness/API.md) — CPU host, bit snapshots, repository paths,
  `PythonScript`.
- [Fixtures](../Fixtures/API.md) — the limbs of the source's seeds and multiplier
  (`cases/random/source_limbs.json`, written from `Legacy/PropStructv3.for.txt`).

Outside the tree: xunit.

⚠ 2026-09-17: L0's first row first read the seed and multiplier limbs straight from the
member `PropStructv3.for` of `legacy/PropStructV3.zip` (`System.IO.Compression`, the
file decoded as ISO-8859-1), a declared deviation (AGENTS.md §12) from reading
`tests/Fixtures/Legacy/PropStructv3.for.txt`: `tests/Fixtures` was being produced by
another coder in the same wave and its transcoded source did not exist yet when this
node was written. Lifted the same day, once `tests/Fixtures/Legacy` shipped:
`LegacySource.cs` now reads the transcoded `.for.txt` (same line numbers,
`tests/Fixtures/BOOT.md`), and `## Dependencies` names `Fixtures` again. Since
2026-10-04 the source lies outside the repository: `SourceLimbs.cs` reads the limbs
`Fixtures` derived from it. Every case below still passed unchanged (`.for.txt`'s digits
are identical to the archive member's, `tests/Fixtures/generate.py verify`).

## Constraints

- Part of the default test command.

## Acceptance criteria

- [x] Every row of the levels table is green, with a date and the names of the tests:
      2026-09-17, `PropStruct.Random.Tests`, all classes (`SourceLimbsTests`,
      `ArithmeticTests`, `DrawStreamTests`, `KernelEqualityTests`, `IndependentSeedsTests`,
      `StreamSeedsTests`), 450 fast cases and 6 long cases
      (`dotnet test tests/Random.Tests --filter Category!=Long` and
      `--filter Category=Long`).
- [x] Every check is proven non-degenerate by a recorded mutation (AGENTS.md §13):
      2026-09-17, ten mutations applied and reverted, each seen red then green again
      (`git diff --stat` empty after revert). The first four predate the Independent
      layout; the last six are its own:
      - Seed test: `src/Random/OriginalSeeds.cs`, `Seed1Low` last hex digit `1` → `2`.
        Red: `SourceLimbsTests.SeedMatchesItsSourceLines(streamName: "S1")`,
        `SourceLimbsTests.Streams6To1EqualPowersOfTheMultiplierExceptStream3sTransposedLimb`,
        `DrawStreamTests.FirstThousandDrawsMatchTheApprovedBitSnapshot(streamName: "S1")`.
      - `A40` constant test: `src/Random/Mcg128.cs`, `A40High` last hex digit `0` → `1`.
        Red: `ArithmeticTests.A40EqualsTheBigIntegerModularPower`,
        `ArithmeticTests.ForParticleEqualsTheBigIntegerJump` (every case with a nonzero
        ordinal).
      - Ten-term output test: `src/Random/Mcg128.cs`, `X9` (the dominant weight,
        2^-11) perturbed by 1 part in 2^40. Red: every
        `DrawStreamTests.FirstThousandDrawsMatchTheApprovedBitSnapshot` and every
        `DrawStreamTests.MillionDrawsMatchTheBigIntegerStepper` case (fails on draw 0).
        (`X0`, the smallest weight at 2^-128, was tried first and found *not* to move
        the sum: its term is ~35 orders of magnitude below the dominant `X9` term, so
        double-precision addition rounds it away once later terms are added — true of
        the original's own `REAL*8` summation too, not a port defect.)
      - Host-vs-CPU-accelerator test: `tests/Random.Tests/KernelEqualityTests.cs`,
        `NextKernel.Run`'s `lowOut[index] = state.Low;` → `state.Low + 1`, a mutation
        confined to the kernel path so only kernel-vs-host parity could catch it. Red:
        exactly `KernelEqualityTests.NextMatchesOnHostAndTheCpuAccelerator` (426 of 427
        fast tests still passed).
      - Independent seed test: `src/Random/IndependentSeeds.cs`, `Seed1Low` last hex
        digit `3` → `4`. Red: `IndependentSeedsTests.SeedMatchesTheSha256Rule(role: 1,
        streamName: "S1")`,
        `DrawStreamTests.FirstThousandDrawsOfIndependentStreamsMatchTheApprovedBitSnapshot(streamName:
        "S1")` (448 of 450 fast tests still passed).
      - Shared jump-factor test (the refactor that gave `OriginalSeeds.ForParticle` and
        `IndependentSeeds.ForParticle` one formula instead of two):
        `src/Random/ParticleJump.cs`, `Pow`'s bit test `& 1UL` → `& 2UL`. Red: both
        `ArithmeticTests.ForParticleEqualsTheBigIntegerJump` and
        `ArithmeticTests.IndependentForParticleEqualsTheBigIntegerJump`, every case with
        a nonzero seed or ordinal (8 of 450 fast tests failed), proving the extraction is
        exercised by both layouts and not bypassed by either.
      - Independent host-vs-CPU-accelerator test:
        `tests/Random.Tests/KernelEqualityTests.cs`,
        `IndependentForParticleKernel.Run`'s `lowOut[baseIndex + 0] = streams.S1.Low;` →
        `streams.S1.Low + 1`, confined to the kernel path. Red: exactly
        `KernelEqualityTests.IndependentForParticleMatchesOnHostAndTheCpuAccelerator`
        (449 of 450 fast tests still passed).
      - Dispatch test: `src/Random/StreamSeeds.cs`, the layout branch condition
        `layout == StreamLayout.Independent` → `layout == StreamLayout.Original`
        (swapping which seed source each branch returns). Red: both
        `StreamSeedsTests.OriginalLayoutMatchesOriginalSeeds` and
        `StreamSeedsTests.IndependentLayoutMatchesIndependentSeeds` (448 of 450 fast
        tests still passed); no other class caught it, since every other test reaches
        `OriginalSeeds`/`IndependentSeeds` directly, not through the dispatcher.
      - Low-28-bit distinctness test: `src/Random/IndependentSeeds.cs`, `Seed2Low`'s
        tail `...0005` → `...0003` (colliding role 2's low-28-bit class with role 1's).
        Red: `IndependentSeedsTests.LowTwentyEightBitClassesAreDistinct` (plus
        `SeedMatchesTheSha256Rule(role: 2)` and the "S2" snapshot case, which the
        mutation also broke against the SHA-256 rule). This mutation also showed that
        `LowTwentyEightBitClassesAreDistinct` and `PairwiseRatiosStayFarFromASmallInteger`
        must read `IndependentSeeds.Streams`'s actual tabulated constants, not
        recompute the SHA-256 rule a second time: the first version of these two tests
        checked only the from-scratch rule and stayed green under this same mutation,
        because the rule computation never touched the (wrong) production literal.
      - Pairwise-ratio test: `src/Random/IndependentSeeds.cs`, `Seed3Low`/`Seed3High`
        replaced by `3 · Seed4 mod 2¹²⁸` (a low-28-bit class, 27, that stays distinct
        from every role's, so only the ratio invariant breaks). Red:
        `IndependentSeedsTests.PairwiseRatiosStayFarFromASmallInteger` (ratio
        `s3·s4⁻¹ = 3`, highest set bit 1), plus `SeedMatchesTheSha256Rule(role: 3)` and
        the "S3" snapshot case (447 of 450 fast tests still passed).
- [x] The `ForBatchedParticle` rows of the levels table (`OriginalSeeds.ForBatchedParticle`,
      `StreamSeeds.ForBatchedParticle`, host-vs-CPU-accelerator, the three persistent lags,
      the removed cross-group replay) are green, with the names of the tests: 2026-09-19,
      `PropStruct.Random.Tests`, 471 fast cases
      (`dotnet test tests/Random.Tests --filter Category!=Long`; the fast count in the
      first criterion above is not restated there, since it would go stale at the next
      addition — AGENTS.md §8, "numbers repeating the length of a list").

  ⚠ 2026-09-19, same day: this criterion first read "the two persistent lags" and
  "468 fast cases", for the `{1, 2}, {3}, {4, 5}, {6}` grouping. The coordinator's
  follow-up measurement (`src/Random/BOOT.md`, "Batched derivation", ⚠ of the same
  day) found that grouping made the statistical criterion markedly worse, not better,
  and traced it to a third persistent lag (`S3 · S4⁻¹`) the `{1, 2}, {3}, {4, 5}, {6}`
  grouping did not preserve. Regrouped to `{1, 2}, {3, 4, 5}, {6}`, with a new test
  (`BatchedDerivationTests.PersistentRatioS3TimesS4InverseIsConstantAndEqualsTheOriginals`)
  and 3 more fast cases (471 total, the new test's 3 `SeedOrdinalPairs` cases); every
  other `ForBatchedParticle` test needed no change except
  `ArithmeticTests.ForBatchedParticleEqualsTheBigIntegerJumpWithTheRoleGroupOffset`,
  whose expected group assignment for S3 moved from `3 · group38` to `1 · group38`.
- [x] Every new check is proven non-degenerate by a recorded mutation (AGENTS.md §13):
      2026-09-19, five mutations applied and reverted, each seen red then green again
      (`git diff --stat` empty after revert):
      - `A38` constant test: `src/Random/Mcg128.cs`, `A38High` last hex digit `c` → `d`.
        Red: `ArithmeticTests.A38EqualsTheBigIntegerModularPower`,
        `ArithmeticTests.ForBatchedParticleEqualsTheBigIntegerJumpWithTheRoleGroupOffset`
        (every generated case; 462 of 468 fast tests still passed).
      - Role-group assignment test: `src/Random/OriginalSeeds.cs`,
        `ForBatchedParticle`'s `S6 = Mcg128.Multiply(original.S6, group2)` →
        `group1` (colliding stream 6's group with streams 4/5's). Red:
        `ArithmeticTests.ForBatchedParticleEqualsTheBigIntegerJumpWithTheRoleGroupOffset`
        (every case) and `BatchedDerivationTests.RoleGroupOffsetRemovesTheSpuriousCrossGroupReplay`
        (every case; 460 of 468 fast tests still passed) — the collision this mutation
        creates between stream 6 and stream 4 is exactly the defect the role-group
        offset exists to remove.
      - Batched dispatch test: `src/Random/StreamSeeds.cs`, `ForBatchedParticle`'s branch
        condition `layout == StreamLayout.Independent` → `layout == StreamLayout.Original`
        (swapping which seed source each branch returns). Red:
        `StreamSeedsTests.ForBatchedParticleOriginalLayoutMatchesOriginalSeedsForBatchedParticle`
        and `StreamSeedsTests.ForBatchedParticleIndependentLayoutMatchesIndependentSeedsForParticle`
        (466 of 468 fast tests still passed); no other test reaches `ForBatchedParticle`
        through the dispatcher.
      - Persistent-lag pairing test: `src/Random/OriginalSeeds.cs`, `ForBatchedParticle`'s
        `S5 = Mcg128.Multiply(original.S5, group1)` → `group0` (moving S5 out of S4's
        group). Red: `ArithmeticTests.ForBatchedParticleEqualsTheBigIntegerJumpWithTheRoleGroupOffset`
        (every case) and all three cases of
        `BatchedDerivationTests.PersistentLagsHoldInsideOneDerivedParticle` (460 of 468
        fast tests still passed) — the X21/X2 lag test needs S4 and S5 to share their
        group, not merely to exist.
      - Batched host-vs-CPU-accelerator test: `tests/Random.Tests/KernelEqualityTests.cs`,
        `ForBatchedParticleKernel.Run`'s `lowOut[baseIndex + 0] = streams.S1.Low;` →
        `streams.S1.Low + 1`, confined to the kernel path. Red: exactly
        `KernelEqualityTests.ForBatchedParticleMatchesOnHostAndTheCpuAccelerator` (467
        of 468 fast tests still passed).
      - `Kernels.DeriveStreams` wiring test (`tests/Execution.Tests`, not this node, but
        exercised from here since `Execution` is this feature's other half):
        `src/Execution/Kernels.cs`, `DeriveStreams`'s
        `StreamSeeds.ForBatchedParticle(...)` reverted to `StreamSeeds.ForParticle(...)`.
        Red: exactly `RunBatchEqualsSequentialContinuedBatchesTests.RunBatchOverCycle0EqualsSequentialRunContinuedBatchCallsBitForBit`
        (47 of 48 fast `Execution.Tests` still passed) — proving `RunBatch` really calls
        `ForBatchedParticle` and not the unbatched jump.

  Two more mutations, the same day, prove the regrouping's own new test
  (`git diff --stat` empty after each revert):
      - S3 role-group test: `src/Random/OriginalSeeds.cs`, `ForBatchedParticle`'s
        `S3 = Mcg128.Multiply(original.S3, group1)` → `group2` (putting stream 3 back
        in stream 6's group instead of streams 4/5's). Red: both
        `ArithmeticTests.ForBatchedParticleEqualsTheBigIntegerJumpWithTheRoleGroupOffset`
        and `BatchedDerivationTests.PersistentRatioS3TimesS4InverseIsConstantAndEqualsTheOriginals`
        (every case of each; 463 of 471 fast tests still passed).
      - `Big128.Inverse`'s own test-helper oracle: `tests/Random.Tests/Big128.cs`,
        `Order` (`2¹⁰⁰`) changed to `2⁹⁹`, breaking the modular-inverse identity
        `Big128.Inverse` relies on. Red: exactly
        `BatchedDerivationTests.PersistentRatioS3TimesS4InverseIsConstantAndEqualsTheOriginals`
        (every case; 468 of 471 fast tests still passed) — no other test in this node
        calls `Big128.Inverse`, so this also proves the new test is the only one
        exercising it.

- [x] 2026-09-23: the replica-shift rows of the levels table are green, with the names of the tests and each
      proven non-degenerate by a recorded mutation, seen red and reverted
      (`tests/Random.Tests/ReplicaShiftTests.cs`; root task "epsx(4) exclusion", `tests/Fixtures/BOOT.md`
      "Seed-patched replicas are shifts" and `src/Random/BOOT.md` "Seed and replica jumps are rigid shifts"):

      - `JumpOfTwoPow80/79IsARigidShiftAcrossConsecutiveStates` (6 streams each) and the negative control
        `JumpOfTwoPow40IsNotARigidShiftAcrossConsecutiveStates` (6 streams): 18 fast cases, all green
        (`dotnet test tests/Random.Tests -c Release --filter "FullyQualifiedName~ReplicaShiftTests&Category!=Long"`).
      - `Epsx4RecomputedFromTheGeneratorMatchesEveryHpepa3IndependentReplica`: worst relative error 7.64e-4
        against the 2e-3 tolerance, over all 32 HPEPA3 independent replicas.
      - `ReplicaArcOfShiftsHasLessThanHalfTheWholeCirclesSpreadAtTheirOwnMedianDrawCount`: circle sd 2.902e-5,
        32-replica arc sd 1.273e-5, ratio 0.439 (N = 123,946,374, the replicas' own median `NFY`).

      Three mutations applied and reverted, each seen red then green again (`git diff --stat` empty after
      revert):
      - Rigid-shift boundary: `JumpOfTwoPow79IsARigidShiftAcrossConsecutiveStates`'s own exponent,
        `BigInteger.One << 79` → `BigInteger.One << 70` (valuation 98, below the ≥100 threshold
        `src/Random/BOOT.md` names). Red: all 6 streams, every one failing at state 1 (`state·a^J − state`
        changes between the first two states of the orbit, exactly the property this test exists to catch).
      - Half-step drop (the task's own example): `Epsx4RecomputedFromTheGeneratorMatchesEveryHpepa3IndependentReplica`'s
        jump, `k·2⁸⁰ + 2⁷⁹` → `k·2⁸⁰` (dropping `+ (BigInteger.One << 79)`). Red: replica 1 alone already fails
        (the test stops at the first failing replica) — fixture `epsx(4)=9.37e-05`, recomputed `1.22e-05`,
        relative error 0.87, three orders of magnitude past the 2e-3 tolerance.
      - Original-layout seed (the task's own other example): `ReplicaArcOfShiftsHasLessThanHalfTheWholeCirclesSpreadAtTheirOwnMedianDrawCount`'s
        three uses of `IndependentSeeds.Streams.S4` → `OriginalSeeds.Streams.S4` (the delta computation and the
        base histogram). Red: ratio rises from 0.439 to 0.839, past the `< 0.5` bound — the `Original` layout's
        own stream 4 does not carry the same reduced-arc-spread property this test asserts of `Independent`.

- [x] 2026-09-25: `tests/Fixtures`' own Python seed arithmetic agrees with this node's C#, state for
      state (architecture audit 2026-09-24, R5: nothing checked the two independently — a drift would
      silently validate the wrong layout). `tests/Random.Tests/PythonSeedArithmeticCrossCheckTests`, 5
      fast cases, reads only `src/Random`'s published contract (`OriginalSeeds`/`IndependentSeeds.Streams`,
      `.ForParticle`, `Mcg128.Advance`) against the two scripts' new `print-states` entry points
      (`tests/Fixtures/API.md`, "## generate.py", "## Measurement scripts", added by this task; neither
      script runs `PropStructV3.exe` under `print-states`):
      - `BaseStatesOfBothLayoutsMatchThePythonScript`: `generate.py print-states --layout lagged|independent`
        (no `--ks`) against `OriginalSeeds.Streams`/`IndependentSeeds.Streams` — also this cross-check's own
        positive control (see below);
      - `LaggedReplicaStatesForEveryKTheFixturesUseMatchThePythonScript`,
        `IndependentReplicaStatesForEveryKTheFixturesUseMatchThePythonScript`: `generate.py print-states
        --ks 1..32` (every `k` any reference formulation's `R` uses, HPEPA3's 32 the largest) against
        `Mcg128.Advance(OriginalSeeds.Streams.S{1..6}, kLow, kHigh)` /
        `IndependentSeeds.Streams.S{1..6}`, the exponent `k·2⁸⁰ + 2⁷⁹` split by `Big128.Split` — the published
        `Advance`, not a second jump formula;
      - `RunOriginalStatesForTheTreesSeedSpreadMatchThePythonScript`: `run_original.py --print-states` at
        seeds 0–4, `seedIndex·2¹⁶` (`seedIndex` 0–15, the rate table's own stride) and `k·2¹⁷` (`k` 0–7, the
        batched-mode two-sample setup's stride) against `OriginalSeeds`/`IndependentSeeds.ForParticle(seed, 0)`;
      - `EveryProvenanceReplicaJumpMatchesTheFormula`: every one of `tests/Fixtures/provenance.json`'s 96
        lagged and 96 independent entries (machine-read, not a typed sample, AGENTS.md §6) has its own
        `"jump"` field equal to `k·2⁸⁰ + 2⁷⁹` for its own `"k"` — the fixtures' own committed record, not
        only a fresh script run, cross-checked too.

      Proven right once (root BOOT.md's Taboos, "every check ... proven twice"): `BaseStatesOfBothLayoutsMatchThePythonScript`'s
      `"lagged"` case is `original_stream_value(1..6)`, which `SourceLimbsTests` already proves equals the
      Fortran source's own packed seeds (lines 52–63) — so this criterion's green also shows the Python
      arithmetic reproduces those seeds, through the Python path, not only through `src/Random`'s.

      Proven red twice, reverted after each (`git diff --stat` empty both times):
      - Python limb: `tests/Fixtures/generate.py`, `_MULTIPLIER_LIMBS`' last limb `1526` → `1527`. Red:
        `BaseStatesOfBothLayoutsMatchThePythonScript`, `LaggedReplicaStatesForEveryKTheFixturesUseMatchThePythonScript`
        and `RunOriginalStatesForTheTreesSeedSpreadMatchThePythonScript` (every case touching the `Original`
        layout's `original_stream_value`; the two `Independent`-layout tests, built from SHA-256 instead,
        stayed green — 2 of 5 fast cases passed);
      - Python exponent: `tests/Fixtures/generate.py`, `replica_jump_exponent`'s `(1 << 79)` → `(1 << 78)`.
        Red: exactly the two replica tests,
        `LaggedReplicaStatesForEveryKTheFixturesUseMatchThePythonScript` and
        `IndependentReplicaStatesForEveryKTheFixturesUseMatchThePythonScript` (3 of 5 fast cases still
        passed) — the base-state and reference-mode tests never call `replica_jump_exponent`, so they were
        unaffected, proving the mutation was caught by the arithmetic it actually changed.
      - Committed fixture: `tests/Fixtures/provenance.json`, every `"jump": "1813388729421943762059264"`
        (`k = 1`) changed to `"...059265"`. Red: exactly `EveryProvenanceReplicaJumpMatchesTheFormula`
        (the other four cases, which never read `provenance.json`, stayed green).

⚠ 2026-09-24: the test names cited above were renamed for CA1707 (underscores removed
from method names, no change of meaning); the old → new map is
`tests/test-renames-2026-09-24.txt`. No criterion's date moved.

- [x] `SourceLimbsTests`, `ArithmeticTests` and `DrawStreamTests` read
      `tests/Fixtures/cases/random/source_limbs.json`, `LegacySource.cs` is gone, and
      the committed limbs equal the source's under `Fixtures`' `Category=Legacy`
      derive check; the fast set reads no file of the original (stage S2, 2026-10-04:
      one limb of the `54-55` range, 7916 to 7917, red in 1 of the 449 cases of the
      three classes, and the same limb of `1649-1650` in 225 of 449; reverted, green;
      `generate.py verify` regenerates the file byte for byte; 494 cases of the fast set
      with the original absent).

## Taboos

- Do not type seed limbs into a test: read them from the source.
