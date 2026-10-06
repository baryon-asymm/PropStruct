# ACCEPTANCE.md — .github

## Acceptance criteria

- [ ] `actionlint` is green locally on every workflow and action, with the label `gpu`
      declared in `actionlint.yaml`; and red on a planted undefined `needs` (date, the
      version of actionlint). Not met in S4, 2026-10-04: actionlint is not installed on
      the reference machine and was not downloaded; the YAML of both workflows, the
      three actions and `actionlint.yaml` parses, every `needs` and every local action
      resolves, every `run` step of an action names its shell. The run is a step of
      the owner's setup before the rehearsal.
- [x] 2026-10-06 — `ci.yml` ran once on GitHub on the commit that introduced it, green,
      with the fast set `Category!=Long&Category!=Legacy` on `windows-latest` and
      `PROPSTRUCT_LEGACY_DIR` unset, and the facts that the hosted CPU moved, if any,
      reclassified as the Constraints say. Run 37412082320 on `b76d3a3`, push of the
      snapshot, about 4.5 minutes, every step green; no fact was moved by the hosted CPU
      (none reclassified: 0 of the bit snapshots changed on that run).
- [ ] The package-content check and the sample and tool steps of `ci.yml` are red once
      on a mutation and right once on the real packages: a project of `src/` left
      out of the merge, a dropped `.xml`, a second dependency (date, run ids).
- [x] 2026-10-05 — `scripts/check_release.py` is red on a tag that differs from the
      version, a missing `CHANGELOG.md` section, an "unreleased" one on a tag, a tag
      message without `Rehearsal:` or `Legacy:`, a Legacy line for another commit or
      with a failing count, a lightweight tag, a run that is not a completed dispatch of
      `Release` that succeeded, a head commit that is not the tagged one, and right on
      the files of a good release (`scripts/test_check_release.py`, 40 cases; with each
      of seven conditions of the script disabled in turn, 1 to 7 of the first 30 fail);
      red on a placeholder of the attribution in `NOTICE`, `README.md` or a packed
      README, one problem per file and line, found from the projects' own items, and
      right on a tree without them (the same file; with the call, the packed READMEs or
      `<PERMISSION>` disabled in turn, 1, 2 and 3 of the 40 fail; all attribution sites filled,
      NOTICE carrying no statement about the data, by the owner's decision of 2026-10-05;
      the tree's own case asserts the check green on the real tree). The shell of the
      `check` job was run on a local repository with an annotated tag and a bare origin:
      `message`, `notes` and `run` exit 0 there.
- [x] 2026-10-04 — The preflight names a missing item, red once on each: no
      `libdevice.10.bc`, no `nvvm64_40_0.dll`, `PROPSTRUCT_NO_CUDA` set,
      `PROPSTRUCT_LEGACY_DIR` set, a foreign `Runner.Listener` running (a real process
      of that name from another directory), a `dotnet` process among the GPU's compute
      applications, no `nvidia-smi`, no git, a Python older than 3.8, an SDK that is not
      installed, no `RUNNER_TEMP`; right on a prepared machine, with the runner's own
      listener running, with graphics processes among the compute applications, and
      under the exit-code wrapper of a composite action, and when the job's own listener,
      an ancestor of the script, runs from another directory (`scripts/test_preflight.py`,
      19 cases, Windows PowerShell 5.1). The missing `nvidia-smi` case found a defect: a
      command not found leaves `$LASTEXITCODE` at 0 and the script had passed; fixed.
- [ ] The runner account is isolated (stage S7): verified as that account, listing the
      legacy directory, the repositories and every bundle or clone is denied, and the
      owner's own access holds (date; the output of the listing). Not met, by the owner's
      decision of 2026-10-06: the runner runs under the owner's account (`BOOT.md`,
      declared deviation); the criterion waits for the isolated account.
- [ ] The preflight is right on the registered runner, on the rehearsal's GPU job (stage
      S7; date, run id). On the reference machine's own environment, with no runner
      up, it passed on 2026-10-04.
- [ ] A dispatch rehearsal of `release.yml` is green through `pack`, its GPU job on the
      self-hosted runner with `propstruct devices` naming the CUDA device and
      `PROPSTRUCT_REQUIRE_CUDA=1`, and the timeout of the job is set from its duration
      (date, run id, commit, the duration).
- [ ] The release 0.1.0 ran as the root's criterion describes: tag, `publish` after the
      owner's approval, `github-release` (date, run id, nuget.org versions).
