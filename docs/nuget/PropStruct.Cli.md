# PropStruct.Cli

`propstruct` is the command-line front end of
[PropStruct](https://www.nuget.org/packages/PropStruct): a formulation file in the
original `.dat` format in, `results.m` in the original's layout out (and JSON on
request), for the pocket model of the local structure of composite solid propellants.
It is a port of the Fortran program PropStructV3.

## Install

```console
dotnet tool install --global PropStruct.Cli
```

The tool targets `net10.0` on Windows x64 and needs the .NET 10 runtime.

## Minimal example

Take a formulation, for instance
[`inpt.dat`](https://github.com/baryon-asymm/PropStruct/blob/main/tests/Fixtures/Legacy/formulations/inpt.dat),
the smallest of the shipped ones, and run it:

```console
propstruct run inpt.dat
```

`results.m` is written to the current directory, where the original wrote it. The
model's fourteen parameters, which the original asked for interactively, are options
with the original's defaults (`propstruct defaults` lists them); a size is written as
the original's menu took it, a value of 0.1 or more in micrometres, any other value in
metres.

To reproduce the numbers the original program itself prints, take the three switches
together; the run is sequential, as the original was:

```console
propstruct run inpt.dat --mode reference --layout original --precision original --output ref.m
```

The defaults, `--precision binary64` and `--layout independent`, are the more accurate
answer and differ from the original's own. Other options: `--accelerator auto|cpu|cuda`,
`--seed <n>`, `--json <path>`, `--quiet`. `propstruct --help` lists every command and
flag, `propstruct --version` prints the tool's version, and `propstruct devices` lists
the accelerators found.

Exit codes: 0 ok, 1 the run failed with a status, 2 invalid arguments or invalid input,
3 accelerator or infrastructure error, 4 an unhandled exception, 130 interrupted.

## CUDA

`--accelerator cpu` needs nothing beyond the tool. `--accelerator cuda`, or `auto` on a
machine with a usable GPU, additionally needs, at run time:

- an NVIDIA driver with CUDA 12.8 or newer;
- `nvvm64_40_0.dll` and `libdevice.10.bc`, both from an NVIDIA CUDA Toolkit 12.8 or
  newer.

`auto` falls back to the CPU when no usable CUDA device or library is found;
`PROPSTRUCT_NO_CUDA=1` refuses CUDA.

## License and credit

This package is licensed `MIT AND NCSA`: PropStruct is MIT, and the bundled `ILGPU.dll`
is under the University of Illinois/NCSA Open Source License, reproduced in
`THIRD-PARTY-NOTICES.txt`. PropStruct is a port of PropStructV3 by V. A. Babuk and
A. A. Nizyaev; the original program is not distributed. The package includes `NOTICE`
with the full attribution.
