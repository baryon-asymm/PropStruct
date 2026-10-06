# BOOT.md — legacy

## Purpose

The node that keeps the original program, PropStructV3, out of the repository and keeps
the repository honest about it. Since 2026-10-04 (root `BOOT.md`, `## Delivery`) five
files of the original live outside the tree, in the one directory the environment
variable `PROPSTRUCT_LEGACY_DIR` names, and the public repository holds none of its
code. The node owns three things: **identity**, the SHA-256 of each of the five files,
so that every consumer knows it is reading the recorded original and not another build;
**access**, the one place scripts and tests learn where a file is and whether it is the
right one; and **proof of absence**, a scan that the repository, exported for
publication, reproduces none of the original's text or bytes beyond the quotation rule
below. It also states the rule of the `Legacy` test category, the tests that read the
original, and the gate that makes that category fail rather than skip.

The five files are, by the names the manifest uses: `PropStructV3.zip` (the archive as
received), `PropStructV3.exe` and `dforrt.dll` (the executable and its runtime),
`PropStructv3.for.txt` (the Fortran source, UTF-8, LF, the original's line numbers) and
`PropStructV3.cycle-plane.listing.txt` (the excerpt of the executable's listing,
`tests/Fixtures/API.md`, "## Cycle-plane listing"). The formulations and the outputs
of the archive are data, stay in `tests/Fixtures/Legacy`, and are not the node's.

Stage S2 of the delivery built the node on 2026-10-04: `original.sha256`, the generated
`forbidden.sha256`, `legacy.py`, `scan.py` and their self-tests exist, every declaration
of `API.md` is ✅, and the five files left the repository the same day.

## Invariants

- **Identity is a recorded hash.** `original.sha256` holds one line per file, `<hex
  sha256>  <name>`, the five names above and nothing else (`## Manifest`); the tree
  compares the files it reads against it and never against a hash typed elsewhere. The
  manifest is written once from the files the owner holds and changes only when the
  owner names a different original in a design session.
- **One reader of the variable.** `legacy.py` is the only code in the tree that reads
  `PROPSTRUCT_LEGACY_DIR`; a script reaches a file through `legacy_file(name)`, a test
  through a script, and `tests/Benchmarks`' runner through `legacy.py path`. The path
  resolves lazily, at the first use of a file, so that a script's entry that needs no
  file (`generate.py print-states`, `run_original.py --print-states`) runs without the
  variable.
- **Unset or wrong is a failure, never a skip.** An unset variable ends a command with
  exit code 2 and a file whose hash differs from the manifest with exit code 1, the
  message naming the file, the recorded hash and the actual one; a fact that needs the
  original fails with that message. Nothing in the tree turns either into a skipped
  fact, a default path or a silent pass.
- **The forbidden list is generated.** `forbidden.sha256` holds the SHA-256 of the
  five files and of every other member of the archive that is not data (a member that
  is neither a `.dat` nor an `.m` file), the hash of empty content excluded; it is
  produced from the archive by `legacy.py forbidden`, never typed, and the `Legacy`
  category compares the committed file with a regeneration (`Fixtures.Tests`'
  `TheForbiddenListIsTheOneTheArchiveGenerates`, which runs `legacy.py forbidden
  --check`). It is what `scan.py --hashes` reads, so the scan that CI runs needs no
  original.
- **The scan reports where, never what.** A finding names the file, the line in it and,
  for a statement, the number of the source line it matches; for bytes, the offset
  in the matched section; never the quoted text or bytes, since a failing log may be
  public.
- **The quotation rule** (referenced by the root taboo "No code of the original in the
  repository"):
  1. no generated artefact, table or column carries the source's or the listing's text:
     it names lines and holds derived facts;
  2. a quotation is evidence for one claim: one statement with its line number, and at
     most eight quoted statements in one file;
  3. not code for this rule: output format (labels, format descriptors, print
     expressions), constants reproduced bit for bit (seeds, limbs, menu defaults),
     variable names;
  4. no instruction bytes and no listing lines; addresses are facts.

  The scan enforces what a machine can read, and the count it prints is a lower bound
  of rule 2, not the count. Rule 4 by byte windows. Rule 2 by counting, per file, the
  distinct statements of the source it touches: a statement of 25 characters or more
  when one of its 25-character fragments occurs anywhere in the file, a statement of 10
  to 24 characters only when a quoted span of the file is exactly that statement (a
  backtick span, a `<c>` element, the body of a comment line, a line of a fenced block
  of a Markdown file). Rule 3 by what the statement set leaves out (data, format, write
  and print statements, text between quotes). A reviewer's remainder: rule 1 (a column
  or table that carries source text), a statement shorter than 10 characters, a short
  statement in running prose or in a span with more around it, and a statement split
  over lines.
- **The `Legacy` category** (`Category=Legacy`). A fact that reads one of the five
  files, directly or through a script, carries it, and the fast set excludes it by
  name. No `Legacy` fact runs on a GitHub-hosted runner, where a failure would print
  source lines or bytes in a public log. The category runs on the reference machine,
  in Release, before every release (`dotnet test -c Release --filter Category=Legacy`),
  and the annotated tag of the release carries `Legacy: <commit sha> <date>
  <passed>/<total>` of that run, a line `.github`'s release check reads. Outside the
  maintainer's machine the unfiltered full set is red by design, and `README.md` and
  `CLAUDE.md` give the filter.
- **The gate.** `Fixtures.Tests`' `TheOriginalIsTheRecordedOne` runs `legacy.py check`
  and is a `Legacy` fact: it is red with the variable unset and red on a copy of the
  directory with one byte of one file changed, so every other `Legacy` fact stands on a
  file proven to be the recorded one.
- **Python 3.8+ and the standard library only**, as the other tools: the checks run
  before anything is installed.

## Dependencies

None.

Outside the tree: Python 3.8+ (standard library, `unittest` for the self-tests); the
five files of the original at `PROPSTRUCT_LEGACY_DIR`, for `legacy.py check`,
`legacy.py forbidden` and the full scan only.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- **The directory is flat**: the five files by their manifest names, directly in
  `PROPSTRUCT_LEGACY_DIR`, no subdirectory. The owner's directory is the owner's choice
  and is named in no document of the tree, only the variable.
- **Exit codes** of `legacy.py` and `scan.py`: 0 clean; 1 a mismatch (`legacy.py`) or a
  finding (`scan.py`); 2 an unset variable or invalid arguments, and, for `scan.py`, a
  file of the original that does not match the manifest.
- **What the scan reads.** It walks the tree it is given, skipping `.git`, `bin`, `obj`,
  `__pycache__` and `.claude/worktrees` (the other sessions' copies of the tree, ignored
  by git and never exported), and reads every file as bytes. Checks, in order: (1) the
  SHA-256 of a file against `forbidden.sha256` (an empty file is not a finding); (2) the
  file's name against the extensions of the original's build products (`.exe`, `.dll`,
  `.zip`, `.obj`, `.pdb`, `.for`, `.dsp`, `.dsw`, `.opt`, `.plg`, and a name containing
  `.for.`); (3), in the full scan only, 16-byte windows of the code sections of
  `PropStructV3.exe` and `dforrt.dll` (a window of at most three distinct byte values is
  padding and is not kept), searched in the file raw, in the byte columns of a `dumpbin`
  listing and in any run of hexadecimal digits of 32 or more; (4), in the full scan
  only, the 25-character fragments of the source's statements, normalised (lowercase,
  whitespace removed, text between quotes left out), looked for in every text file, and,
  for a code statement of 10 to 24 normalised characters (`SHORT_STATEMENT_FLOOR` up to
  the fragment length; no quoted text in it), the whole statement against the quoted
  spans of every text file: a backtick span, a `<c>` element, the body of a `//` or `#`
  comment line (not in a Markdown file, where `#` is a heading), every line of a fenced
  block of a Markdown file. The source's data, format, write and print statements, and
  its comments, contribute neither, and `tests/Fixtures/Legacy` (data) and `*.m.txt` are
  not scanned for either. A file that touches more than eight distinct statements, the
  two counts together, is a finding, one line per statement. A source line is read in
  the fixed form (a label in columns 1-5, a continuation mark in column 6) or the tab
  form (a tab within the first six columns, a continuation digit right after a
  label-free tab); the full scan prints what it searched for, `reference: <statements>
  statements, <fragments> fragments, <short> short statements, <windows> code windows`,
  and the most statements any one file touches.
