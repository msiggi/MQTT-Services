<#
.SYNOPSIS
    Runs the package smoke test, optionally against a version other than the one
    pinned in smoketest.cs.

.DESCRIPTION
    A file-based app resolves its #:package directive at build time, so the version
    cannot be passed in at runtime. This copies the source to a temporary file with
    the directive rewritten and runs that instead, which leaves smoketest.cs alone.

.EXAMPLE
    ./run.ps1
    ./run.ps1 -Version 4.0.0-beta.2
#>
[CmdletBinding()]
param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'smoketest.cs'

if (-not $Version) {
    dotnet run $source
    exit $LASTEXITCODE
}

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("mqtt-smoketest-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    $target = Join-Path $work 'smoketest.cs'
    (Get-Content $source -Raw) -replace '(?m)^#:package MQTT-Services@.*$', "#:package MQTT-Services@$Version" |
        Set-Content $target -Encoding utf8 -NoNewline

    Write-Host "Running the smoke test against MQTT-Services $Version" -ForegroundColor Cyan
    dotnet run $target
    exit $LASTEXITCODE
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
