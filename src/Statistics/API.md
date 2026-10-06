# API.md — Statistics

Namespace `PropStruct.Statistics`. Setup before the first cycle and processing at the
end of each cycle, on the host. Surface internal to the tree (`InternalsVisibleTo` for
`Simulation`, the tests of both, and `Particle.Tests`/`Execution.Tests` — each of
those two needs `Setup.Prepare`'s own setup for the reference-formulation rows of its
own `BOOT.md`, decided 2026-09-18 at the tree root, AGENTS.md §11).

## Setup ✅

```csharp
internal enum SetupStatus
{
    Ok, InvalidCoefficients, InvalidCellSize, InvalidMinimumSize, InvalidFractionBounds,
    ZeroFractionShare, InvalidPocketFormingFractionCount, NoPocketFormingFraction,
    InvalidTailProbability, InvalidOxidizerFraction, InvalidNnWindow, NoActiveFraction,
}

internal sealed record SetupTables(
    double[] Bounds,            // 2·NMM, m
    double[] Cumulative,        // Z11, NMM + 1
    double[] Share,             // ZX, NMM
    byte[] PocketForming,       // SFR, NMM
    double Zss,                 // PARAM's own ZS, "## Setup plane"
    double[] MassShare);        // GDOK as read/rounded, NMM ("## Setup plane", "One stored setup")

internal sealed record SetupInputs(          // every setup-plane input CycleStatistics/Output need,
    double OxidizerDensity, double PropellantDensity,   // as stored ("## Setup plane", "One stored setup")
    double OxidizerMassFraction, double MetalMassFraction,
    double HomogenizedOxidizerFraction, double EpsDok, double AggregatedOxideFraction);

internal readonly record struct TailDraw(double X, double X1);   // Particle.SizeLaw.Sample's own x/x1

internal sealed record PendingEchoes(
    double Dokm, double Doksd, double Ddokmax,
    double TailProbabilityModified, double OxidizerMassFractionEffective);   // SetupEchoes minus Dmax

internal sealed record SetupEchoes(
    double Dokm, double Doksd, double Ddokmax, double Dmax,
    double TailProbabilityModified, double OxidizerMassFractionEffective);   // alfa after PARAM, GGG

internal static class Setup
{
    public static void Sizes(
        ImmutableArray<OxidizerFraction> fractions, Length cellSize, Length categoryStep, double ak4,
        out double ddokmax, out int ndok, out int nkarm, out int ncat, out int nc);

    public static void AnalyticSizes(
        int sizeLaw, int fractionCount, double[] massShare, double[] share, double[] bounds, PrecisionKind precision,
        out double dokm, out double doksd);

    public static SetupStatus Prepare(
        Formulation formulation, ModelParameters parameters, PrecisionKind precision,
        int neighbourBudget, int pocketRedrawBudget,
        out ModelSetup setup, out SetupTables tables, out TailDraw draw, out PendingEchoes pending,
        out SetupInputs inputs);

    // Compatibility overload (added 2026-09-24, "## Setup plane", "One stored setup"): forwards to the
    // five-out overload above and discards `inputs`. Every existing tree caller reachable from outside this
    // node (`Particle.Tests`/`Execution.Tests`, AGENTS.md §11) calls under PrecisionKind.Binary64 only, so
    // discarding the extra out-parameter changes nothing it reads.
    public static SetupStatus Prepare(
        Formulation formulation, ModelParameters parameters, PrecisionKind precision,
        int neighbourBudget, int pocketRedrawBudget,
        out ModelSetup setup, out SetupTables tables, out TailDraw draw, out PendingEchoes pending);

    public static SetupEchoes CompleteEchoes(ref ModelSetup setup, PendingEchoes pending, double dmax);
}

internal static class FractionLaw
{
    public static void Build(
        int sizeLaw, int fractionCount, double[] massShare, double[] bounds, PrecisionKind precision,
        out double[] share, out double[] cumulative, out double sum);

    public static bool TryTailDraw(
        int fractionCount, double[] bounds, double[] cumulative, double tailProbability,
        out double x, out double x1, out double tailProbabilityModified);
}

internal static class Binary32
{
    public static double ToNearestRepresentable(double value);
    public static double Multiply(double a, double b);
    public static long TruncatedQuotient(double numerator, double denominator);
    public static long TruncatedQuotientOfProduct(double product, double denominator);
}
```

