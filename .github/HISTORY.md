# HISTORY.md — .github

Append-only. Newest first. Each entry carries the date, the section of `BOOT.md` it came
from and the original text in full. Read only by following a dated pointer from
`BOOT.md`; the start procedure (AGENTS.md §10) does not read this file.

---

<a id="preflight-compute-names-2026-10-04"></a>

## 2026-10-04 — from "## Invariants", "One GPU job on the machine at a time"

Moved to make room for the isolation caveat of the runner account (the same section,
first invariant). The correction as it stood in `BOOT.md`:

>   ⚠ 2026-10-04: was "a compute process holds the GPU", now those four names: on the
>   reference machine, a desktop under WDDM, `nvidia-smi --query-compute-apps` lists 25
>   graphics processes with their memory `[N/A]`, none of them CUDA work, so the bare list
>   would fail every run.

---
