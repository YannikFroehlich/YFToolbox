[CmdletBinding()]
param(
    [ValidateSet("", "patch", "minor", "major")]
    [string]$ForceBump = "",
    [string]$RepositoryPath = "."
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Push-Location $RepositoryPath
try {
    $head = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $head -notmatch "^[0-9a-f]{40}$") {
        throw "The repository HEAD could not be resolved."
    }

    $tagsAtHead = @(
        (git tag --points-at HEAD --list "v[0-9]*.[0-9]*.[0-9]*") |
            Where-Object { $_ -match "^v\d+\.\d+\.\d+$" } |
            Sort-Object { [version]($_.Substring(1)) } -Descending
    )
    if ($tagsAtHead.Count -gt 0) {
        [pscustomobject]@{
            Version = $tagsAtHead[0].Substring(1)
            SourceSha = $head
            PreviousTag = $tagsAtHead[0]
            Bump = "existing"
            ExistingTag = $true
        } | ConvertTo-Json -Compress
        return
    }

    $lastTag = @(git tag --list "v[0-9]*.[0-9]*.[0-9]*" --sort=-v:refname) |
        Where-Object { $_ -match "^v\d+\.\d+\.\d+$" } |
        Select-Object -First 1

    if (-not $lastTag) {
        [pscustomobject]@{
            Version = if ($ForceBump -eq "major") { "1.0.0" } else { "0.1.0" }
            SourceSha = $head
            PreviousTag = $null
            Bump = if ($ForceBump) { $ForceBump } else { "initial" }
            ExistingTag = $false
        } | ConvertTo-Json -Compress
        return
    }

    $range = "$lastTag..HEAD"
    $messages = (git log $range --format="%B%x1e") -join "`n"
    if ($LASTEXITCODE -ne 0) {
        throw "Commit messages could not be read."
    }

    $bump = $ForceBump
    if (-not $bump) {
        if ($messages -match "(?mi)^BREAKING CHANGE:\s" -or $messages -match "(?mi)^[a-z]+(?:\([^)]+\))?!:") {
            $bump = "major"
        }
        elseif ($messages -match "(?mi)^feat(?:\([^)]+\))?:") {
            $bump = "minor"
        }
        else {
            # YFRemote compatibility: fixes and all non-conventional changes default to patch.
            $bump = "patch"
        }
    }

    $current = [version]$lastTag.Substring(1)
    $next = switch ($bump) {
        "major" { [version]::new($current.Major + 1, 0, 0) }
        "minor" { [version]::new($current.Major, $current.Minor + 1, 0) }
        default { [version]::new($current.Major, $current.Minor, $current.Build + 1) }
    }

    [pscustomobject]@{
        Version = "$($next.Major).$($next.Minor).$($next.Build)"
        SourceSha = $head
        PreviousTag = $lastTag
        Bump = $bump
        ExistingTag = $false
    } | ConvertTo-Json -Compress
}
finally {
    Pop-Location
}