**Setup hand-off** (decided 2026-09-18, `../Simulation/API.md`'s own "Setup hand-off"
section): `Prepare` computes every pure-`double` part of the setup, including
`FractionLaw.TryTailDraw`'s own exclusion loop, and stops exactly where the original's
`PARAM` calls `SIZE` (Fortran line 1754). It returns `setup` with `Dmax` still at its
default (`0`), `tables` for the fraction-law arrays, `draw` (the `x`/`x1` pair
`Particle.SizeLaw.Sample` needs) and `pending` (every other `SetupEchoes` scalar). The
driver — `Simulation`, which owns the accelerator through `Execution` — builds
`ArrayView`s over `tables.Bounds`/`tables.Cumulative` through its own accelerator,
calls `SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, ..., draw.X, draw.X1, out var
dmax, out _)`, and passes `dmax` to `Setup.CompleteEchoes(ref setup, pending, dmax)`,
which writes `setup.Dmax` and returns the completed `SetupEchoes`. `Statistics` itself
never builds an `ArrayView` or stands up an accelerator (found by the audit of this
node: the previous version of `FractionLaw.MaxBaseSize` did both, undeclared, to call
`Particle.SizeLaw.Sample` before that member was even published — `../Particle/API.md`,
"## Size law").

**Setup under a precision kind** (decided and implemented 2026-09-23, BOOT.md, "## Setup
plane"; root BOOT.md, "Precision kind"): `Prepare`, `FractionLaw.Build` and
`Setup.AnalyticSizes` each take the run's `PrecisionKind`. Under `Original` they return
the setup plane's values as the Fortran stores them (`tables.Zss` among them, "## Setup"
above); under `Binary64` every result is bit-identical to what this node always
computed, except `SetupEchoes.Ddokmax`, whose stored value moved at the eighth
significant digit when the 2026-09-24 review corrected which store it reads (BOOT.md,
"## Invariants", "The setup follows the precision kind").

⚠ 2026-09-27: was "under `Binary64` every result is bit-identical", without the
`Ddokmax` exception BOOT.md's own invariant has carried since the 2026-09-24 review.
Found by the D6 review of this node's own documents (AGENTS.md §12, a hidden
deviation of this file specifically, not of BOOT.md, which already stated it).

## Cycle ✅

```csharp
internal static class CycleStatistics
{
    public static CycleReport Compute(
        in ModelSetup setup, SetupTables tables, SetupEchoes echoes, SetupInputs inputs,
        int cycleIndex,                         // 0 for the warm-up
        Span<long> integerTotals,               // rewritten in place: Qdoks
        Span<double> realTotals,                // rewritten in place: Dokp41, Dokp31, VdokTotal, VdokTotal2
        double[] nextPdoksmall,                 // Ndok, written
        out double nextDmaxxx,
        out CategoriesStatus categoriesStatus); // Categories.MergeAndDescribe's own status, passed through
}

internal enum CategoriesStatus { Ok, CategoryCountExceedsCapacity }

internal static class Categories
{
    public static CategoriesStatus MergeAndDescribe(
        int ndok, int ncat, double cellSize, double categoryStep, double epsDok, double dpMax,
        AccumulatorLayout layout, Span<long> integerTotals, Span<double> realTotals, PrecisionKind precision,
        out int dpRow, out double[] dpockets, out double[] dokp43, out double[] qdokkarm, out double[][] qdokso);
}

internal static class SmallParticles
{
    public static double[] Probability(
        int ndok, double cellSize, double ak2, double ggg, double gdokleft, double karmcoef,
        double oxidizerDensity, double propellantDensity, double[] vdokstr, PrecisionKind precision);

    public static double MaxSize(
        int ndok, double cellSize, double ddokmax, double karmcoef, double mkmcoef, double[] pdoksmall, PrecisionKind precision);
}

internal sealed record CycleReport
{
    /* every field of BOOT.md "## Report", named there field for field; the type is coded in CycleReport.cs. */
}
```

