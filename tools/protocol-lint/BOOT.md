# BOOT.md — protocol-lint

## Purpose

The language-independent half of the protocol's machine checks (`AGENTS.md`, §13). The
node checks **the document tree against the protocol**, not the code against the
documents: everything for which reading files is enough lives here; everything that
needs reflection over a build is written for the specific stack and lives in the
project's tests node.

The node is placed in the tree as an ordinary node (for example `tools/protocol-lint/`)
and is run before and after work in any node.

## Invariants

- **Standard library of Python 3.8+ only.** The protocol check must run where nothing
  has been installed yet, including on the first commit of a new repository, before a
  package manager has been chosen.
- **Not a single change on disk.** The linter only reads: a check that edits what it
  checks hides the divergence instead of showing it.
- **Every check is named after the article of the protocol** it is based on. A check
  without an article is the linter author's personal opinion about someone else's project.
- **The split between errors and warnings is substantive**: an error is a violation of
  an article, a warning is something that can be legitimate (a textual heuristic, an
  obsolete form). By default only errors fail the run; `--strict` makes warnings fail
  it too.
- **Links and headings inside code do not count**: the examples in `AGENTS.md` are
  written in backticks precisely so that they remain examples.
- **The size check (`AGENTS.md` §15) counts, it does not read.** Whether a BOOT.md is
  over its line limit is arithmetic on the file as written; whether what is left in it
  is still current truth is not something this node can tell, so a node's own
  `⚠ Declared deviation, §15:` line is taken on trust, the same way `## Dependencies`'
  `None` is. The same trust extends to a node's own `⚠ Declared deviation, §6: …
  replaced by: ## Section, …` line: this node checks that each named section actually
  exists before excluding its lines, but not that the node genuinely cannot be
  self-sufficient without it.
- **A citation is checked, not trusted.** `→ HISTORY.md#<anchor>` is the one place the
  size machinery writes something a reader is expected to follow; an anchor that does
  not exist defeats the pointer's only purpose, so this is an error, unconditionally,
  not gated behind anything, and it is checked wherever it is written - in a BOOT.md or
  an API.md, in a comment, in backticks or out of them. The one exception is a
  placeholder exactly like this bullet's own example: an anchor never starts with `<`,
  so `HISTORY.md#<anchor>` cannot itself be mistaken for a citation.
- **`ACCEPTANCE.md` is current truth, not history** (AGENTS.md 3.2). Any node, a leaf
  included, may hold its `## Acceptance criteria` body there instead of in `BOOT.md`,
  leaving the one line `→ [ACCEPTANCE.md](ACCEPTANCE.md)` behind; this node checks
  that the file exists exactly when the pointer does, and that its dates - read over
  the whole file, with or without the section heading - and its own 400-line limit
  hold, the same way `BOOT.md`'s do, the limit's overflow downgraded to a warning by
  the node's own `⚠ Declared deviation, §15:` line exactly as a `BOOT.md`'s is. The
  root's own `BOOT.md` limit rises from 250 to 400 once it uses this, since it is then
  measured for frame alone, not for evidence too (AGENTS.md 15); the pointer exempts
  no other node's `BOOT.md` from its own limit.

  ⚠ 2026-10-01: was "that it is never a leaf's", with no deviation for the file's own
  overflow and its dates read in the `## Acceptance criteria` section only; now any
  node, a deviation shared with `BOOT.md`'s, and the whole file read for dates
  (AGENTS.md 3.2, Appendix D).

## Dependencies

None.

Outside the tree: Python 3.8+ (standard library), `unittest` for the self-test.

## Constraints

- The node knows none of the project's programming languages: the list of source
  extensions is a parameter, not knowledge.
- A false positive costs more than a miss: the linter runs on every commit, and noise
  in it devalues the real findings. That is why the textual heuristic (names under ✅)
  yields a warning, not an error, and is switched off by a flag.
- Output is one line per finding, with path and line number: it is read by a human in
  a terminal and by an agent in a tool's output.
- **The size check ran behind `--size` for one wave, then went unconditional**
  (decided 2026-09-20, wave 7, revised the same wave). `AGENTS.md` §15 was new the day
  the two largest leaves of the tree (`src/Execution`, 719 non-blank lines;
  `src/Statistics`, 417) were already over its 400-line leaf limit, and the tree root
  and `tests/Harness` (1574 lines) were over theirs; `--size` let the tree adopt the
  check without every run of the command in `CLAUDE.md` going red from a known,
  already-being-worked backlog — exactly the "perpetually red check" `AGENTS.md` §13
  forbids. Once the root, `src/Execution` and `src/Statistics` were brought inside
  their limits (the first two by moving ⚠ corrections to their own `HISTORY.md`, the
  third by the §6 section exemption below) and `tests/Harness` carried its own
  declared deviation, the flag itself became the thing standing between the tree and
  the check `AGENTS.md` §15 actually asks for: a flag a caller can forget is not a
  check, so it was removed and the size check now runs unconditionally, the same as
  every other check in this node.

