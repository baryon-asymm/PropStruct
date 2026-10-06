# BOOT.md — .github

## Purpose

The repository's continuous-integration and release configuration and the scripts that
configuration runs: the workflows `ci.yml` and `release.yml`, the composite actions they
share under `actions/`, and the scripts under `scripts/`. It gives the tree the proof, on
every push and pull request, that a commit builds and passes the fast set on a
GitHub-hosted Windows runner without CUDA, and, on a tag, the path from the tested
commit through the one GPU run of the release to the two packages on nuget.org. It holds
no product code and no formula: a step calls the tree's own projects, scripts and tools.
Its contract is the list of workflows, jobs, actions and scripts in [API.md](API.md).

The node follows the delivery decided on 2026-10-04 (root `BOOT.md`, `## Delivery`) and
is modelled on the `.github` of APThermo (github.com/baryon-asymm/APThermo). Stage S3
built `scripts/check_packages.py` and stage S4 the workflows, the actions and the other
scripts, all on 2026-10-04 and tested offline; what only GitHub can prove, a run of each
workflow, is stages S6 and S7 and stands in the acceptance criteria below unticked.
The scripts have a child node of their own ([scripts](scripts/BOOT.md)).

## Invariants

- **Self-hosted jobs run only on a tag or a manual dispatch**, never on a pull request
  or a push to a branch, and the runner is not a service. It runs under an account
  without administrator rights, is started for a release and stopped after it, and that
  account has no access to the directory `PROPSTRUCT_LEGACY_DIR` names, so that a job
  compromised through a dependency cannot read the original. The repository's setting
  "require approval for all outside collaborators" for fork pull-request workflows is
  on; the owner sets it before the first push (stage S6 of the delivery).

  ⚠ 2026-10-04: the isolation of that account is not the machine's default: on the
  reference machine `C:\Projects` and its subdirectories inherit `BUILTIN\Users:(RX)`
  and `Authenticated Users:(M)`, so any account reads the original. The owner's step 4
  below makes it true and verifies it as the runner account; until then the criterion
  stands unticked and the runner is not registered.

  ⚠ Declared deviation, §12 (2026-10-06, owner): the runner is registered and runs under
  the owner's own account, with administrator rights and access to the original. Was an
  account without either, the isolation of step 4, now none: the owner weighed the risk
  and takes it. What stays: the runner is no service, takes only tag and dispatch runs,
  fork pull requests need approval, and only the owner's code reaches it. What lifts the
  deviation: the isolated account of step 4, verified as that account.
- **One GPU job on the machine at a time, and never with APThermo's.** A personal
  account registers a runner per repository, so one runner cannot serve both. The
  rule is the owner's discipline, runners started only for a release with the other
  repository's runner stopped, and a check that fails fast when it is broken: the
  preflight fails when a `Runner.Listener` process outside this runner's own directory,
  and not an ancestor of the preflight's own process, is running or a `dotnet`,
  `testhost`, `python` or `propstruct` process is among the GPU's compute applications.

  ⚠ 2026-10-04: was "a compute process holds the GPU", now those four names →
  HISTORY.md#preflight-compute-names-2026-10-04
- **What a self-hosted runner must provide**, checked by the preflight step, which
  names every missing item: git; the .NET SDK of `global.json`; Python 3.8 or newer;
  an NVIDIA driver (`nvidia-smi`); `nvvm64_40_0.dll` and `libdevice.10.bc` where
  `src/Execution`'s discovery looks (`CUDA_PATH` and the toolkit directories);
  `PROPSTRUCT_NO_CUDA` unset, since it would refuse the CUDA the job exists to run;
  `PROPSTRUCT_LEGACY_DIR` unset, since no `Legacy` fact runs on GitHub; no runner of
  another repository; no compute process of the kind above on the GPU. The SDK is
  accepted as `global.json`'s `rollForward: latestPatch` resolves it. A step of a
  self-hosted job names its shell, `powershell`.
- **CUDA runs only in the release, and a CUDA fact that finds CUDA refused fails.** The
  GPU job sets `PROPSTRUCT_REQUIRE_CUDA=1`, which `Harness` turns, for a fact that
  would otherwise return when no accelerator is available, into a failure; the job
  also logs `propstruct devices`, so the log shows the device the facts ran on.
