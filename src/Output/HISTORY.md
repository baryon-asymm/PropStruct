# HISTORY.md — Output

Append-only. Newest first. Each entry carries the date, the section of `BOOT.md` it
came from, and the original text in full. Read only by following a dated pointer from
`BOOT.md`; the start procedure (AGENTS.md §10) does not read this file.

<a id="karmcoef-control-value-2026-09-24"></a>

## 2026-10-04 — from "## Design decision (2026-09-24): print the stored setup" — the review note on the control value, told in full

Moved to keep the node within its limit (AGENTS.md §15) once its dependency on
`tools/legacy` was declared.

  ⚠ 2026-09-24, review: the value above was first `karmcoef = 12.345`, and that control
  did not discriminate. DVF's list-directed `REAL*4` field prints seven significant
  digits in fixed form at that magnitude (`12.34500`), and binary32(12.345) rounds to
  the same seven digits as the unrounded double, so the check would have passed
  unchanged on the pre-change code, before this branch rounded `PocketCoefficient` at
  all — right for the wrong reason, the shape the root taboo's positive-control rule
  exists to catch. `0.03` sits below the field's own 0.1 threshold, where it switches to
  eight-digit exponent form and the error shows: `2.9999999E-02` against the double's
  own `3.0000000E-02`. `PocketCoefficientHeaderEchoTests` now proves the discrimination
  directly (`CoefficientLineGivenThePortsDoubleUnroundedDiffersFromTheOriginalsRealFourStore`,
  `PortsCoefficientLineUnderDoubleDoesNotMatchTheArchivedOriginal`), the second checked
  against the pre-change build too: it prints `3.0000000E-02` under
  `--precision original`, since nothing rounded `PocketCoefficient` there either.

---

<a id="time-line-start-time-2026-09-19"></a>

## 2026-10-01 — from "## Design decisions (2026-09-19)" — the time-line correction told in full

Moved to keep the node within its limit (AGENTS.md §15).

  ⚠ 2026-09-19: this line first said the time line prints the run's own *start time*.
  `RunDiagnostics` (`src/Simulation/API.md`) carries no timestamp, only the `TimeSpan
  Elapsed` of the run; the original's own `hour1`/`minut1`/`sec1` are themselves an
  elapsed duration (computed from a start and end clock read, not printed as a
  time-of-day). Found while wiring `ResultsMWriter`'s footer line against
  `RunDiagnostics`'s real shape; corrected to name the field that exists.

---
