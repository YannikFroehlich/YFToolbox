[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$ReportPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
$findings = @()
foreach ($project in @($report.projects)) {
    if ($project.PSObject.Properties.Name -notcontains "frameworks") {
        continue
    }

    foreach ($framework in @($project.frameworks)) {
        $topLevel = if ($framework.PSObject.Properties.Name -contains "topLevelPackages") {
            @($framework.topLevelPackages)
        } else { @() }
        $transitive = if ($framework.PSObject.Properties.Name -contains "transitivePackages") {
            @($framework.transitivePackages)
        } else { @() }
        foreach ($package in $topLevel + $transitive) {
            if ($package.PSObject.Properties.Name -notcontains "vulnerabilities") {
                continue
            }

            foreach ($vulnerability in @($package.vulnerabilities)) {
                if ($vulnerability) {
                    $findings += [pscustomobject]@{
                        Project = $project.path
                        Framework = $framework.framework
                        Package = $package.id
                        Version = $package.resolvedVersion
                        Severity = $vulnerability.severity
                        Advisory = $vulnerability.advisoryurl
                    }
                }
            }
        }
    }
}

if ($findings.Count -gt 0) {
    $findings | Format-Table -AutoSize | Out-String | Write-Error
    throw "$($findings.Count) vulnerable dependency finding(s) detected."
}

Write-Host "No vulnerable NuGet dependencies were reported."
