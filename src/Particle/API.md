# API.md — Particle

Namespace `PropStruct.Particle`. The attempt program, the kernel form of the setup and
cycle inputs, the accumulator layouts, the normalization of the pocket histogram and
the fold. Surface internal to the tree (`InternalsVisibleTo` for `Statistics`,
`Execution`, `Simulation`, their tests and `ILGPURuntime`). All types are
kernel-compatible and callable on the host thread.

## Setup and cycle inputs ✅

```csharp
internal struct ModelSetup
{
    public int FractionCount;                 // NMM
    public int SizeLaw;                       // JZZ: 2 uniform in D, any other value uniform in 1/D²
    public double CellSize, CategoryStep;     // Di, Dj, m
    public double Dmin, Dmax;                 // m; Dmax from PARAM (tail probability)
    public double Lambda;                     // sLamd, line 378
    public double Ak1, Ak2, Ak3, Ak4;
    public int Variant;                       // ivar
    public double Alpha, PocketCoefficient, BridgeCoefficient, NnMin, NnMax;   // k5, k7, k8, k6, nn_max
    public int Ndok, Nkarm, Ncat, Nc;
    public int NeighbourBudget, PocketRedrawBudget;
    public PrecisionKind Kind;                 // root BOOT.md, "Precision kind"; unset = Binary64
    public AccumulatorLayout Layout;
}

internal struct FractionTable
{
    public ArrayView<double> Bounds;          // 2·NMM, lower and upper per fraction, m
    public ArrayView<double> Cumulative;      // Z11, NMM + 1
    public ArrayView<byte> PocketForming;     // SFR, NMM, 1 or 0
}

internal struct CycleInputs
{
    public int CycleFlag;                     // IPRIS: 0 in cycle 0, 1 afterwards
    public double Dmaxxx;
    public ArrayView<double> Qks1;            // Nkarm, frozen
    public ArrayView<double> Pdoksmall;       // Ndok
}
```

`ModelSetup` is produced by `Statistics.Setup.Prepare`. The driver builds the views of
`FractionTable` and `CycleInputs` from the host arrays that `Statistics` returns
(`SetupTables`, the next `pdoksmall` and `Dmaxxx`) and from `PocketHistogram.Normalize`
(Qks1).

## Accumulator layout ✅

```csharp
internal struct AccumulatorLayout
{
    // Offsets into the integer totals, in the order of BOOT.md "Integer totals".
    public int Conditions, Nfx, Nfy, Nfz, Nfq, Nfw, IbridgeTotal, LoopCompletions,
               AlldokFract, Alldok, Qks, FqkarmCor, Coef, Qmkm1, Qmkm2, Qdoks,
               IntegerLength;
    // Offsets into a record, in the order of BOOT.md "Real-valued record".
    public int Xss, DokBase41, DokBase31, DokSur41, DokSur31, Sd4, Sd3, D41, D31, Dp31, Dp41,
               DpMax, DpMaxCor, JammedTotal, NnTotal, VmkmTotal2, VdokTotal2,
               Allvdok, Vdokstr, Vsmkm, Svd, VmkmTotal, VdokTotal,
               Vks, FmkarmCor, Fmkarm2, Dokp41, Dokp31,
               RecordLength;
    public int ScratchLength;                  // 3·Nkarm + 2: fmkarm2_loc, FQKS, DPOC

    public static AccumulatorLayout Create(int fractionCount, int ndok, int nkarm, int ncat, int nc);
}
```

⚠ 2026-09-17: `ScratchLength`'s comment first read "3·Nkarm + 2 + scalar locals", implying room
beyond the three arrays. The scalar locals of Fortran lines 426-445 (`TU`, `ipocket_loc`,
`Vdok_loc`, …) turned out not to need array storage at all: they live for one `Attempt.Run`
call only, so they are ordinary kernel-thread locals (registers/stack), exactly like `Random`'s
own generator state variables. `ScratchLength` is exactly `3·Nkarm + 2`, checked by
`AccumulatorLayout.Create` and by every constructed-input test that allocates a scratch buffer
of this size (`tests/Particle.Tests`).

**Field contract** (2026-09-17, moved here from `BOOT.md`: this is what a neighbour needs to
read or write either buffer without reading `Attempt.Run`; which Fortran line writes each
field and why it sits in the buffer it does stays in `BOOT.md`, "Accumulators", which links
back here instead of repeating this table). Sizes: `NMM` fractions, `Ndok`, `Nkarm`, `Ncat`
from `AccumulatorLayout.Create`'s own parameters, `Nc = 1000`. A field of length `> 1` is
indexed `layout.Field + k` for `0 ≤ k < length`, e.g. `layout.Conditions + 3` or
`layout.Xss + 2`; every multi-cell field below is used this way, not only the two named in
the audit that asked for this table.

