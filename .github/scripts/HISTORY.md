# HISTORY.md — .github/scripts

Append-only store of what `BOOT.md` no longer states as current truth (`AGENTS.md`, §15).
Newest entry first; each names its date, the `BOOT.md` section it came from, and carries
the original text in full.

---

<a id="source-mapping-control-2026-10-20"></a>

## 2026-10-20 — from "## Invariants", "Nothing restored from a cache or a source the script did not make" — the control without the mapping

The 0.2.0 rehearsal (run 39406892891, commit b6d1ba7) failed on the hosted Windows
runner in `SourceMappingTest`: with a config that lists the public folder first and has
no package source mapping, NuGet restored the packed folder's package. NuGet asks every
source at once and takes the first answer, so the order of a config without a mapping
decides nothing; the control had passed locally and in the 0.1.0 rehearsal by chance of
timing. It is replaced by the same mapped config with the two folders swapped, whose
answer is determined, and which goes red when the mapping names neither (proved
2026-10-20, the `PropStruct*` pattern changed in scratch). The text as it stood:

>   is never restored either (`test_check_packages.py`, `SourceMappingTest`: two feeds
>   holding different packages of one id and version, the packed one wins, and without the
>   mapping the other does). ILGPU is restored from nuget.org into it; nothing else leaves
>   the machine.
