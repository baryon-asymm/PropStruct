# PropStruct

[![CI](https://github.com/baryon-asymm/PropStruct/actions/workflows/ci.yml/badge.svg)](https://github.com/baryon-asymm/PropStruct/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/baryon-asymm/PropStruct/blob/main/LICENSE) [![.NET 10](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
[![NuGet PropStruct](https://img.shields.io/nuget/v/PropStruct.svg?label=PropStruct)](https://www.nuget.org/packages/PropStruct) [![NuGet PropStruct.Cli](https://img.shields.io/nuget/v/PropStruct.Cli.svg?label=PropStruct.Cli)](https://www.nuget.org/packages/PropStruct.Cli)

Local structure of a composite solid propellant by Monte Carlo: the pockets between
large oxidizer particles, the bridges between pockets, and from them the size
distributions and the mass-medium size of metal agglomerates (the pocket model). A
.NET library and a command-line tool, ported from the Fortran program PropStructV3, in
which one numerical program runs on the CPU and, whole cycles of base particles at
once, on an NVIDIA GPU (CUDA) in double precision.

Input is a formulation file in the original `.dat` format and the fourteen model
parameters the original asked for interactively, given as options with the original's
defaults. Output is a structured result holding every quantity the original prints and
a `results.m` in the original's layout, so that existing MATLAB post-processing keeps
working.

## Install

```console
dotnet add package PropStruct
```

```console
dotnet tool install --global PropStruct.Cli
```

The first is the library; the second is `propstruct`, its command-line front end. The
platform is Windows x64 with .NET 10.

## Quick start

From the command line, on the smallest shipped formulation,
[`tests/Fixtures/Legacy/formulations/inpt.dat`](tests/Fixtures/Legacy/formulations/inpt.dat):

```console
propstruct run inpt.dat
```

`results.m` is written to the current directory. `propstruct --help` lists every flag
and `propstruct defaults` the fourteen parameters with the original's values.

From .NET code, the same kind of run with one model parameter, `EpsDok`, changed from
its default (the program is [`samples/Quickstart`](samples/Quickstart), built against
the packed library in CI):

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

## Comparing with the original

A run carries two choices that decide how it relates to the original program, and the
result and `results.m` record both.

- `Original` precision and the `Original` stream layout, in reference mode, reproduce
  the original: its generator bit for bit and the binary32 values its executable
  computes with, in the accumulators, the setup plane and the per-cycle plane, and so
  its printed answers up to the declared differences, within the statistical spread of
  its own seeds. The sample path itself is not byte for byte the original's (root
  [BOOT.md](BOOT.md), "Precision kind").
  `propstruct run inpt.dat --mode reference --layout original --precision original`
  asks for this.
- The defaults, `Binary64` precision and the `Independent` layout, are the more
  accurate answer and differ from the original's own. The original's REAL*4
  accumulators move its headline answers by five to seven per cent on one reference
  formulation, HPEPA3, and its seeds make consecutive draws dependent, which biases
  some pocket and bridge quantities (root [BOOT.md](BOOT.md), "Known bias of the
  original's seeds"). Expect results that differ from old ones.

The original's defects are reproduced or declared, one row each, in
[docs/ORIGINAL-DEFECTS.md](docs/ORIGINAL-DEFECTS.md).

## Platform and CUDA

Windows x64. The CPU accelerator needs nothing beyond the package. CUDA additionally
needs, at run time, an NVIDIA driver with CUDA 12.8 or newer, plus `nvvm64_40_0.dll`
and `libdevice.10.bc` from a CUDA Toolkit 12.8 or newer. `Auto` falls back to the CPU
when no usable device or library is found, and `PROPSTRUCT_NO_CUDA=1` refuses CUDA.

## Building and testing

```console
dotnet build PropStruct.sln
dotnet test PropStruct.sln --filter "Category!=Long&Category!=Legacy"
```

The second line is the fast set and what CI runs. A fact that reads the original
program, which is not distributed with this repository, carries `Category=Legacy`, so
without the original the filter above is the one to use; unfiltered, those facts fail,
by design, rather than skip. Maintainers who hold the original point
`PROPSTRUCT_LEGACY_DIR` at it. The repository is a tree of directories, each with a
`BOOT.md` (its specification) and an `API.md` (its contract), run by the protocol of
[AGENTS.md](AGENTS.md); start at [BOOT.md](BOOT.md).

## Credit and license

PropStruct is a port of PropStructV3 by V. A. Babuk and A. A. Nizyaev (Baltic State
Technical University "VOENMEH"); the original program is not distributed. The model is
described in V. A. Babuk, A. A. Nizyaev, "Modelling of structure of the composite solid
propellants and problem of the description of agglomeration process", Khimicheskaya
Fizika i Mezoskopiya (Chemical Physics and Mesoscopy), 2014, vol. 16, no. 1, pp. 31-42.
See [NOTICE](NOTICE). This repository is MIT-licensed, Copyright (c) 2026
Eduard Burachek; see [LICENSE](LICENSE).