**Integer totals** (`long`, atomic add), in this order:

| Field | Length | Stride / cell meaning |
|---|---|---|
| `Conditions` | 9 | one cell per rejection test of the attempt; `Conditions + k` is the original's `conditions(k + 1)` |
| `Nfx`, `Nfy`, `Nfz`, `Nfq`, `Nfw` | 1 each | draw counters (`NFX`, `NFY`, `NFZ`, `NFQ`, `NFW`) |
| `IbridgeTotal` | 1 | committed bridges |
| `LoopCompletions` | 1 | port only: completed neighbour loops |
| `AlldokFract` | `NMM` | cell `k` = draws (base and neighbour) landing in fraction `k` (0-based) |
| `Alldok` | `Ndok` | cell `k` = draws landing in oxidizer-size cell `k` (0-based, `k = int(D / Di)`) |
| `Qks` | `Nkarm` | cell `k` = pockets whose size falls in cell `k` (0-based, `k = int(Rk / Di)`) |
| `FqkarmCor` | `Nkarm` | cell `k` = corrected pocket count of the same cell as `Qks` |
| `Coef`, `Qmkm1`, `Qmkm2` | `Nc` each | cell `k` = draws whose own coefficient bins into cell `k` |
| `Qdoks` | `Ncat · Ndok` | row-major by category: category `c` (0-based), oxidizer-size cell `i` (0-based, same indexing as `Alldok`) is `Qdoks + c * Ndok + i` |

**Real-valued record** (`double`, one per particle, summed over all its attempts):

| Field | Length | Kind | Stride / cell meaning |
|---|---|---|---|
| `Xss` | 7 | sum | cell `k` (0-based, `k = 0..6`) is the original's `XSS`k |
| `DokBase41`, `DokBase31`, `DokSur41`, `DokSur31` | 1 each | sum | base/neighbour 4th/3rd moment |
| `Sd4`, `Sd3`, `D41`, `D31`, `Dp31`, `Dp41` | 1 each | sum | scalar moments |
| `DpMax`, `DpMaxCor` | 1 each | max, start 0 | running maximum pocket radius (`Fold.Add` takes the maximum, never the sum) |
| `JammedTotal`, `NnTotal`, `VmkmTotal2`, `VdokTotal2` | 1 each | sum, committed | end-of-loop totals |
| `Allvdok`, `Vdokstr` | `Ndok` each | sum | cell `k` = oxidizer-size cell `k` (same indexing as `Alldok`) |
| `Vsmkm`, `Svd`, `VmkmTotal`, `VdokTotal` | `Ndok` each | sum, committed | cell `k` = base-particle size cell `k` (0-based, `k = int(Dr / Di)`) |
| `Vks`, `FmkarmCor` | `Nkarm` each | sum | cell `k` = pocket-size cell `k` (same indexing as `Qks`) |
| `Fmkarm2` | `Nkarm` | sum at loop end, cycles ≥ 1 | cell `k` = pocket-size cell `k` (same indexing as `Qks`) |
| `Dokp41`, `Dokp31` | `Ncat` each | sum | cell `k` = category `k` (0-based, same indexing as `Qdoks`'s row) |

"Committed" fields are written only on `Accepted` in cycles ≥ 1 (`BOOT.md`, "Accumulators").

## Precision kind ✅

```csharp
internal enum PrecisionKind : byte
{
    Binary64 = 0,  // every accumulator sums in double (the default: an unset ModelSetup.Kind is this)
    Original = 1,  // rounds to binary32 after every addition, exactly the fields RealFourAccumulators.generated.txt names
}
```

⚠ 2026-09-24: was named `Double`, now `Binary64` (CA1720, owner's decision: an identifier
may not contain a type name).

`ModelSetup.Kind` selects it; `Attempt.Run` reads it once per call and needs nothing
else from a caller. `Binary64` reproduces exactly the bits every caller built before this
option existed already got. `Original` rounds fifteen of the record's twenty-seven
sum-kind fields (root BOOT.md, "Precision kind is an option of every run") after
every addition, reproducing the original's own REAL*4 accumulation loss; the other
twelve, every size, distance and decision variable, and `DpMax`/`DpMaxCor` (running
maxima, not sums) stay `double` under either kind. The rounded set is **generated**
from the Fortran declaration block, never typed:
`classify-real4-accumulators.py` → `RealFourAccumulators.generated.txt` (this node's
own committed tool and artifact, checked the way `docs/ORIGINAL-DEFECTS.md` is
checked — a test regenerates and fails on any byte difference).

This node covers the accumulators only: the root's precision kind also names a setup
plane, computed once per run before the particle loop, and a per-cycle plane, computed
after each cycle, both `Statistics`' own (implemented 2026-09-23 and 2026-10-01,
`src/Statistics/BOOT.md`, "## Setup plane" and "## Report").