## Acceptance criteria

- [x] Every check is proven non-degenerate: exactly one breakage is applied to a
      conformant tree and the check turns red — 68 tests, none skipped
      (2026-09-12, extended 2026-09-20 for the §15 size check and again 2026-09-20 for
      the §6 section exemption and the HISTORY.md pointer check, 2026-10-01 for
      `ACCEPTANCE.md` in any node; the module and its tests are, since 2026-10-01,
      adopted byte for byte from the sibling project's tree so that one linter serves
      one protocol, `test_protocol_lint.py`, run as
      `python -X utf8 tools/protocol-lint/test_protocol_lint.py`).
- [x] The AGENTS.md §15 size check: a leaf over 400 non-blank lines and a node with
      children over 250 each turn it red, a leaf or a parent within its own limit
      stays clean, a `⚠ Declared deviation, §15:` line downgrades the finding to a
      warning and is named in it, and the check runs unconditionally, with no flag
      left to silence it (2026-09-20, `test_protocol_lint.py`:
      `test_a_leaf_boot_over_its_limit_is_an_error`,
      `test_a_leaf_boot_within_its_limit_is_clean`,
      `test_a_parent_boot_uses_the_tighter_limit`,
      `test_a_leaf_boot_is_not_held_to_the_parent_limit`,
      `test_a_declared_deviation_downgrades_the_finding_to_a_warning`,
      `test_the_size_check_runs_by_default`,
      `test_exit_codes_reflect_the_size_check`; disabling the threshold
      turns exactly these tests red, checked by hand against a scratch copy of the
      module, not committed).
- [x] The AGENTS.md §15 section exemption: a `⚠ Declared deviation, §6: … replaced by:
      ## Section` line excludes that section's non-blank lines from the count once the
      section is confirmed to exist; naming a section that does not exist is an error
      instead of a silent no-op; the exemption narrows a genuine overflow, it does not
      hide one (2026-09-20, `test_protocol_lint.py`:
      `test_a_named_section_is_excluded_from_the_count`,
      `test_a_named_section_that_does_not_exist_is_an_error`,
      `test_a_section_exemption_does_not_hide_a_genuine_overflow`; removing the
      exclusion arithmetic turns the first test red, removing the existence check
      turns the second red, checked by hand against a scratch copy of the module, not
      committed).
- [x] The HISTORY.md citation check: every `HISTORY.md#<anchor>`, wherever it is
      written - a BOOT.md, an API.md, a comment or a string in the code under `src/`
      or `tests/` - and however it is written, in backticks or out of them, resolves;
      a bare one in the citing node's own HISTORY.md or an ancestor's, a qualified one
      (`tests/Harness/HISTORY.md#<anchor>`, or a `../`-relative path) in the node it
      names. A citation inside a HISTORY.md itself is not checked. A missing anchor,
      and a missing HISTORY.md entirely, are both errors; a bare citation that happens
      to name a neighbour's anchor is an error too, never a silent match
      (2026-09-20, extended 2026-09-27 to read backticks and every document and to
      qualified and code-file citations, `test_protocol_lint.py`:
      `test_a_pointer_to_a_missing_anchor_is_an_error`,
      `test_a_pointer_that_resolves_is_not_flagged`,
      `test_a_pointer_with_no_history_file_at_all_is_an_error`,
      `test_a_backticked_dangling_citation_in_api_md_is_an_error`,
      `test_a_backticked_placeholder_citation_is_not_flagged`,
      `test_a_citation_inside_a_history_md_itself_is_not_checked`,
      `test_a_bare_citation_of_a_neighbours_anchor_is_an_error`,
      `test_a_bare_citation_resolves_via_an_ancestor`,
      `test_a_qualified_citation_of_a_neighbour_resolves`,
      `test_a_relative_qualified_citation_resolves`,
      `test_history_citations_are_checked_in_code_under_src_and_tests`,
      `test_history_citations_outside_src_and_tests_are_not_checked_in_code`;
      removing the check turns the first, third, fourth and eighth red, checked by
      hand against a scratch copy of the module, not committed).
