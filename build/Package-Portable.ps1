[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$PublishDirectory,
    [Parameter(Mandatory)] [string]$ArtifactDirectory,
    [Parameter(Mandatory)] [string]$Version
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must be numeric MAJOR.MINOR.PATCH."
}

$publish = (Resolve-Path -LiteralPath $PublishDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $publish "YFToolbox.App.exe") -PathType Leaf)) {
    throw "The self-contained application executable is missing."
}

$artifacts = [IO.Path]::GetFullPath($ArtifactDirectory)
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$layoutRoot = Join-Path $artifacts ".portable-layout"
$packageName = "YFToolbox-$Version-win-x64"
$packageRoot = Join-Path $layoutRoot $packageName
$zipPath = Join-Path $artifacts "$packageName.zip"

if ([IO.Directory]::GetParent($layoutRoot).FullName -ne $artifacts) {
    throw "The portable layout escaped the artifact directory."
}

if (Test-Path -LiteralPath $layoutRoot) {
    Remove-Item -LiteralPath $layoutRoot -Recurse -Force
}
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
Copy-Item -Path (Join-Path $publish "*") -Destination $packageRoot -Recurse -Force
Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Filter "*.pdb" | Remove-Item -Force

@"
YF Toolbox $Version

This is the self-contained Windows 11 x64 build. No .NET runtime, installer,
cloud account, subscription, license key, or administrator permission is
required. Extract the complete folder and start YFToolbox.App.exe.
"@ | Set-Content -LiteralPath (Join-Path $packageRoot "START-HERE.txt") -Encoding utf8NoBOM

Compress-Archive -LiteralPath $packageRoot -DestinationPath $zipPath -CompressionLevel Optimal
Remove-Item -LiteralPath $layoutRoot -Recurse -Force

if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf) -or (Get-Item -LiteralPath $zipPath).Length -eq 0) {
    throw "The portable archive was not created."
}

Write-Output $zipPath
