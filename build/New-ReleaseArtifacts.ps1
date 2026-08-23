[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$ArtifactDirectory,
    [Parameter(Mandatory)] [string]$Version,
    [Parameter(Mandatory)] [string]$Repository,
    [Parameter(Mandatory)] [string]$SourceSha,
    [Parameter(Mandatory)] [string]$DependencyJson,
    [Parameter(Mandatory)] [string]$BuildTimeUtc,
    [Parameter(Mandatory)] [string]$DotNetVersion
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$channel = if ([version]$Version -lt [version]"1.0.0") { "Preview" } else { "Stable" }

$dependencyData = Get-Content -LiteralPath $DependencyJson -Raw | ConvertFrom-Json
$packageIndex = [ordered]@{}
foreach ($project in $dependencyData.projects) {
    foreach ($framework in $project.frameworks) {
        $topLevel = if ($framework.PSObject.Properties.Name -contains "topLevelPackages") {
            @($framework.topLevelPackages)
        } else { @() }
        $transitive = if ($framework.PSObject.Properties.Name -contains "transitivePackages") {
            @($framework.transitivePackages)
        } else { @() }
        foreach ($package in $topLevel + $transitive) {
            if ($package) {
                $key = "$($package.id.ToLowerInvariant())|$($package.resolvedVersion)"
                $packageIndex[$key] = [pscustomobject][ordered]@{
                    SPDXID = "SPDXRef-Package-$($package.id -replace '[^A-Za-z0-9.-]', '-')-$($package.resolvedVersion)"
                    name = $package.id
                    versionInfo = $package.resolvedVersion
                    downloadLocation = "https://www.nuget.org/packages/$($package.id)/$($package.resolvedVersion)"
                    filesAnalyzed = $false
                    licenseConcluded = "NOASSERTION"
                    licenseDeclared = "NOASSERTION"
                    copyrightText = "NOASSERTION"
                }
            }
        }
    }
}
$packages = @($packageIndex.Values | Sort-Object name, versionInfo)
$sbom = [ordered]@{
    spdxVersion = "SPDX-2.3"
    dataLicense = "CC0-1.0"
    SPDXID = "SPDXRef-DOCUMENT"
    name = "YFToolbox-$Version"
    documentNamespace = "https://github.com/$Repository/releases/tag/v$Version/spdx/$SourceSha"
    creationInfo = [ordered]@{
        created = $BuildTimeUtc
        creators = @("Tool: YFToolbox release pipeline")
    }
    packages = @($packages)
    relationships = @($packages | ForEach-Object {
        [ordered]@{
            spdxElementId = "SPDXRef-DOCUMENT"
            relationshipType = "DESCRIBES"
            relatedSpdxElement = $_.SPDXID
        }
    })
}
$sbomPath = Join-Path $ArtifactDirectory "YFToolbox-$Version-sbom.spdx.json"
$sbom | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $sbomPath -Encoding utf8NoBOM

$noticeSource = Join-Path (Split-Path $PSScriptRoot -Parent) "THIRD-PARTY-NOTICES.md"
Copy-Item -LiteralPath $noticeSource -Destination (Join-Path $ArtifactDirectory "THIRD-PARTY-NOTICES.md") -Force

$primaryFiles = Get-ChildItem -LiteralPath $ArtifactDirectory -File |
    Where-Object { $_.Name -notin @("release-manifest.json", "YFToolbox-$Version-checksums.txt") }
$hashes = [ordered]@{}
foreach ($file in $primaryFiles) {
    $hashes[$file.Name] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}

$manifest = [ordered]@{
    schemaVersion = 1
    semanticVersion = $Version
    repository = $Repository
    commitSha = $SourceSha
    buildTimeUtc = $BuildTimeUtc
    releaseChannel = $channel
    dotnetVersion = $DotNetVersion
    distribution = "portable-win-x64"
    runtimeIdentifier = "win-x64"
    selfContained = $true
    artifacts = $hashes
}
$manifestPath = Join-Path $ArtifactDirectory "release-manifest.json"
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM

$checksumPath = Join-Path $ArtifactDirectory "YFToolbox-$Version-checksums.txt"
Get-ChildItem -LiteralPath $ArtifactDirectory -File |
    Where-Object Name -ne (Split-Path $checksumPath -Leaf) |
    Sort-Object Name |
    ForEach-Object {
        "{0}  {1}" -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
    } | Set-Content -LiteralPath $checksumPath -Encoding ascii