- **A measured baseline** (stage S2, 2026-10-04): the source holds 1,444 statements, of
  which 1,154 are code and give 6,626 distinct fragments and 348 distinct short
  statements of 10 to 24 characters (405 statements); the two executables hold 392,970
  distinct 16-byte windows outside padding. Of the 1,154 code statements the scan sees
  765: 360 by a fragment and 405 whole in quoted spans; it does not see 389, 369 shorter
  than 10 characters and 20 whose quoted text leaves less than a fragment. The tree as
  it was at 75bc2c4, 1,351 files: 5 forbidden hashes, 4 forbidden names, 3 files with
  code windows, 108 statements of the source in the listing table, which then carried a
  `fortran` column. The tree as of S2, exported: see the criteria. **The floor of 10
  characters** (decided 2026-10-04 on the tree of the S2 fix-up, tools/legacy/BOOT.md
  left out of the count, exact-span matching): at floors 4, 6, 8, 10 and 12 the short
  statements are 483, 458, 421, 348 and 300 distinct texts, the quoted spans that equal
  one 64, 47, 37, 26 and 23 in 29, 23, 21, 14 and 14 files, and the most statements any
  one file touches, both counts together, 10, 8, 7, 6 and 6 (`src/Statistics/BOOT.md`
  for the first three, `src/Particle/BOOT.md` for the others). The 38 spans in 15 files
  that a floor of 10 drops against a floor of 4 quote the language's own words and
  assignments of a constant or a bare name (`continue`, `goto`, `else`, `enddo`,
  `endif`, `exit`, `return`, `jj=0`, `qdokss=0`, `rk=aa`, `sfr=1`) and nothing of the
  model; the 22 distinct texts that remain at 10 are all statements of the source, 5 of
  them (a menu default, a close, three reads) the kind rule 3 would exempt, counted
  nonetheless.
