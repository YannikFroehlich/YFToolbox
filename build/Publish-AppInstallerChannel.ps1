[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Repository,
    [Parameter(Mandatory)] [string]$SourceSha,
    [Parameter(Mandatory)] [string]$ChannelFile,
    [Parameter(Mandatory)] [string]$TargetPath,
    [Parameter(Mandatory)] [string]$CommitMessage
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$branch = "gh-pages"
$normalizedTarget = $TargetPath.Replace("\", "/").TrimStart("/")
if ($normalizedTarget -notmatch "^channels/[A-Za-z0-9._-]+\.appinstaller$") {
    throw "The channel target must be a safe appinstaller path below channels/."
}
if (-not (Test-Path -LiteralPath $ChannelFile -PathType Leaf)) {
    throw "The AppInstaller channel file does not exist."
}

& gh api "repos/$Repository/git/ref/heads/$branch" --silent 2>$null
if ($LASTEXITCODE -ne 0) {
    & gh api "repos/$Repository/git/refs" -X POST -f "ref=refs/heads/$branch" -f "sha=$SourceSha" --silent
    if ($LASTEXITCODE -ne 0) { throw "Could not create the gh-pages branch." }
}

$content = [Convert]::ToBase64String([IO.File]::ReadAllBytes($ChannelFile))
$target = "repos/$Repository/contents/$normalizedTarget"
$existingSha = (& gh api "${target}?ref=$branch" --jq ".sha" 2>$null)
$arguments = @(
    "api", $target, "-X", "PUT",
    "-f", "message=$CommitMessage",
    "-f", "content=$content",
    "-f", "branch=$branch"
)
if ($existingSha) {
    $arguments += @("-f", "sha=$existingSha")
}

& gh @arguments --silent
if ($LASTEXITCODE -ne 0) { throw "The AppInstaller channel could not be updated." }