- [x] `ACCEPTANCE.md` (AGENTS.md 3.2, 2026-10-01): the file stands beside a
      `## Acceptance criteria` that is the one pointer line, in any node, a leaf
      included; its dates - read over the whole file, with or without the section
      heading - and its own 400-line limit are checked the same way a BOOT.md's are,
      the limit's overflow downgraded to a warning by the node's own declared
      deviation exactly as a BOOT.md's is; the root's own limit rises to 400 once it
      uses the pointer, and no other node's does
      (`test_protocol_lint.py`: `test_a_pointer_with_no_acceptance_file_is_an_error`,
      `test_a_pointer_that_resolves_to_a_real_file_is_clean`,
      `test_an_orphan_acceptance_file_is_an_error`,
      `test_acceptance_md_in_a_leaf_is_clean`,
      `test_a_leaf_moving_its_criteria_out_comes_inside_its_limit`,
      `test_a_declared_deviation_downgrades_an_acceptance_overflow_to_a_warning`,
      `test_an_undated_tick_in_acceptance_md_is_a_warning`,
      `test_an_undated_tick_in_a_headingless_acceptance_md_is_a_warning`,
      `test_a_leaf_with_the_pointer_keeps_the_leaf_limit`,
      `test_acceptance_md_in_a_leaf_over_400_lines_is_an_error`,
      `test_acceptance_md_over_400_lines_is_an_error`,
      `test_a_node_below_the_root_keeps_the_parent_limit_with_the_pointer`,
      `test_a_root_boot_over_400_lines_with_the_pointer_is_an_error`,
      `test_a_root_boot_at_exactly_400_lines_with_the_pointer_is_clean`; removing the
      file/pointer check turns the first three red; on the module as it stood before
      3.2 (the leaf restricted, no deviation for the file, dates read in the section
      only) the four leaf-related tests above are red; exempting a non-root BOOT.md
      from its limit turns `test_a_leaf_with_the_pointer_keeps_the_leaf_limit` and
      `test_a_node_below_the_root_keeps_the_parent_limit_with_the_pointer` red;
      removing the file's size check turns
      `test_acceptance_md_in_a_leaf_over_400_lines_is_an_error` red; each checked by
      hand against a scratch copy of the module, not committed).

      ⚠ 2026-10-01: was "the file stands only ... in a node with children" and "with
      no deviation available" for the file's own overflow, dates read in its section
      only (`test_acceptance_md_in_a_leaf_is_an_error`); now any node, a leaf
      included, the overflow's own deviation, and the whole file read for dates
      (AGENTS.md 3.2, Appendix D).
- [x] Two guard tests against false positives: a grouping directory is not counted as
      a node, a link inside a code block is not resolved
      (2026-09-12, `test_a_grouping_directory_is_not_a_node`,
      `test_links_inside_code_are_not_followed`).
- [x] Verified on a real tree, not only on a fixture: a tree of 52 nodes (the
      PastyPropellant reconstruction) and the tree of the port, both parsed in full,
      and every finding explained (2026-09-12).
- [x] The self-test is run by the project's merge guard, not by a test project: the
      `for selftest in tools/*/test_*.py` loop of `.claude/scripts/merge-guarded.sh`
      runs every tool self-test and fails the merge when one is red
      (2026-10-02, `d8158f0`; proven here by hand with that loop: one assertion of
      `test_a_conforming_tree_is_clean` broken in a scratch edit turned
      `test_protocol_lint.py` red and the loop's exit 1, the revert turned it green
      again). Outside the guard it is still run by nothing but a manual
      `python -m unittest`, and it is not a `dotnet test` project.

      ⚠ 2026-10-02: was the open criterion "the self-test is not run automatically by
      anything except a manual `python -m unittest`: in a project it has to be wired
      into the project's own test set"; now wired into the merge guard, which is the
      project's gate, not a test set. Found by reading `merge-guarded.sh` after
      `d8158f0`.

      ⚠ 2026-10-04: the evidence above lies in `.claude/scripts/merge-guarded.sh`, which
      is not in the published tree (`.claude/` is untracked). The public tree runs the
      same loop as the "Tool self-tests" step of `.github/actions/hosted-checks`, on
      every CI and release run; that step, not the guard, is its gate there, and its
      first run on GitHub is stage S6 of the delivery.
- [ ] The checks that need reflection are not implemented here and cannot be: that is
      a separate node for the project's stack (`AGENTS.md`, §13).

## Taboos

- Do not check the content of documents: whether what is written is true, the machine
  does not know and cannot know.
- Do not add checks without an article of the protocol. A new check needs the article
  first, then the code.
- Do not introduce external dependencies or configuration files: the parameters are flags.
- Do not repair the tree automatically. The linter reports; a human or an agent decides.