- **`legacy.py selftest`** proves the manifest reader and the mismatch path on
  synthetic files written to a temporary directory; it needs no original and holds no
  byte or line of it.

## Manifest

What `original.sha256` holds, written by stage S2 from the owner's five files on
2026-10-04 (the figures are identity, not code):

```text
0478d4faa3314821d13b4075e9c5b549073bb4e113fcca286e34a7aa630867ba  PropStructV3.zip
75770d2954fe94f6bdc3ae5a5c4151c40ebf05b912b668d759c6396fb48bd675  PropStructV3.exe
0d1781837d9d7b3857da3f70ef24dcca828b4ac5a671e93841c156a56130c7f3  dforrt.dll
eb24a816907f3989812fb6a7e0655010a3ba064f90e2a8f1562b78f49e037318  PropStructv3.for.txt
3cd2f1fe7d26c34189758160c5ef50c9b2bb13fdd20f2fca881515d8536d7212  PropStructV3.cycle-plane.listing.txt
```

## Acceptance criteria

- [x] The manifest records the five files and `legacy.py check` is green on the owner's
      directory; red with the variable unset (exit 2) and red on a copy with one byte
      of one file changed (exit 1, the message naming the file and both hashes)
      (2026-10-04: `check` printed five `ok` lines, exit 0; unset, exit 2; on a copy
      with byte 1000 of the source flipped, `mismatch PropStructv3.for.txt recorded ...
      actual ...`, exit 1, and `path` of that file exit 1 while `path dforrt.dll` stayed
      exit 0; `test_legacy.py`'s `CommandLineTest`; `Fixtures.Tests`' gate red on the
      copy and with the variable unset).
