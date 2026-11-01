[CmdletBinding()]
param(
    [ValidateSet(1)][int]$Gate = 1,
    [switch]$Offline
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $Offline) { throw 'Gate 1 demo requires -Offline and synthetic fixtures.' }

function Invoke-Checked {
    param([string]$File, [string[]]$Arguments)
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File failed (exit $LASTEXITCODE)." }
}

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$python = Join-Path $repoRoot '.venv-contracts/Scripts/python.exe'
if (-not (Test-Path -LiteralPath $python)) { $python = Join-Path $repoRoot '.venv-contracts/bin/python' }
if (-not (Test-Path -LiteralPath $python)) { throw 'Prepare .venv-contracts first; see tools/README.md.' }
$null = Get-Command dotnet -ErrorAction Stop
$null = Get-Command docker -ErrorAction Stop

Push-Location $repoRoot
try {
    Write-Output "Gate $Gate synthetic demo: no model/weather/private-document integration."
    Write-Output 'Prerequisite: restored packages and cached container images. Offline does not isolate the network.'
    Invoke-Checked docker @('info', '--format', '{{.OSType}}')
    Invoke-Checked dotnet @('build', 'wAInery.slnx', '--configuration', 'Release', '--no-restore', '--verbosity', 'quiet')
    Invoke-Checked dotnet @('run', '--project', 'tests/Architecture/wAInery.ArchitectureChecks.csproj', '--configuration', 'Release', '--no-build', '--no-restore')
    Invoke-Checked $python @('tools/validate-gate3-contracts.py')
    Invoke-Checked $python @('scripts/validate_agent_skills.py')
    Invoke-Checked dotnet @('test', '--solution', 'wAInery.slnx', '--configuration', 'Release', '--no-build', '--no-restore', '--verbosity', 'quiet')
    Write-Output 'PASS Gate 1: actual synthetic contract, SQL, migration CLI, outbox, lease and Host scenarios.'
    Write-Output 'Human Gate 1 review remains required; this command does not approve the next milestone.'
}
finally { Pop-Location }
