# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

Nothing yet.

## [0.1.0] - 2026-10-06

The first release: the library `PropStruct` and the tool `PropStruct.Cli` (`propstruct`),
released together under one version. Below 1.0.0 a minor version may break the surface.

### Added
- The pocket model of the original program PropStructV3: pockets between large oxidizer
  particles, interpocket bridges, and the size distributions and mass-medium size of
  metal agglomerates, computed by one numerical program that runs on the CPU and, whole
  cycles of base particles at once, on an NVIDIA GPU in double precision.
- Reference mode (the original's sequence, one thread) and batched mode (whole cycles
  at once, CPU or CUDA), with a per-run choice of accelerator, batch size, budgets and
  seed.
- The precision kind of a run: `Binary64`, the default, and `Original`, which
  reproduces the binary32 values the original's executable computes with in its
  accumulators, its setup plane and its per-cycle plane. `Original` is sequential only.
- The stream layout of a run: `Independent`, the default, six disjoint orbits of the
  original's generator, and `Original`, the original's six seeds with their defects.
  `Original` is sequential only. The original's generator is reproduced bit for bit.
- A reader of the original `.dat` format, the fourteen model parameters with the
  original's menu defaults, a writer of `results.m` in the original's layout, and a JSON
  writer and reader of the result; every run records its precision kind and layout.
- The `propstruct` command line: `run`, `devices` and `defaults`, with exit codes 0 to 4
  and 130.

### Known limitations
- Windows x64 only; the CPU path and the CUDA path are both measured there alone.
- The original's generators GSV=1 and GSV=3 are not ported; every shipped formulation
  uses GSV=2, and a formulation with another generator is refused with exit code 2.
- Under `Original` precision the attempt plane (sizes drawn, distances, decision
  variables) and the print plane's own arithmetic are computed in `double`, not in the
  original's binary32: the last printed digit of a value can differ. The sample path is
  not byte for byte the original's.
- No interactive input and no graphics; the model itself is unchanged.
- The original program is not distributed. Its defects are reproduced or declared in
  `docs/ORIGINAL-DEFECTS.md`.

[Unreleased]: https://github.com/baryon-asymm/PropStruct/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/baryon-asymm/PropStruct/releases/tag/v0.1.0