- [x] `forbidden.sha256` is regenerated from the archive byte for byte by `legacy.py
      forbidden --check`, and holds the hash of every non-data member, listed by the
      machine and not typed, and the committed file is held by a fact of the category
      (2026-10-04: `forbidden` printed 23 and `forbidden --check` exited 0: the five
      files and 18 other hashes, the archive's 22 files that are neither `.dat` nor `.m`
      holding 21 distinct contents, one of them empty and excluded and two the
      executable's and the runtime's, which are the manifest's; `Fixtures.Tests`'
      `TheForbiddenListIsTheOneTheArchiveGenerates` red with one line of the 23 deleted
      (`forbidden --check` exit 1, "1 hash(es) missing, 23 regenerated against 22
      committed") and green restored).
- [x] `scan.py` is red once and right once (AGENTS.md §13; root `BOOT.md`, taboo "Every
      check that guards a quantitative claim is proven twice"): red on the private tree
      before the removal of the five files, on a planted 600-byte fragment of the
      listing excerpt, on a planted block of nine statements of the source and, with
      `--hashes`, on a planted copy of one of the five files; right on the exported
      public snapshot (2026-10-04, all exit 1 and exit 0 as named: the export of
      75bc2c4, 1351 files, forbidden hashes 5, names 4, code windows 3, statements 108;
      a 600-byte slice of the excerpt in the plane's range planted in `docs`, 1 code
      window; nine consecutive code statements of the source planted, 10 statement
      findings, the ninth statement sharing its fragments with one more; each of the
      five files planted under another name, `--hashes` and no variable, 1 hash finding
      each; the export of 529e259 with `.claude` removed, 1361 files, 0, 0, 0, 0, exit
      0, `reference: 1444 statements, 6626 fragments, 392970 code windows`).
- [x] `scan.py --hashes` runs without the variable set and finds the same hash and
      name findings as the full scan on the same tree (2026-10-04: on the export of
      75bc2c4 the two outputs hold the same 9 hash and name lines, 5 hashes and 4 names;
      without the variable the `--hashes` output is the same bytes).
- [x] The tool's self-tests, `tools/legacy/test_*.py`, are green with no original on the
      machine, every case proven red on its mutation (2026-10-04: 60 cases, 51 of
      `scan.py` and 9 of `legacy.py`. At stage S2, 42 cases and 58 mutations of the two
      modules and the two data files, each turning at least one case red, none
      surviving; the fix-up added 18 cases and 24 mutations of the short-statement code
      (the floor, the fragment length, the quote and I/O exclusions, each of the six
      kinds of quoted span, the fence, the name of the first quotation and the order of
      the findings), 23 turning a case red and 1 equivalent, the `<` against `<=` at
      the fragment length, where both counts reach the same statement. The 58 were not
      re-run).
- [x] The scan counts whole short statements as well as fragments, and is red once and
      right once on them: red on 40 distinct statements of 15 to 24 characters planted
      in four forms, right on the final tree (2026-10-04: 205 such statements in the
      source, every fifth planted, in a scratch copy of the tree; as backtick spans in
      a Markdown file, as `<c>` elements and as `#` comment lines in code, and as the
      lines of a fenced block, each form alone: the scan of a579125 exit 0, the scan of
      the fix-up exit 1, 40 statement findings in the one planted file, each naming the
      file, the line and the source line; the final tree, see the criterion below).
- [x] The quotation rule holds on the exported snapshot: at most eight statements in any
      one file, counted by fragments and whole short statements together, the maximum
      named (2026-10-04: 6 statements at most, `src/Particle/BOOT.md`; the export of the
      fix-up's commits, 1,361 files, 0 hashes, 0 names, 0 code windows, 0 statements,
      `reference: 1444 statements, 6626 fragments, 348 short statements, 392970 code
      windows`).

  ⚠ 2026-10-04: was "at most eight statements in any one file, the scan's own count",
  ticked on 6 (`src/Random/OriginalSeeds.cs`, the export of 529e259), which counted only
  the 360 of 1,154 code statements that have a 25-character fragment: 40 planted
  statements of 15 to 24 characters passed with exit 0, and `src/Statistics/BOOT.md`
  quoted six more whole in backticks, 12 in all against 6 seen. Found by the arbiter's
  review of a579125. Now: 765 of the 1,154 are seen (`## Constraints`, "A measured
  baseline"), the count is a lower bound of rule 2 and says so, and `src/Statistics`
  cites those lines by number.
- [x] `tests/Protocol.Tests`' `Tree.Skipped` no longer lists `legacy`, which skips this
      node as well as the root's archive directory that S2 removes, so the node is
      walked, and `src/Statistics`, `src/Particle`, `src/Output`,
      `tests/Fixtures.Tests`, `tests/Statistics.Tests` and `tests/Benchmarks` declare it
      by a link under `## Dependencies`, their prose mention replaced, with
      `DependencyTests` green (2026-10-04: `Tree.cs`, the four links,
      `DependencyTests.EveryNodeDeclaresTheNeighboursItUsesAndNoOther` green, 23 cases
      of `Protocol.Tests`; `src/Particle` and `src/Output`, whose generator scripts
      import `legacy`, added by the fix-up, the 23 green again).
- [x] The category's rule holds in both directions, counted by the test runner and not
      typed here: the fast set run with `PROPSTRUCT_LEGACY_DIR` unset and the five files
      absent is green, so no fact outside the category reads the original; and the
      category run with the variable unset is red on every one of its facts, passed
      count 0 (2026-10-04: the fast set, `Category!=Long&Category!=Legacy`, 11 projects,
      2,535 cases passed, 0 failed; `Category=Legacy` with the variable unset, 19 facts:
      `Fixtures.Tests` 0 of 4, `Particle.Tests` 0 of 1, `Output.Tests` 0 of 2,
      `Statistics.Tests` 0 of 12, passed 0; with it set, `dotnet test PropStruct.sln -c
      Release --filter Category=Legacy`, the 19 are green and nothing was skipped:
      `Fixtures.Tests` 4 in 17 s, `Particle.Tests` 1, `Output.Tests` 2,
      `Statistics.Tests` 12 in 11 m 37 s, the oracle's `verify` among them, on the
      reference machine with the scans of the fix-up running beside it).

## Taboos

- No byte, line or statement of the original in this node, its self-tests included:
  the self-tests use synthetic content.
- No default for the directory, no search for the files in the tree, no `Skip`, no
  `Assert.Inconclusive` and no early return in a `Legacy` fact when the original is
  missing.
- No hash of the original typed outside `original.sha256`, its specification above
  (`## Manifest`) and the generated `forbidden.sha256`.
- No finding that prints the matched text or bytes.
- No scan that is allowed to be run only on the private tree: the full scan runs on
  the export, and `--hashes` on any tree without the original.
- No network and no package outside the standard library.
