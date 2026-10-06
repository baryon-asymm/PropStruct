# API.md — Output

Namespace `PropStruct.Output`. Writers of the result. Public.

## Writers ✅

```csharp
public static class ResultsMWriter
{
    public static void Write(Formulation formulation, ModelParameters parameters, SimulationResult result, string path);
    public static void Write(Formulation formulation, ModelParameters parameters, SimulationResult result, TextWriter writer);
}

public static class ResultsJson
{
    public static void Write(SimulationResult result, string path);
    public static SimulationResult Read(string path);
}
```

⚠ 2026-09-19: `ResultsMWriter.Write` was first sketched as `Write(SimulationResult result, string path)`, matching
only "the result" of this node's dependencies. Writing the header (`Plot1`/`Plot2`/`Gdok`/`Gm`/`Nfr`/`JZ`/
`Dmin`/`Di`/`Dj`/`NN_min`/`NN_max`/`k5`/`alfa`/`eps`/`ivar`/`karmcoef`/`mkmcoef`/`Zok*`/`Gfr`/`Dfr`/`sfr`,
Fortran lines 1184-1221) needs the formulation and the model parameters of the run as well: none of those are
carried by `SimulationResult` (`src/Simulation/API.md`'s own field mapping has no place for them, since they
are the *input* to a run, not part of its result). The two extra parameters are exactly `Input`'s own
`Formulation` and `ModelParameters` (this node's `## Dependencies`); the caller already holds both, since it
had to read the formulation before it could run the simulator at all.

## Print-expression map ✅

The print-time expressions of Fortran 1184–1453 and where `ResultsMWriter` carries
each (`BOOT.md`, "## Print-time expressions"). Prose and tables only: nothing here is
a type. Three files in this directory, read by `tests/Output.Tests`, never by another
node:

| File | Written by | Content |
|---|---|---|
| `PrintExpressions.generated.txt` | `list-print-expressions.py generate` | every row the rule finds |
| `PrintExpressionSites.txt` | hand | one map row per generated row |
| `ResultsMWriter.cs` | hand | fragment lines ending `// Fortran n[, n…]` |

Lines starting `#` are comments. Fields are separated by ` | `.

**Generated row:** `line | role | expression | context`. `line` is the first physical
line of the statement; `expression` is lower case with blanks removed; `context` is the
string item before an `item`, the call and label of an `array` or `length`, `if` of a
`condition`, `<target> =` of an `assignment`, or `-`. Roles: `item`, `array`, `length`,
`condition`, `assignment`, `bound`, `argument` (the last two have no row today). The
header names what is not a row and why.

**Map row:** `line | role | expression | member | fragments | upstream`; the first three
fields repeat a generated row, each generated row has exactly one map row.

| Field | Form |
|---|---|
| `member` | `ResultsMWriter.<Method>`; or `not ported: <reason> (BOOT.md, "<phrase>")`, the phrase found in `BOOT.md` of this node, or of the root when written `(root BOOT.md, …)` |
| `fragments` | C# fragments joined by ` && `, or `-` for a row not ported; blanks in a fragment match any blanks in the code, and a match is bounded by non-identifier characters |
| `upstream` | `-`, or `<Record>.<Member>` of `PropStruct.Simulation` that carries the value computed; the member and the row's line occur in one paragraph of `src/Simulation/API.md` |

**Citation:** a line of `ResultsMWriter.cs` that carries a fragment ends with
`// Fortran n[, n…]`, the lines of the rows it carries. Every ported row has a
fragment on a line citing it, the first inside the named method, and every cited line
carries a fragment of a row it cites. No other comment has that form.

**Script:** `python -X utf8 list-print-expressions.py generate|verify|selftest`, run
from the repository root. `generate` rewrites the generated file (UTF-8, LF); `verify`
regenerates in memory and compares bytes; `selftest` runs the rule on a constructed
fragment and on pinned real lines. Exit 0 on success, 1 on a failed check or an
unrecognised statement, 2 on a usage error.

## Errors

| Situation | Behaviour |
|---|---|
| unwritable path | the I/O exception of the runtime |
| malformed JSON | `JsonException` |

## Side effects

Writes the named file, overwriting it.

## Out of scope

- Reading `results.m`: the parser lives in `tests/Harness`.
