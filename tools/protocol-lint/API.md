# API.md — protocol-lint

The node exposes a command line and one Python module. Everything else is internal
and may change.

## Command line ✅

```console
$ python protocol_lint.py <tree-root> [--ext .sh,.ps1] [--exclude vendor]
                          [--strict] [--no-heuristics] [--list-nodes]
ERROR 1     src/Orders/API.md: a source directory is a node and a node carries both documents; this one is missing
WARN  7     src/Orders/API.md:12: declares Ghost under a tick, and no source file of this node mentions it
ERROR 15    src/Orders/BOOT.md: 512 non-blank lines, over the 400-line limit for a leaf node; move what is no longer current truth to HISTORY.md, oldest superseded material first, or declare the deviation (AGENTS.md §12)
ERROR 15    src/Orders/BOOT.md:37: points to HISTORY.md#ghost-anchor, which no anchor in src/Orders/HISTORY.md defines
protocol_lint: 3 errors, 1 warnings
```

| Argument | Meaning |
|---|---|
| `<tree-root>` | the tree root: the directory holding `AGENTS.md` |
| `--ext` | additional source extensions, comma-separated |
| `--exclude` | additional directory names to skip |
| `--strict` | warnings also produce a non-zero exit code |
| `--no-heuristics` | skip the textual check of names under ✅ |
| `--list-nodes` | print the nodes found and exit |

The `AGENTS.md` §15 line-limit check and the `HISTORY.md` citation check both run on
every invocation, unconditionally (there is no flag to silence either; `BOOT.md`,
"Constraints", records why the size check briefly shipped behind one). The size check
adds one finding per `BOOT.md` over its limit: an `ERROR` naming the line count and the
limit, or, when the node's own `BOOT.md` carries a line starting
`⚠ Declared deviation, §15:`, a `WARN` that quotes that line so the exemption stays
visible in the output, not only in the document. A node inside its limit produces no
finding either way.

`ACCEPTANCE.md` (`AGENTS.md` 3.2) is checked wherever a `BOOT.md`'s own
`## Acceptance criteria` is the one line `→ [ACCEPTANCE.md](ACCEPTANCE.md)`, in any
node, a leaf included: the pointer without the file, and the file without the
pointer, are each an `ERROR`; a ticked criterion in it carries a date, read over the
whole file whether or not it carries the `## Acceptance criteria` heading; and the
file is itself held to 400 lines, an overflow downgraded to a `WARN` quoting the
node's own `⚠ Declared deviation, §15:` line, exactly as a `BOOT.md`'s overflow is.
Only the root's own `BOOT.md` limit rises, from 250 to 400, once it uses the pointer;
no other node's `BOOT.md` limit changes by using it.

⚠ 2026-10-01: was "A node with children whose own `BOOT.md` ... is measured against
the leaf's 400 lines instead of the parent's 250", which the code never did and
`AGENTS.md` 3.1 itself never asked for: only the root's limit ever rose
(`is_root and root_pointer` in `check_boot_size`), the root being "the one common
ancestor of what that frame binds" (`AGENTS.md` §15); a node with children below the
root kept its 250-line limit with the pointer throughout
(`test_a_node_below_the_root_keeps_the_parent_limit_with_the_pointer`). Corrected
together with the `AGENTS.md` 3.2 change this node adopted the same day: any node, not
only one with children, may hold `ACCEPTANCE.md`, and its own overflow now shares
`BOOT.md`'s deviation instead of having none.

A node whose own `BOOT.md` carries a line `⚠ Declared deviation, §6: … replaced by:
## Section, ## Other section` (`AGENTS.md` §15's own exemption for a transcription
node) is measured with those named sections' non-blank lines excluded from the count,
once each is confirmed to exist as a real `##` heading in the same document; naming a
section that does not exist is an `ERROR` on its own, independent of whether the
document is over its limit.

Independent of the size check, every `HISTORY.md#<anchor>` citation is resolved,
wherever it is written - a `BOOT.md`, an `API.md`, `ACCEPTANCE.md` or any other
document, or a comment in the code under `src/` or `tests/` - and however it is
written, inside backticks or out of them (a citation inside a `HISTORY.md` itself is
not checked). A **bare** citation (`HISTORY.md#<anchor>`) is resolved against the citing
file's own node or one of that node's ancestors; a **qualified** one
(`tests/Harness/HISTORY.md#<anchor>`, or a `../`-relative path) against the node it
names. A missing anchor, a missing `HISTORY.md` entirely, and a bare citation of a
neighbour's anchor are all an `ERROR` naming the anchor. The one string this never
resolves is the kit's own placeholder, `` `HISTORY.md#<anchor>` ``: an anchor never
starts with `<`, so it is not a citation to begin with, in or out of backticks.

Exit code: `0` — no errors, `1` — there are errors (with `--strict`, warnings too),
`2` — invocation error. Directories starting with a dot, except `.github`, and the
usual build directories are always skipped; `.github` is read as part of the tree
(2026-10-01, adopted with the module from the sibling project): it holds committed
configuration, and a directory under it with a manifest or code is a node like any
other, needing its own pair of documents.

## Python module ✅

```python
def lint(root, extra_extensions=(), extra_excluded=(), heuristics=True): ...
def main(argv=None): ...

class Finding:                 # level: ERROR | WARN, article, where, message
    ...
```

`lint` returns the list of findings ordered by location; `main` returns the exit code.
Neither writes to disk. For a project wiring the linter into its own test set the entry
point is `lint`: findings are easier to assert on than to parse from the text output.

## What this node does not do

- it does not check the code against the documents: those are the reflection checks,
  written for the project's stack (`AGENTS.md`, §13);
- it does not check whether the document tells the truth;
- it does not edit documents or code.