- **Evidence for workflow changes.** A change under `.github/` runs only on GitHub, so
  it is accepted on a run of the path it changes, on the runner class it targets: a CI
  run for `ci.yml`, a dispatch run for `release.yml`. A review or `actionlint` is not
  enough; `actionlint` runs locally first.
- **No second implementation of a check.** A step calls the tree's own command or script
  (the protocol lint, `defect_report.py --check`, `scan.py --hashes`, the tests) and
  does not restate what it checks; the one comparison the packed tool's example needs is
  `Harness`'s time-line cut, referenced.
- **No `Legacy` fact on GitHub.** No workflow sets `PROPSTRUCT_LEGACY_DIR`, and every
  test step filters `Category!=Legacy` (`tools/legacy`).
- **One version.** The tag `v<version>` equals `VersionPrefix` of `Directory.Build.props`,
  and `CHANGELOG.md` has a section for it.

## Dependencies

- [protocol-lint](../tools/protocol-lint/API.md) — the lint command `ci.yml` runs first.
- [defect-report](../tools/defect-report/API.md) — `defect_report.py --check`.
- [legacy](../tools/legacy/API.md) — `scan.py --hashes` on every CI run and the
  self-test of `legacy.py`; the `Legacy` line of the release tag.
- [Harness](../tests/Harness/API.md) — the time-line cut of `results.m` that the scratch
  program of the packed tool's example step references, and the helper behind
  `PROPSTRUCT_REQUIRE_CUDA`.
- [Output](../src/Output/API.md) and [Cli](../src/Cli/API.md) — the two projects the
  workflows pack.
- [Quickstart](../samples/Quickstart/API.md) — the sample built against the packed
  library.
- [Fixtures](../tests/Fixtures/API.md) — `Legacy/formulations/inpt.dat`, the input of the
  packed tool's example, read from the tree.

Outside the tree: GitHub Actions with `actions/checkout`, `actions/setup-dotnet`,
`actions/setup-python`, `actions/upload-artifact`, `actions/download-artifact` and
`NuGet/login`, at the major versions S4 pins; the hosted runners `windows-latest` and
`ubuntu-latest`; one self-hosted runner of the reference machine (Windows x64, RTX 5070
Ti); nuget.org with Trusted Publishing; the .NET SDK of `global.json`; Python 3.8 or
newer; the `gh` command of the hosted `ubuntu-latest`; actionlint, whose label `gpu` is
declared in `actionlint.yaml`.

## Constraints

Inherited from the parent ([BOOT.md](../BOOT.md)): the rules of the root bind every
workflow here, nothing is pushed to GitHub or nuget.org without the owner's word, each
time, and `.claude/` is not published. In addition:

- **Continuous integration** (`ci.yml`; every push, every pull request, manual
  dispatch). One job, `build`, on `windows-latest` with `PROPSTRUCT_NO_CUDA=1` and a
  job-local `NUGET_PACKAGES`, in this order:
  1. checkout; `actions/setup-dotnet` with `global.json`; the CPU model and core count
     printed by `actions/runner-diagnostics`; `actions/setup-python`;
  2. `actions/hosted-checks`, the one place the release's hosted job shares: the
     protocol lint; the tool self-tests (every `tools/*/test_*.py` and `legacy.py
     selftest`); `defect_report.py --check`; `scan.py --hashes`; the Release build; the
     script self-tests (every `.github/scripts/test_*.py`, which need the build: the
     packed tool's comparison is `Harness`'s); the fast set as the root runs it,
     `--filter "Category!=Long&Category!=Legacy"`; on failure the `*.actual.*` and
     `*.received.*` files the bit snapshots write are uploaded;
  3. `scripts/check_packages.py all`: packing both packages with
     `ContinuousIntegrationBuild` on the command line only; the package-content check:
     `PropStruct`'s `lib/net10.0` holds the assembly and its `.xml` for every project of
     `src/` but `Cli`, its `.snupkg` the PDBs, and ILGPU, exactly 1.5.3, is its only
     dependency; the tool holds ILGPU's licence and says `MIT AND NCSA`; red on every
     mutation of the script's list; `samples/Quickstart` built against the packed
     `PropStruct` from a job-local feed and run; the tool installed from that feed, `propstruct --version`,
     and the approved example, `propstruct run inpt.dat --mode reference --accelerator
     cpu --layout original --seed 0`, whose `results.m` equals byte for byte, time line
     cut by `Harness`'s `ResultsMTimeLine`, the one the source-built tool writes on the
     same runner, so no hosted CPU is held to the reference machine's;
  4. the packages are uploaded as an artifact.

  The SourceLink test of the package, on a push only, is not built: it needs the public
  commit and is stage S6's.

  Whether a hosted CPU moves a bit snapshot is the first run's measurement: APThermo
  measured a hosted CPU changing bits, the CRT choosing FMA3 or plain `exp`,
  `log` and `pow` by CPU, and the draws here go through `Log`, `Pow`, `Acos`, `Sin` and
  `Tan`. A fact that fails there on the CPU alone becomes
  `Category=BitSnapshot`, excluded from CI and run by the release's GPU job on the
  reference machine, with a deviation declared in the root's Platform constraint; it
  loosens nothing. The timeout of 90 minutes is provisional, set before any hosted run;
  the local fast set takes 42 seconds, the packages 29.