**`Original` accumulation is sequential only** (root BOOT.md): binary32 rounding
depends on the order of the additions, and this node has no atomic `double` and takes
no position on scheduling — it only rounds the term it is given, in the order its
caller calls it. A caller that runs several particle threads against one shared
`record` cell under `Original` accumulation gets an order this node did not choose and
cannot make canonical. Refusing that configuration is **not this node's job**: `Attempt.Run`
never throws and reports no status for it (root BOOT.md, "Failures are values"); the
node that owns run configuration (batch size, thread count) is the one that can tell
whether a call is sequential and is the one root BOOT.md asks to refuse it with a
status.

## Size law ✅

```csharp
internal static class SizeLaw
{
    public static void Sample(
        int sizeLaw, int fractionCount, ArrayView<double> bounds, ArrayView<double> cumulative,
        double x, double x1,
        out double diameter, out int fraction);
}
```

Fortran `SIZE` (lines 1761–1776): the one function of the particle program a neighbour may
call outside an attempt. `Statistics` needs it for the tail draw of `PARAM` (the `Dmax` of
`SetupEchoes`), and duplicating it there would be a second implementation of part of the
particle program (root `BOOT.md`, Taboos). It stays kernel-compatible, so its inputs are
`ArrayView`s: a host caller obtains them from the driver that owns an accelerator
(`Simulation`, through `Execution`), never by standing up an accelerator of its own —
`Execution` is the only node that knows accelerators exist (root `BOOT.md`,
`## Decomposition`).

⚠ 2026-09-18: this section is new. `Sample` existed from the node's first commit but was
not published, and `Statistics` reached it undeclared (AGENTS.md §3), building a private
ILGPU context per call to make the views. Found by the audit of `Statistics`; the owner
publishes the member and moves the view-building to the driver, as above.

## Attempt ✅

```csharp
internal enum AttemptOutcome : byte
{
    Accepted, RestartedAfterLoop, RestartedInsideLoop,
    NeighbourBudgetExceeded, BridgeDrawBudgetExceeded, IndexOutOfRange,
}

internal static class Attempt
{
    public static AttemptOutcome Run(
        in ModelSetup setup, in FractionTable fractions, in CycleInputs cycle,
        ref StreamSet streams,
        ArrayView<long> integerTotals,        // shared, atomic add
        ArrayView<double> record,             // this particle's record
        ArrayView<double> scratch);           // this particle's scratch
}

internal static class PocketHistogram
{
    public static void Normalize(in AccumulatorLayout layout, ArrayView<long> integerTotals, ArrayView<double> qks1);
}

internal static class Fold
{
    public static void Add(in AccumulatorLayout layout, ArrayView<double> records, int particleCount,
                           ArrayView<double> realTotals);
}
```

Semantics:

- `Run` advances `streams` by exactly the draws the attempt made; the record and the
  totals receive the attempt's side effects whatever the outcome; on a failure outcome
  the state of totals and record is that of the moment of failure.
- The driver refreshes QKS1 with `Normalize` after `Accepted` and `RestartedAfterLoop`
  in reference mode.
- `Add` is order-fixed: particle 0 first; sums add, `DpMax` and `DpMaxCor` take the
  maximum.
- Not thread-safe per particle: one thread per record and scratch; the integer totals
  may be shared by any number of threads.

## Fold by field ✅

```csharp
internal static class Fold
{
    public static void AddField(in AccumulatorLayout layout, ArrayView<double> records, int particleCount,
                                int field, ArrayView<double> realTotals);
}
```

One field of `Add`, in the same particle order and with the same rule (maximum for
`DpMax` and `DpMaxCor`, sum otherwise), for the fold kernel of `Execution`, which runs
one thread per field. `Add` is `AddField` over every field, so there is one
implementation of the fold (added 2026-09-18 in the design session of `Execution`;
implemented the same day in `Execution`'s own coding session, `src/Particle/Fold.cs`).

## Side effects

`Run` writes only to `integerTotals`, `record`, `scratch` and `streams`; `Normalize`
only to `qks1`; `Add` only to `realTotals`.

## Out of scope

- Batching, launching, relaunching, stream derivation per particle: `Execution`.
- Setup from a formulation, end-of-cycle quantities, `pdoksmall`, `Dmaxxx`, the
  `*_nmax` indices: `Statistics`.
- Refusing `PrecisionKind.Original` with batched mode: this node's `Attempt.Run`
  performs whatever accumulation it is asked to and never inspects how many threads
  call it; the node that owns run configuration decides whether a call is sequential
  and reports the refusal (root BOOT.md, "Precision kind"; this node's own API.md,
  "## Precision kind" above).
- Recording the run's kind in the result or in `results.m`: `Simulation`/`Output`.
