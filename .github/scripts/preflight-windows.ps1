# The preflight of the release's GPU job (.github/BOOT.md, "What a self-hosted runner must
# provide"): asserts what the runner must provide before the job builds or tests anything, and
# names every missing item instead of letting a later step fail on an opaque error. Windows
# PowerShell 5.1: no $IsWindows, no ternary, no `&&`. The working directory is the repository
# root, the job's default, so global.json resolves without $PSScriptRoot.
#
# Exit 0 when every item is present, else 1 with one line per missing item.
#
# -ToolkitBase is the directory whose v* children are the CUDA toolkits; the default is where
# src/Execution's discovery looks. Tests of this script pass a scratch one.
param(
    [string]$ToolkitBase = (Join-Path $env:ProgramFiles 'NVIDIA GPU Computing Toolkit\CUDA')
)

# A composite action runs its step with $ErrorActionPreference = 'Stop', under which a native
# command's output on standard error would end the script; every failure here is collected, not thrown.
$ErrorActionPreference = 'Continue'

$failures = New-Object System.Collections.Generic.List[string]

# The processes of this tree and of APThermo that put CUDA work on the GPU. On a desktop (WDDM)
# nvidia-smi lists every graphics process as a compute application, with its memory [N/A]:
# 25 of them on the reference machine, 2026-10-04, none of them CUDA work. So the list is read
# for these names only.
$gpuHostNames = @('dotnet.exe', 'testhost.exe', 'python.exe', 'propstruct.exe')

# git
if (Get-Command git -ErrorAction SilentlyContinue) {
    Write-Host "git: found ($(git --version))"
}
else {
    $failures.Add('git not found on PATH')
}

# The .NET SDK of global.json, as rollForward latestPatch resolves it: the same major, minor
# and feature band, a patch level equal or higher.
$globalJsonPath = 'global.json'
if (-not (Test-Path $globalJsonPath)) {
    $failures.Add('global.json not found at the repository root')
}
else {
    $requested = (Get-Content $globalJsonPath -Raw | ConvertFrom-Json).sdk.version
    $requestedParts = $requested.Split('.') | ForEach-Object { [int]$_ }
    $sdkLines = $null
    $sdkListed = $false
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        $sdkLines = & dotnet --list-sdks 2>&1
        $sdkListed = ($LASTEXITCODE -eq 0)
    }
    if (-not $sdkListed) {
        $failures.Add("dotnet --list-sdks did not run (not on PATH, or exit code $LASTEXITCODE): $sdkLines")
    }
    else {
        $matching = @($sdkLines | ForEach-Object { ($_ -split ' ')[0] } | Where-Object {
            $parts = $_.Split('.') | ForEach-Object { [int]($_ -replace '[^0-9].*$', '') }
            ($parts[0] -eq $requestedParts[0]) -and ($parts[1] -eq $requestedParts[1]) -and
                ([math]::Floor($parts[2] / 100) -eq [math]::Floor($requestedParts[2] / 100)) -and
                ($parts[2] -ge $requestedParts[2])
        })
        if ($matching.Count -gt 0) {
            Write-Host "dotnet SDK $($matching -join ', '): found for global.json $requested"
        }
        else {
            $failures.Add("dotnet SDK $requested (global.json, rollForward latestPatch) not among the installed SDKs: $($sdkLines -join '; ')")
        }
    }
}

# Python 3.8 or newer: the tests run the tree's generator and verifier scripts.
$pythonOutput = $null
$pythonRuns = $false
if (Get-Command python -ErrorAction SilentlyContinue) {
    $pythonOutput = & python --version 2>&1
    $pythonRuns = ($LASTEXITCODE -eq 0)
}
if (-not $pythonRuns -or ("$pythonOutput" -notmatch 'Python (\d+)\.(\d+)')) {
    $failures.Add("python did not answer 'Python <version>': $pythonOutput")
}
elseif (([int]$Matches[1] -lt 3) -or (([int]$Matches[1] -eq 3) -and ([int]$Matches[2] -lt 8))) {
    $failures.Add("python is $pythonOutput, 3.8 or newer is required")
}
else {
    Write-Host "python: $pythonOutput"
}

# The NVIDIA driver
$smi = $null
$smiRuns = $false
if (Get-Command nvidia-smi -ErrorAction SilentlyContinue) {
    $smi = & nvidia-smi 2>&1
    $smiRuns = ($LASTEXITCODE -eq 0)
}
if (-not $smiRuns) {
    $failures.Add("nvidia-smi did not run (not on PATH, or exit code $LASTEXITCODE): $smi")
}
else {
    Write-Host 'nvidia-smi: runs'
}