Semantics: `Compute` is deterministic in its inputs; it writes only the totals named
in the comments and `nextPdoksmall`. The driver builds the `FractionTable` and
`CycleInputs` views of `Particle` from `SetupTables`, `nextPdoksmall`, `nextDmaxxx` and
the normalized `Qks`.

Cycle under a precision kind (2026-10-01, from the listing 2026-10-03): `Compute`
reads the run's kind from `ModelSetup.Kind`, as `Setup.Prepare` stored it, and passes
it to the three members below it. Under `Original` it takes its stores, compiler
temporaries, sum schedules and register or home reads from
`CyclePlane.listing.generated.txt` ("## Cycle listing map and table"), so every report
field whose Fortran variable is REAL*4 is what the executable's home holds, and
`nextPdoksmall`, `nextDmaxxx` and the rewritten `Dokp41`, `Dokp31`, `VdokTotal`,
`VdokTotal2` are written as it stores them; under `Binary64` every result is
bit-identical to before. The kind is explicit on `MergeAndDescribe`, `Probability` and
`MaxSize`, never optional.

The forms of its products are `CyclePlaneOrder`'s, each held bit for bit by
`PlaneFormsTests` against the table's `order` text. Which form a call site uses, and in
which association it passes the factors, no bit holds: swapped for `Binary64`'s at 805,
806, 968–973 and 1089, the plane moves no bit of any case or oracle output
(`tests/Statistics.Tests`, "## Mutations"). It is held structurally instead
(`CyclePlaneOrderSiteTests`, `ACCEPTANCE.md`, A9): every `order.` call ends its line
with `// Fortran n`, and the association of its factors, read through the `Original`
expression of the form it names, is a sub-expression of the `order` text of a table
row at a cited line, a commutation being one order.

⚠ 2026-10-03: was "products' order" among what `Compute` takes from the table, stated
without that gap, then "planned"; the arbiter's review of stage 3 found it (F3), the
structural check closes it.

⚠ 2026-10-01: was a second `Compute` overload taking `Formulation` and
`ModelParameters`, now removed: under an `Original` setup it fed unrounded inputs to a
rounded plane (BOOT.md, "## Report", "The per-cycle plane from the executable's
listing").

⚠ 2026-10-03: was "what the original's home holds (`CyclePlane.generated.txt`)", the
source-read table, now the listing's →
HISTORY.md#per-cycle-plane-source-read-rules-2026-10-03

## Cycle listing map and table ✅

`CyclePlaneListing.map.txt`, hand-written data in this directory: the one link between
the addresses of `tests/Fixtures/Legacy/PropStructV3.cycle-plane.listing.txt` and the
Fortran's names, which no tool derives (the executable carries no symbols and no line
tables; `dumpbin /headers` reports an empty debug directory). Read by
`classify-cycle-plane-listing.py` and, from the oracle's stage, by
`tests/Fixtures/cycle_plane_oracle.py`; a data contract, so a change of its format is a
change of this section. The line kinds (`|`-separated, `#` a comment) and the row
attributes are documented at the head of `listing_map.py` and `listing_rows.py`:

- `region`: the excerpt's eight ranges and how the reader treats each (`executed`,
  `layout`, `scan`, `callee` (2026-10-03: `PARAM`, run by the oracle through its call and
  not walked), `thunks`, `startup`, `prologue`); `scope`: the ranges whose float events
  the rows must claim;
- `row`: one per stretch of code, its address range, the Fortran lines it compiles, the
  names it uses and its claims (`register=`, `eliminated=`, `stores=`, `temp=`,
  `parts=`); the rows tile every scope without a gap;
- `cell`: a frame slot or absolute address with every tenant name and type in order
  (`:temp` for a compiler temporary); `array`: a descriptor, name, element type, rank;
  `ints`: a run of integer slots with the region that writes it or `external`;
  `input`: a float cell the walk reads before it has written it, with its source;
  `identity`: a total's identity, for the oracle.
