# API.md — Input

Namespace `PropStruct.Input`. The formulation and the model parameters, and the reader
of the original `.dat` format. Public.

## Formulation ✅

```csharp
public readonly record struct Length(double AsWritten)
{
    public bool IsMicrometres { get; }                        // AsWritten ≥ 0.1
    public double Metres { get; }
    public static Length FromMetres(double metres);            // precondition: metres < 0.1
    public static Length FromMicrometres(double micrometres);  // precondition: micrometres ≥ 0.1
}

public sealed record Formulation(
    string Name,
    double OxidizerDensity, double PropellantDensity,         // PLOT1, PLOT2, kg/m³
    double OxidizerMassFraction, double MetalMassFraction,    // GGG0, Gm
    double Ak1, double Ak2, double Ak3, double Ak4,
    int SizeLawCode,                                          // JZZ as read
    int Cycles, int ParticlesPerCycle,                        // KXX (≥ 1), N
    double GeneratorWarmup,                                   // NNZ as read, unused
    ImmutableArray<OxidizerFraction> Fractions,
    ImmutableArray<int>? PocketFormingFractions)              // SFR, when read
{
    public SizeLaw SizeLaw { get; }
}

public readonly record struct OxidizerFraction(double MassShare, Length LowerBound, Length UpperBound);

public enum SizeLaw { None = 0, UniformInReciprocalSquare = 1, Uniform = 2 }   // JZZ; values other than 2 read as 1

public static class DatFile
{
    public static Formulation Read(string path, bool readPocketFormingFractions = false);
    public static Formulation Parse(Stream stream, string name, bool readPocketFormingFractions = false);
}

public sealed class FormulationFormatException : FormatException
{
    public int LineNumber { get; }
    public FormulationFormatException();
    public FormulationFormatException(string message);
    public FormulationFormatException(string message, Exception innerException);
    public FormulationFormatException(string message, int lineNumber);
    public FormulationFormatException(string message, int lineNumber, Exception innerException);
}
```

⚠ 2026-09-24: `SizeLaw` gained `None = 0` and `FormulationFormatException` gained the
three standard exception constructors (parameterless, message-only, message-and-inner),
each with `LineNumber` 0. Both additions satisfy CA1032/CA1008 under the root's
"no suppression anywhere" decision (root `BOOT.md`, "Language and build") without
changing any existing behaviour: `Formulation.SizeLaw` still only ever produces
`UniformInReciprocalSquare` or `Uniform`, and the pre-existing two constructors are
unchanged.

## Model parameters ✅

The meaning of `Dmin`, `Alpha`, `NnMin` and `BridgeCoefficient` is that of the matching
coefficients Dок min, k1, k2 and k3 of V. A. Babuk, A. A. Nizyaev, Khimicheskaya Fizika
i Mezoskopiya 16 (1), 2014, pp. 31–42 (reference in `NOTICE`).

```csharp
public sealed record ModelParameters
{
    public static ModelParameters Default { get; }
    public Length Dmin { get; init; }                         // menu [1]
    public Length CellSize { get; init; }                     // Di, [2]
    public Length CategoryStep { get; init; }                 // Dj, [3]
    public double EpsDok { get; init; }                       // [4]
    public double Alpha { get; init; }                        // k5, [5]
    public double NnMin { get; init; }                        // k6, [6]
    public double PocketCoefficient { get; init; }            // k7 karmcoef, [7]
    public double BridgeCoefficient { get; init; }            // k8 mkmcoef, [8]
    public double TailProbability { get; init; }              // alfa, [9]
    public double NnMax { get; init; }                        // [10]
    public double HomogenizedOxidizerFraction { get; init; }  // gdokns, [11]
    public bool ReadPocketFormingFractions { get; init; }     // answer1, [12]
    public int Variant { get; init; }                         // ivar, [13]
    public double AggregatedOxideFraction { get; init; }      // eta, [14]
}
```

## Errors

| Situation | Behaviour |
|---|---|
| missing file | `FileNotFoundException` |
| missing or malformed value, repeat count, `/`, non-integral integer, `GSV ≠ 2` | `FormulationFormatException` with the line number |
| `Length.FromMetres`/`FromMicrometres` outside its range | `ArgumentOutOfRangeException` |

## Side effects

Reads the named file or stream only.

## Out of scope

- Model preconditions: `Statistics`.
- Writing formulations.
