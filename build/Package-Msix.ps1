[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$PublishDirectory,
    [Parameter(Mandatory)] [string]$ArtifactDirectory,
    [Parameter(Mandatory)] [string]$Version,
    [Parameter(Mandatory)] [string]$Repository,
    [Parameter(Mandatory)] [string]$PackageIdentity,
    [Parameter(Mandatory)] [string]$Publisher,
    [Parameter(Mandatory)] [string]$ChannelBaseUri
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($Version -notmatch "^\d+\.\d+\.\d+$") {
    throw "Version must be a three-part numeric semantic version."
}
$versionParts = $Version.Split('.') | ForEach-Object { [int]$_ }
if ($versionParts | Where-Object { $_ -gt 65535 }) {
    throw "Each MSIX version component must be between 0 and 65535."
}
if ($PackageIdentity -notmatch "^[A-Za-z0-9.-]{3,50}$") {
    throw "The package identity is not valid for MSIX."
}
if ([string]::IsNullOrWhiteSpace($Publisher)) {
    throw "The package publisher is required."
}
if ($Repository -notmatch "^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$") {
    throw "Repository must use the owner/name form."
}
$channelOrigin = [uri]$ChannelBaseUri
if (-not $channelOrigin.IsAbsoluteUri -or $channelOrigin.Scheme -ne "https") {
    throw "The AppInstaller channel requires an absolute HTTPS URI."
}

$msixVersion = "$Version.0"
$identityXml = [Security.SecurityElement]::Escape($PackageIdentity)
$publisherXml = [Security.SecurityElement]::Escape($Publisher)
$layout = Join-Path $ArtifactDirectory "msix-layout"
if (Test-Path -LiteralPath $layout) {
    Remove-Item -LiteralPath $layout -Recurse -Force
}

New-Item -ItemType Directory -Path $layout -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $layout "Assets") -Force | Out-Null
New-Item -ItemType Directory -Path $ArtifactDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $PublishDirectory "*") -Destination $layout -Recurse -Force
Get-ChildItem -LiteralPath $layout -Filter "*.pdb" -File -Recurse | Remove-Item -Force

Add-Type -AssemblyName PresentationCore
function New-LogoPng {
    param([string]$Path, [int]$Width, [int]$Height)

    $stride = $Width * 4
    $pixels = [byte[]]::new($stride * $Height)
    for ($index = 0; $index -lt $pixels.Length; $index += 4) {
        $pixels[$index] = 0xB5
        $pixels[$index + 1] = 0x71
        $pixels[$index + 2] = 0x24
        $pixels[$index + 3] = 0xFF
    }

    $bitmap = [System.Windows.Media.Imaging.WriteableBitmap]::new(
        $Width,
        $Height,
        96,
        96,
        [System.Windows.Media.PixelFormats]::Bgra32,
        $null)
    $bitmap.WritePixels([System.Windows.Int32Rect]::new(0, 0, $Width, $Height), $pixels, $stride, 0)
    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [System.IO.File]::Create($Path)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}

$assets = Join-Path $layout "Assets"
New-LogoPng (Join-Path $assets "Square44x44Logo.png") 44 44
New-LogoPng (Join-Path $assets "Square150x150Logo.png") 150 150
New-LogoPng (Join-Path $assets "Wide310x150Logo.png") 310 150
New-LogoPng (Join-Path $assets "StoreLogo.png") 50 50
New-LogoPng (Join-Path $assets "SplashScreen.png") 620 300

$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
         IgnorableNamespaces="uap rescap">
  <Identity Name="$identityXml" Publisher="$publisherXml" Version="$msixVersion" ProcessorArchitecture="x64" />
  <Properties>
    <DisplayName>YF Toolbox</DisplayName>
    <PublisherDisplayName>YF Toolbox</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>Local-first file conversion and utility tools.</Description>
  </Properties>
  <Resources>
    <Resource Language="en-us" />
    <Resource Language="de-de" />
  </Resources>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.22000.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>
  <Applications>
    <Application Id="App" Executable="YFToolbox.App.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="YF Toolbox" Description="YF Toolbox"
          BackgroundColor="transparent" Square150x150Logo="Assets\Square150x150Logo.png"
          Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" ShortName="YF Toolbox" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@
[System.IO.File]::WriteAllText((Join-Path $layout "AppxManifest.xml"), $manifest, [System.Text.UTF8Encoding]::new($false))

$kitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
$makeAppx = Get-ChildItem -Path $kitsRoot -Filter "MakeAppx.exe" -Recurse |
    Where-Object { $_.DirectoryName -match "\\x64$" } |
    Sort-Object FullName -Descending |
    Select-Object -First 1
if (-not $makeAppx) {
    throw "MakeAppx.exe was not found in the Windows SDK."
}

$msixName = "YFToolbox-$Version-x64.msix"
$msixPath = Join-Path $ArtifactDirectory $msixName
& $makeAppx.FullName pack /d $layout /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed with exit code $LASTEXITCODE." }

$major = [version]$Version
$isPreview = $major.Major -eq 0
$channelName = if ($isPreview) { "YFToolbox.Preview.appinstaller" } else { "YFToolbox.appinstaller" }
$channelUri = "$($ChannelBaseUri.TrimEnd('/'))/channels/$channelName"
$packageUri = "https://github.com/$Repository/releases/download/v$Version/$msixName"
$appInstaller = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller xmlns="http://schemas.microsoft.com/appx/appinstaller/2018" Version="$msixVersion" Uri="$channelUri">
  <MainPackage Name="$identityXml" Publisher="$publisherXml" Version="$msixVersion" ProcessorArchitecture="x64" Uri="$packageUri" />
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="8" ShowPrompt="true" UpdateBlocksActivation="false" />
    <AutomaticBackgroundTask />
    <ForceUpdateFromAnyVersion>false</ForceUpdateFromAnyVersion>
  </UpdateSettings>
</AppInstaller>
"@
$channelPath = Join-Path $ArtifactDirectory $channelName
[System.IO.File]::WriteAllText($channelPath, $appInstaller, [System.Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath $channelPath -Destination (Join-Path $ArtifactDirectory "YFToolbox.appinstaller") -Force

[pscustomobject]@{
    MsixPath = $msixPath
    MsixVersion = $msixVersion
    Channel = if ($isPreview) { "Preview" } else { "Stable" }
    ChannelFile = $channelName
} | ConvertTo-Json -Compress
