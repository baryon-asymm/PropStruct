# CLAUDE.md

Loader. This repository is run by the document-tree protocol; the protocol and the
tree root are imported below and enter the context of every session.

@AGENTS.md
@BOOT.md

⚠ No claims about the system live here and none may be added (`AGENTS.md`, §2).
Everything about the system lives in the `BOOT.md` and `API.md` of the nodes: a second
source of truth drifts from the first sooner or later, and the tree cannot correct it.

## Before any work

Start procedure: `AGENTS.md`, §10. Read the chain of `BOOT.md` from the task's node up
to the root, the `API.md` of the neighbours listed in that node's `## Dependencies`,
decide the mode (design or coding), run the linter before and after the work.

## Commands

- Protocol lint: `python -X utf8 tools/protocol-lint/protocol_lint.py . --exclude templates`
  (`-X utf8` avoids a cp1252 crash on Windows consoles; `--exclude templates` keeps the
  document templates under `docs/protocol/templates/` from being read as nodes).
- Defect report check: `python -X utf8 tools/defect-report/defect_report.py . --check`
  (fails if `docs/ORIGINAL-DEFECTS.md` is stale against the nodes' own `## Defects of
  the original` tables; drop `--check` to regenerate it).
- Build: `dotnet build PropStruct.sln`
- Tests, fast set, as CI runs it: `dotnet test PropStruct.sln --filter
  "Category!=Long&Category!=Legacy"`
- Tests, `Legacy` set (needs the environment variable `PROPSTRUCT_LEGACY_DIR`, whose
  directory holds the original's five files): `dotnet test PropStruct.sln -c Release
  --filter "Category=Legacy"`
- Tests, full set (needs the original as well; red without it): `dotnet test
  PropStruct.sln`
- Tests, the release's GPU job: `dotnet test PropStruct.sln -c Release --filter
  "Category!=Legacy"` with `PROPSTRUCT_REQUIRE_CUDA=1`
- Scan for the original's text and bytes, from the delivery's stage S2 (`tools/legacy`):
  `python -X utf8 tools/legacy/scan.py . --hashes`

## Repository

- Branch `main`, Conventional Commits.
- English in every document, identifier, comment and commit message, the protocol kit
  (`AGENTS.md`, `docs/protocol/`, `tools/protocol-lint/`) included.
- Nothing here may be published on behalf of the owner without being asked.
