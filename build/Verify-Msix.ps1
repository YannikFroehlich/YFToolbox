[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$MsixPath,
    [Parameter(Mandatory)] [string]$ExpectedIdentity,
    [Parameter(Mandatory)] [string]$ExpectedPublisher,
    [Parameter(Mandatory)] [string]$ExpectedVersion
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$kitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
$signTool = Get-ChildItem -Path $kitsRoot -Filter "signtool.exe" -Recurse |
    Where-Object { $_.DirectoryName -match "\\x64$" } |
    Sort-Object FullName -Descending |
    Select-Object -First 1
$makeAppx = Get-ChildItem -Path $kitsRoot -Filter "MakeAppx.exe" -Recurse |
    Where-Object { $_.DirectoryName -match "\\x64$" } |
    Sort-Object FullName -Descending |
    Select-Object -First 1
if (-not $signTool -or -not $makeAppx) {
    throw "The Windows SDK signing and packaging tools were not found."
}

& $signTool.FullName verify /pa /v $MsixPath
if ($LASTEXITCODE -ne 0) { throw "The MSIX signature could not be verified." }

$unpackDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "YFToolbox-Msix-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $unpackDirectory | Out-Null
try {
    & $makeAppx.FullName unpack /p $MsixPath /d $unpackDirectory /o
    if ($LASTEXITCODE -ne 0) { throw "The MSIX package could not be unpacked." }

    [xml]$manifest = Get-Content -LiteralPath (Join-Path $unpackDirectory "AppxManifest.xml") -Raw
    $identity = $manifest.Package.Identity
    if ($identity.Name -ne $ExpectedIdentity) { throw "Unexpected package identity '$($identity.Name)'." }
    if ($identity.Publisher -ne $ExpectedPublisher) { throw "Unexpected package publisher '$($identity.Publisher)'." }
    if ($identity.Version -ne $ExpectedVersion) { throw "Unexpected package version '$($identity.Version)'." }
}
finally {
    if (Test-Path -LiteralPath $unpackDirectory) {
        Remove-Item -LiteralPath $unpackDirectory -Recurse -Force
    }
}

Write-Host "MSIX signature, identity, publisher and version are valid."
