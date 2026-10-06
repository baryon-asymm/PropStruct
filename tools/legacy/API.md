# API.md — legacy

The node exposes two command lines and one Python module, and holds two data files.
Designed on 2026-10-04 and built the same day by stage S2 of the delivery; everything
else the node holds is internal and may change.

## Files ✅

| File | What it is |
|---|---|
| `original.sha256` | the manifest: `<hex sha256>  <name>` for the five files of the original (`BOOT.md`, "## Manifest") |
| `forbidden.sha256` | generated: `<hex sha256>  <label>`, the five files by manifest name, then every non-data member of the archive as `archive:<member>`; a hash once, the hash of empty content never; read by `scan.py` |

## legacy.py ✅

```console
$ python legacy.py path <name>        # the absolute path of one of the five files, verified
$ python legacy.py check              # every one of the five files against original.sha256
$ python legacy.py forbidden [--check]    # write forbidden.sha256 from the archive; --check compares only
$ python legacy.py selftest           # the reader and the mismatch path on synthetic files
```

| Command | Output | Exit code |
|---|---|---|
| `path <name>` | the path on one line | 0; 2 the variable unset; 1 the hash differs from the manifest; 2 a name that is not one of the five |
| `check` | one line per file, `ok <name>` or `mismatch <name> recorded <hash> actual <hash>` | 0 all five match; 1 any mismatch or missing file; 2 the variable unset |
| `forbidden` | the number of lines written | 0; 2 the variable unset; 1 the archive does not match the manifest |
| `forbidden --check` | nothing when equal, else a one-line diff | 0 equal; 1 differs; 2 the variable unset |
| `selftest` | one line per case | 0 all cases pass; 1 a case fails; needs no variable |

`<name>` is a manifest name: `PropStructV3.zip`, `PropStructV3.exe`, `dforrt.dll`,
`PropStructv3.for.txt` or `PropStructV3.cycle-plane.listing.txt`.

## scan.py ✅

```console
$ python scan.py <tree> [--hashes]
<kind> <file>[:<line>] [offset <o>] [source line <n>]
scan: <n> files; forbidden hashes <h>, names <m>, code windows <w>, statements <s>
most statements in one file: <k> (<file>)
reference: <s> statements, <f> fragments, <t> short statements, <w> code windows
```

The finding lines come first, the counts after; the last two lines are the full scan's
only (`--hashes` prints `-` for the code windows and the statements of the first).

| Argument | Meaning |
|---|---|
| `<tree>` | the directory to scan, normally the export of the public tree |
| `--hashes` | the hash and name checks only, from `forbidden.sha256` and the extension list; needs no original |

`<kind>` is `hash`, `name`, `bytes` or `statement`. A `statement` finding names the file
and line of the quotation and the source line number it matches (one finding per
statement of a file that touches more than eight), a `bytes` finding the file and the
offset of the matched window in the file or, for a listing's byte columns and a
hexadecimal run, in the bytes they decode to, a `hash` or `name` finding the file; none
prints the matched text or bytes. Exit code 0: no finding; 1: at least one finding;
2: invalid arguments, or, without `--hashes`, the variable unset or a file of the
original that does not match the manifest.

## Module ✅

```python
class LegacyUnset(Exception): ...                # PROPSTRUCT_LEGACY_DIR is not set
class LegacyMismatch(Exception):
    name: str        # the manifest name
    recorded: str    # the hash in original.sha256
    actual: str      # the hash of the file found, or "missing"

MANIFEST_NAMES: "tuple[str, ...]"                # the five manifest names, in manifest order
def manifest() -> "dict[str, str]": ...          # name -> recorded SHA-256 hex
def legacy_dir() -> str: ...                     # the directory; raises LegacyUnset
def legacy_file(name: str) -> str: ...           # the verified path, checked once per process; raises LegacyUnset or LegacyMismatch
def forbidden_hashes() -> "frozenset[str]": ...  # the hashes of forbidden.sha256
```

`legacy_file` raises `KeyError` for a name outside `MANIFEST_NAMES`. A script that wants
the exit codes of the commands catches the two exceptions; one that does not ends with
the exception's message, which names the variable or the file and both hashes.

Read by the scripts of [Fixtures](../../tests/Fixtures/API.md),
[Statistics](../../src/Statistics/API.md), [Particle](../../src/Particle/API.md) and
[Output](../../src/Output/API.md), whose generators and checks need the original, and
run as a command by `tests/Fixtures.Tests`' gate and by `tests/Benchmarks`' runner.
`legacy_file` is lazy: it reads the variable and the file at the call, not at import.

## The `Legacy` test category ✅

`Category=Legacy` marks a fact that reads one of the five files, directly or through a
script. The rule, the gate and the release evidence are `BOOT.md`'s ("The `Legacy`
category", "The gate"); the membership of a test node is that node's `BOOT.md`. The
fast set's filter is `Category!=Long&Category!=Legacy`, the category's own
`Category=Legacy`.