- **Release** (`release.yml`; a tag `v*`, manual dispatch), in order: `check`
  (`ubuntu-latest`); `hosted` (`windows-latest`, `actions/hosted-checks`); `gpu`
  (`[self-hosted, windows, gpu]`, a timeout of 240 minutes, provisional until the
  rehearsal measures the duration: the preflight, the Release build, `propstruct
  devices`, the tests with `--filter "Category!=Legacy"` and
  `PROPSTRUCT_REQUIRE_CUDA=1`; no `setup-dotnet`, since the preflight demands the SDK on
  the machine); `pack` (`windows-latest`: `check_packages.py pack` and `verify`);
  `publish` (`ubuntu-latest`, nuget.org through Trusted Publishing, the `.nupkg` files,
  each `.snupkg` going beside its own, `--skip-duplicate`, environment `release`,
  `id-token: write`, a push only);
  `github-release` (notes of `CHANGELOG.md`, the packages, `--verify-tag`, a push only).
- **The release check** (`scripts/check_release.py`, three commands that read files and
  are tested offline, `scripts/API.md`): `notes` on every run, the section of
  `CHANGELOG.md` for the version of `Directory.Build.props` is non-empty, and on a push
  the tag equals `v<version>` and the section is dated, not "unreleased"; `message`, on
  a push, the tag is annotated and its message carries exactly one `Rehearsal: <run id>`
  and one `Legacy: <commit sha> <date> <passed>/<total>`, the sha the tagged commit and
  `passed` equal to a `total` above zero; `run`, on a push, the `gh run view` JSON of
  the rehearsal run says status completed, conclusion success, event
  `workflow_dispatch`, workflow `Release`, head commit the tagged commit. `notes`
  also refuses, on every run and before `pack`, a placeholder of the owner's attribution
  (`<AUTHORS>`, `<AUTHORS, AFFILIATION>`, `<PERMISSION>`) in `NOTICE`, `README.md` or a
  file a project of `src/` packs as its `README.md`, so that none reaches nuget.org:
  **green on this tree: all attribution sites filled, `NOTICE` carrying no statement
  about the data, by the owner's decision of 2026-10-05** (root `ACCEPTANCE.md`). The tag message of 0.1.0 is therefore, whatever its first line:

  ```text
  PropStruct 0.1.0

  Rehearsal: 123456789
  Legacy: 0123456789abcdef0123456789abcdef01234567 2026-10-30 412/412
  ```
- **Rehearsal before the tag.** A dispatch of `release.yml` runs `check` to `pack` and
  never `publish` or `github-release`. A tag is pushed only on a commit whose dispatch
  run is green through packing, the tag message names that run, and a tag is not moved
  once pushed: a failure after the tag is fixed on a new commit, rehearsed, and
  released under the next patch version.
