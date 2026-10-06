# PropStruct

PropStruct computes the local structure of a composite solid propellant by Monte Carlo:
the pockets between large oxidizer particles, the bridges between pockets, and from
them the size distributions and the mass-medium size of metal agglomerates (the pocket
model). It is a port of the Fortran program PropStructV3, rebuilt so that one numerical
program runs on the CPU and, whole cycles of base particles at once, on an NVIDIA GPU
(CUDA) in double precision.

This package is the library. The command-line tool is `PropStruct.Cli` (`propstruct`).

## Install

```console
dotnet add package PropStruct
```

The package targets `net10.0` on Windows x64. Its one dependency is ILGPU.

## Minimal example

The program reads a formulation in the original `.dat` format
([`inpt.dat`](https://github.com/baryon-asymm/PropStruct/blob/main/tests/Fixtures/Legacy/formulations/inpt.dat)
is the smallest shipped), runs the simulator and writes the result as `results.m` in
the original's layout and as JSON:

<!-- snippet: QuickstartUsings -->
```csharp
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.Simulation;
```

<!-- snippet: Quickstart -->
```csharp
var formulation = DatFile.Read("inpt.dat");

var parameters = ModelParameters.Default with { EpsDok = 0.02 };

var options = new SimulationOptions
{
    Parameters = parameters,
    Mode = ExecutionMode.Batched,          // or ExecutionMode.Reference
    Accelerator = AcceleratorKind.Auto,    // Auto, Cpu, Cuda
    BatchSize = null,                      // null: the whole cycle, capped by the record budget
    Streams = StreamLayout.Independent,    // the default; Original runs in reference mode only
    Precision = PrecisionKind.Binary64,    // the default; Original runs in reference mode only
    Seed = 0,                              // 0: the layout's initial states
};

using var simulator = Simulator.Create(options);
var result = simulator.Run(formulation);   // SimulationFailedException on a failed status

ResultsMWriter.Write(formulation, parameters, result, "results.m");
ResultsJson.Write(result, "results.json");
```

The defaults are the more accurate answer: arithmetic in `double` and six disjoint
random streams. To reproduce the numbers the original program itself prints, take the
three switches `Mode = ExecutionMode.Reference`, `Streams = StreamLayout.Original` and
`Precision = PrecisionKind.Original` together; they run in one thread, as the original
did. The same example runs as `samples/Quickstart` in the source repository, built
against this package in CI.

## CUDA

The CPU accelerator (`AcceleratorKind.Cpu`) needs nothing beyond this package and
ILGPU. The CUDA accelerator (`AcceleratorKind.Cuda`, or `Auto` on a machine with a
usable GPU) additionally needs, at run time:

- an NVIDIA driver with CUDA 12.8 or newer;
- `nvvm64_40_0.dll` and `libdevice.10.bc`, both from an NVIDIA CUDA Toolkit 12.8 or
  newer.

`AcceleratorKind.Auto` falls back to the CPU accelerator when no usable CUDA device or
library is found, and so does setting the environment variable `PROPSTRUCT_NO_CUDA=1`,
which refuses CUDA.

## Documentation

The contract of every namespace and the design of the model are in the source
repository: [README](https://github.com/baryon-asymm/PropStruct/blob/main/README.md)
and the `API.md` of each directory.

## License and credit

This package is MIT-licensed. PropStruct is a port of PropStructV3 by V. A. Babuk and
A. A. Nizyaev; the original program is not distributed. The package includes `NOTICE`
with the full attribution.
