# ACCEPTANCE.md — Benchmarks

## Acceptance criteria

- [x] 2026-10-03: HPEPA3 and HMX are measured for reference mode, the host-thread path
      (1 and 16 threads) and CUDA, with the original's own figure on the same machine,
      at the shipped defaults (budget 8192, CUDA group of 64), and every row carries its
      provenance (`BOOT.md`, `## Figures`, the ship-window table, clean commit `531d66c`;
      the quiet checks before, between and after, and the first, void attempt of the
      window, are in `HISTORY.md#ship-window-2026-10-03`). The CPU accelerator's own
      kernel-launch path (`cpu`) stays out of scope for this criterion, unresolved (the
      escalation in `BOOT.md`): its row reads `not available`, not a blank, for both
      formulations.

  ⚠ 2026-09-20: was "the CPU accelerator" among the four measured paths, now its row
  reads `not available` (AGENTS.md §6) →
  HISTORY.md#benchmarks-criterion-1-cpu-2026-09-20

  ⚠ 2026-09-28: was dated 2026-09-20, against the table measured that day, now
  re-verified against the Stage 0 table (Decision B): the same command, the same
  machine, a later commit → `HISTORY.md#figures-2026-09-20-superseded`.

  ⚠ 2026-10-02: was dated 2026-09-28, against the Stage 0 table (`ce30f62`, budget 32,
  `host1` unpinned), now re-verified against W2 (`7e8f6f7`, budget 8192, `host1`
  pinned) → `HISTORY.md#figures-w2-2026-10-02`

  ⚠ 2026-10-03: was dated 2026-10-02, against W2 (`7e8f6f7`, CUDA group automatic, 256),
  now re-verified against the ship window (`531d66c`, group 64) →
  `HISTORY.md#figures-w2-superseded-2026-10-03`
- [x] 2026-09-20: the measurement is repeatable: a second run of the same command on
      the same machine reproduced every figure within the spread the node recorded with
      it, for every path and both formulations (`HISTORY.md#figures-2026-09-20-superseded`,
      "Repeatability check"). Not re-run this session: Stage 0's own `--repeats 3`
      already records a spread per row, and the property under test (this node's own
      repeats capture the run-to-run variation) does not depend on which commit is
      measured.

  ⚠ 2026-10-02: was "this node's own repeats capture the run-to-run variation",
  withdrawn as a general claim: CUDA HMX reads 5030-5095 over four invocations on one
  machine in one hour, wider than the ± 2 to ± 31 of any one n = 3 spread (`BOOT.md`,
  `## Figures`). The 2026-09-20 check stands as dated; a figure's spread bounds its
  own invocation only → `HISTORY.md#figures-w2-2026-10-02`

- [x] 2026-09-20: the cost of one cycle against the whole run is measured once and
      noted (`BOOT.md`, `## Figures`, "Cycle cost measurement"): on `inpt`, cycle 1
      reads 5-6× below the plateau a longer run settles into, a warm-up effect that is an
      absolute, not per-particle, cost and so shrinks in relative weight as a
      formulation's own `N` grows; HPEPA3's and HMX's cycle-1 windows are long enough
      that two full runs of each found no figure outside its own recorded spread. The
      one-cycle shortcut is therefore a measured, bounded decision for the two
      formulations this node measures, not a general claim about every formulation.
- [x] 2026-09-20: the node builds in Release with zero warnings, refuses to run under a
      Debug build (proven non-degenerate: built and ran under `-c Debug`, saw the
      refusal; then under `-c Release`, saw it proceed), and one end-to-end smoke run
      on the smallest formulation (`inpt`, 200 particles, one repeat) produced a
      correctly-shaped row for every path — `reference`, `host1` (2431/s), `host16`
      (5605/s, faster than `host1` as expected), `cuda` (4188/s, a real device on this
      machine), `original` (6714/s) and `cpu` (`not available`, the escalation text
      above) — proving the plumbing without recording it as a figure: these numbers
      are not in the `## Figures` table of `BOOT.md` and must not be copied there (too
      few particles, one repeat, and this machine had a concurrent unrelated workload at
      the time).
- [x] 2026-10-02: the profile at the shipped budget is recorded, and read by one script
      whose output reproduces Stage 1's recorded figures but three, corrected in place:
      W3 (`Kernel_RunAttempts` at 196 registers per thread and block 256; R2 0.81 % on
      HPEPA3 and 2.26 % on HMX, cycle 1) and W4 (control passed, D/T* = 3.245) in
      `BOOT.md`, "### W3/W4 record"; the window's quiet checks, the SHA-256 of the
      reader, its two proofs (`reader-proof.txt`, `reader-proof-w4.txt`) and the hook
      patches, and the reader's three outputs in `HISTORY.md#w34-2026-10-02`. Not met by
      this evidence and still open: R3's counter clause and R4-R6, which need counters
      that were not granted. The reader and the patches are a working note, not in the
      tree; the SHA-256 is what attributes the figures to them.
- [x] 2026-10-03: the launch shape of `Kernels.RunAttempts` on CUDA is measured by the
      pre-registered W5 rule and the verdict recorded: window C, four rounds, arms A,
      G128, G64, G32, R at budget 8192 on HMX and HPEPA3, no round void, no stop; the
      rule's verdict is "adopt G64" (HMX ×1.359, HPEPA3 ×1.366 against ILGPU's automatic
      group). `BOOT.md`, "### W5: launch shape"; the tables, the SHA-256 of the reader,
      the rule, the patches and the run logs in `HISTORY.md#w5-record-2026-10-03`. The
      shipped constant moves no bit and repeats the gain: the ship window, 2026-10-03
      (`BOOT.md`, "Shipped 2026-10-03"; `HISTORY.md#ship-window-2026-10-03`;
      `src/Execution/ACCEPTANCE.md`).

- [x] The baseline row's `LegacyRunner` finds `PropStructV3.exe` and `dforrt.dll`
      through `legacy.py path` and ends with that command's message, never a default
      path, when `PROPSTRUCT_LEGACY_DIR` is unset or a hash differs; the measurement of
      the row is unchanged by the move (stage S2, 2026-10-04, `--formulation inpt
      --paths original --repeats 1`: unset, the row reads "not measured on this machine"
      with the command's message naming the variable; on a copy of the directory with
      one byte of `dforrt.dll` changed, with that file's name and both hashes; with the
      variable set the row measures 6165 particles/s (one run, so no spread, and not a
      figure of record: the code path is the one it was, only the path of the files
      comes from the command).