- `oracle` (added 2026-10-03, stage W2b; `listing_map.py` keeps them raw, nothing in
  `classify-cycle-plane-listing.py` reads them): the listing oracle's data, one kind per
  second field. `run` (name, first and end address), `callee` (2026-10-03: name, first and
  end address of a routine the pre-loop calls and the oracle runs, no stub), `exit`
  (address, name), `call` (target address, stub label), `slice` (a range run before every plane run), `inject`
  (home, name, type, a setup key or a Python expression over them, what for), `pointer`
  (slot, array: the address of element zero), `array` (array, setup key: filled from the
  setup), `expect` (setup key, cell, type: what the executed pre-loop leaves, set beside
  the model's value), `total` (home or `array` or `-`, name, type, a Python expression:
  what the particle loop leaves, over the setup keys, the oracle's helpers and the
  totals above it), `trip` (guard, back branch, remainder head, block size and the sites
  of one unrolled loop, whose trip counts are measured), `warning` (a compare and the
  call it guards), `draw` and `feature` (how a candidate case is drawn and what a case
  must be seen to reach), `pin` (2026-10-03: a formulation, a key and why: a case the
  search keeps besides the drawn ones, for a quantity the pre-loop alone decides; the
  key says which rule the oracle's self-test shows it to tell: `dokm`, `zss`, `zx`,
  `jzz2`). A total is an input the port also reads, and where the port derives a
  quantity from other totals the expression yields what the port derives. An `expect`
  row names an array by its `array` row (`ZX`, `Z11`): its elements are read.

Six checks hold it against the excerpt, `classify-cycle-plane-listing.py check`, each
seen red by `selftest` on a break of the map or the excerpt made in memory: (a) every
region, scope and row range is on instruction boundaries and the rows leave no gap;
(b) every float store, load and integer copy of a float in a scope lies in exactly one
row and resolves to a name it declares, every REAL assignment of a row's lines is
stored, claimed as a register value or declared eliminated, and every claim is such an
assignment; (c) every call goes to a known thunk at the stack depth its contract
states; (d) every tenant has the Fortran's type and its name occurs in its row's lines;
(e) every float cell read before it is written is an input with a source; (f) every
array descriptor is the one the ALLOCATE statements write.

`CyclePlane.listing.generated.txt`, generated beside it, never edited: one line per
site, in address order, `at | row | lines | target | type | kind | rounds | home | count
| reads | schedule | order`. The kinds, each read off the block-local x87 walk
and never typed: **S** a REAL*4 store (rounds), **Q** a REAL*8 store (exact), **K** a
store or move of a constant, **M** a move of a loaded or merged value, **T** a store to
a compiler temporary, **P** a sum reloaded and stored every pass, **B**`n` a sum
unrolled `n`-fold (reloaded at the block head, stored after pass `n` and every pass of
the remainder), **X** a sum carried in the register through its loop and stored every
pass, **C**`n` `n` stores each reading the one before from the register, **R** a value
held in a register and never stored, **E** a value no line reads or a line with no
code, **CMP** a comparison. `reads` names each operand and where it comes from
(`name:home`, `name:input`, `name:register`), `order` the expression as the executable
forms it. Commands: `generate`, `verify` (byte for byte), `check`, `selftest`. The
table drives the code (`CycleStatistics`, `Categories`, `SmallParticles` and
`Setup.Prepare`, through `CyclePlaneRounding`, `UnrolledSchedule` and `CyclePlaneOrder`)
and the twin of `tests/Fixtures`; `CyclePlaneSites.txt` maps its rounding sites to the
code, checked by `tests/Statistics.Tests/CyclePlaneSiteMapTests`.

The table holds twelve columns, the twelfth being `order`: the `fortran` column, which
quoted the source's statements and was the one place the tree quoted it in bulk, was
dropped by stage S2 of the delivery (root `BOOT.md`, `## Delivery`; the quotation rule
of [legacy](../../tools/legacy/API.md)). `lines` names the source lines and the readers
(`classify-*.py`, the oracle) take what they need from the source in the original's
directory.

⚠ 2026-10-04: was thirteen columns, the last `fortran`, the text of the lines a site
compiles; now twelve, the generator's `Site` no longer carries the text.

## Side effects

Writes into the caller's totals as listed; nothing else.

## Out of scope

- Running particles and refreshing QKS1 during a cycle: `Particle`, `Execution`.
- The cycle loop, `FI`, collecting convergence values: `Simulation`.
- Printing and µm scaling: `Output`.