- **nuget.org** (the owner's, stage S6 of the delivery): a Trusted Publishing policy
  naming the repository `PropStruct`, the workflow `release.yml` and the environment
  `release`; the environment requires the owner's approval and admits tags `v*` only;
  the secret `NUGET_USER` names the account. No API key exists.
- **No nightly run.** Everything long runs in the release.

## Setup by the owner

What no workflow can do for itself, in the order it is needed. Nothing here is done by
an agent, and nothing is pushed, tagged or published without the owner's word (root
`BOOT.md`, "Repository").

Stage S6, before the first push:

1. Create the empty public repository `baryon-asymm/PropStruct`, without a README, a
   licence or a `.gitignore`: the first push is the one snapshot commit.
2. In Settings, Actions, General, "Fork pull request workflows from outside
   collaborators": **Require approval for all outside collaborators**, possibly labelled
   "Require approval for all external contributors". Set it before the first push, so
   that no pull request runs a workflow unapproved; never approve a run of a fork that
   touches `.github/`.
3. In Settings, Actions, General, "Workflow permissions": read repository contents only;
   the workflows ask for more where they need it.

Stage S7, before the first rehearsal:

4. **The runner**, at repository level (Settings, Actions, Runners), not organisation or
   account level, since a personal account has none of those: register it on the
   reference machine under a Windows account without administrator rights and without
   access to the directory `PROPSTRUCT_LEGACY_DIR` names, with the labels `self-hosted`,
   `windows` and `gpu`. Do not install it as a service: start `run.cmd` for a release
   and stop it after. Install the .NET SDK of `global.json`, Python 3.8 or newer, git,
   the NVIDIA driver and a CUDA Toolkit 12.8 or newer for that account; leave
   `PROPSTRUCT_NO_CUDA` and `PROPSTRUCT_LEGACY_DIR` unset in its environment. Before
   starting it, cancel any queued run waiting for the self-hosted labels.

   **Isolate the account first**, since it is not the default: remove the inheritance
   on `C:\Projects`, or at least on the legacy directory, the repositories and every
   bundle or clone, and grant only the owner, SYSTEM and Administrators, for example
   `icacls C:\Projects /inheritance:r /grant:r <owner>:(OI)(CI)F SYSTEM:(OI)(CI)F
   Administrators:(OI)(CI)F` from an elevated prompt, then verify as the runner account
   that listing the legacy directory is denied, and that the owner still reaches it.
5. **The environment `release`** (Settings, Environments): the owner as required
   reviewer; the deployment rule a tag rule, `v*`; the secret
   `NUGET_USER`, the nuget.org account name, stored in this environment so that only the
   `publish` job reads it. No API key is created anywhere. The workflows pin the actions
   by major version, as APThermo's do, not by commit; pinning `NuGet/login` and
   `download-artifact` by SHA, the publish job's two, is the owner's choice.
6. **nuget.org Trusted Publishing**: a policy for the owner's account naming the
   repository owner `baryon-asymm`, the repository `PropStruct`, the workflow file
   `release.yml` and the environment `release`. If nuget.org wants the package ids to
   exist before it accepts a policy, reserving `PropStruct` and `PropStruct.Cli` first
   is the owner's decision at that stage.
7. Run `actionlint` on `.github` locally, then dispatch `release.yml` as the rehearsal.

**Coordination with APThermo.** Both repositories use the one GPU of the reference
machine and a personal account registers a runner per repository, so each has its own.
The rule: only one runner is started at a time, for a release, with the other
repository's runner stopped first, and a session that wants the GPU asks whether a
release is running. The preflight is the net under the rule: it fails the job at its
first step when a runner of another repository or a compute process of the tree's or
APThermo's kind is up, naming it, so that two jobs never share the GPU unnoticed.

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Taboos

- No product code and no formula in a workflow, an action or a script.
- No push to nuget.org outside the `publish` job, which waits for the owner's approval of
  the `release` environment, and no tag moved once pushed.
- No pull request's code on a self-hosted runner, and no assumption of PowerShell 7 or
  Git Bash there.
- No `Legacy` fact and no `PROPSTRUCT_LEGACY_DIR` on GitHub, hosted or self-hosted.
- No API key for nuget.org, stored or typed.
- No analyzer or workflow-lint suppression.