# libnvvm and libdevice, where src/Execution's discovery looks: both files named by
# PROPSTRUCT_LIBNVVM_PATH and PROPSTRUCT_LIBDEVICE_PATH when both are set, else a toolkit
# directory (CUDA_PATH, then the v* directories of $ToolkitBase, newest first) holding
# nvvm\bin\nvvm64_40_0.dll or nvvm\bin\x64\nvvm64_40_0.dll together with
# nvvm\libdevice\libdevice.10.bc.
$nvvmFound = $null
$libdeviceFound = $null
$searched = New-Object System.Collections.Generic.List[string]
if ($env:PROPSTRUCT_LIBNVVM_PATH -and $env:PROPSTRUCT_LIBDEVICE_PATH) {
    $searched.Add($env:PROPSTRUCT_LIBNVVM_PATH)
    $searched.Add($env:PROPSTRUCT_LIBDEVICE_PATH)
    if ((Test-Path $env:PROPSTRUCT_LIBNVVM_PATH) -and (Test-Path $env:PROPSTRUCT_LIBDEVICE_PATH)) {
        $nvvmFound = $env:PROPSTRUCT_LIBNVVM_PATH
        $libdeviceFound = $env:PROPSTRUCT_LIBDEVICE_PATH
    }
}
else {
    $toolkitDirs = New-Object System.Collections.Generic.List[string]
    if ($env:CUDA_PATH) {
        $toolkitDirs.Add($env:CUDA_PATH)
    }
    if (Test-Path $ToolkitBase) {
        Get-ChildItem $ToolkitBase -Directory -Filter 'v*' |
            Sort-Object { [version]($_.Name -replace '^v', '') } -Descending |
            ForEach-Object { $toolkitDirs.Add($_.FullName) }
    }
    foreach ($dir in $toolkitDirs) {
        if ($nvvmFound) { break }
        $bitcode = Join-Path $dir 'nvvm\libdevice\libdevice.10.bc'
        foreach ($candidate in @((Join-Path $dir 'nvvm\bin\nvvm64_40_0.dll'), (Join-Path $dir 'nvvm\bin\x64\nvvm64_40_0.dll'))) {
            $searched.Add($candidate)
            if ((Test-Path $candidate) -and (Test-Path $bitcode)) {
                $nvvmFound = $candidate
                $libdeviceFound = $bitcode
                break
            }
        }
    }
}
if ($nvvmFound) {
    Write-Host "libnvvm: found at $nvvmFound"
    Write-Host "libdevice.10.bc: found at $libdeviceFound"
}
else {
    $failures.Add("nvvm64_40_0.dll with libdevice.10.bc not found; examined: $($searched -join '; ')")
}

# PROPSTRUCT_NO_CUDA must be unset, or the CUDA facts would only check the refusal
if ($env:PROPSTRUCT_NO_CUDA) {
    $failures.Add("PROPSTRUCT_NO_CUDA is set ('$($env:PROPSTRUCT_NO_CUDA)'); the CUDA facts would refuse the accelerator instead of running on it")
}
else {
    Write-Host 'PROPSTRUCT_NO_CUDA: unset'
}

# PROPSTRUCT_LEGACY_DIR must be unset: no Legacy fact runs on GitHub, and this account has no
# access to the original
if ($env:PROPSTRUCT_LEGACY_DIR) {
    $failures.Add("PROPSTRUCT_LEGACY_DIR is set ('$($env:PROPSTRUCT_LEGACY_DIR)'); no Legacy fact runs on GitHub and this runner must not reach the original")
}
else {
    Write-Host 'PROPSTRUCT_LEGACY_DIR: unset'
}

# No runner of another repository: a personal account registers one runner per repository,
# and two must not use the GPU at once. This runner's own directory is the parent of _work. A
# listener that is an ancestor of this very process is the runner running this job, wherever
# its files are installed (a hosted image runs it from its own directory, outside RUNNER_TEMP's
# grandparent), so it is the runner's own whatever its path; any other listener outside the
# runner's directory is foreign.
if (-not $env:RUNNER_TEMP) {
    $failures.Add('RUNNER_TEMP is not set: the preflight must run as a step of a job, which names this runner''s directory')
}
else {
    $ownRoot = (Split-Path (Split-Path $env:RUNNER_TEMP -Parent) -Parent).TrimEnd('\') + '\'
    $ancestors = New-Object System.Collections.Generic.HashSet[int]
    $cursor = [int]$PID
    while ($cursor -gt 0 -and $ancestors.Add($cursor)) {
        $process = Get-CimInstance Win32_Process -Filter "ProcessId = $cursor" -ErrorAction SilentlyContinue
        $cursor = if ($process) { [int]$process.ParentProcessId } else { 0 }
    }
    $foreign = @(Get-CimInstance Win32_Process -Filter "Name = 'Runner.Listener.exe'" |
        Where-Object { -not $ancestors.Contains([int]$_.ProcessId) } |
        Where-Object { -not ($_.ExecutablePath) -or (-not $_.ExecutablePath.StartsWith($ownRoot, [System.StringComparison]::OrdinalIgnoreCase)) })
    if ($foreign.Count -gt 0) {
        foreach ($listener in $foreign) {
            $failures.Add("another runner is up: process $($listener.ProcessId), $($listener.ExecutablePath); this runner is $ownRoot")
        }
    }
    else {
        Write-Host "Runner.Listener: only this runner's own ($ownRoot, or an ancestor of this process)"
    }
}

# No compute process of this tree's or APThermo's kind on the GPU
if ($smiRuns) {
    $apps = @(& nvidia-smi --query-compute-apps=pid,process_name,used_memory --format=csv,noheader 2>&1 |
        Where-Object { $_ -and ($gpuHostNames -contains ((($_ -split ',')[1]).Trim().Split('\')[-1].ToLowerInvariant())) })
    if ($apps.Count -gt 0) {
        foreach ($app in $apps) {
            $failures.Add("a compute process holds the GPU: $app")
        }
    }
    else {
        Write-Host 'GPU: no dotnet, testhost, python or propstruct process among the compute applications'
    }
}

if ($failures.Count -gt 0) {
    Write-Host '::error::preflight failed:'
    foreach ($failure in $failures) {
        Write-Host "::error::- $failure"
    }
    exit 1
}

Write-Host 'preflight: all checks passed'
