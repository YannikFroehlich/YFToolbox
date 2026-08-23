[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$MsixPath,
    [string]$Publisher = "CN=YF Toolbox Development",
    [string]$CertificatePath = "artifacts/YFToolbox-Development.cer"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$resolvedMsix = (Resolve-Path -LiteralPath $MsixPath).Path
$certificate = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Publisher `
    -KeyAlgorithm RSA `
    -KeyLength 3072 `
    -HashAlgorithm SHA256 `
    -KeyExportPolicy NonExportable `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter ([DateTime]::UtcNow.AddYears(1))

try {
    $certificateDirectory = Split-Path $CertificatePath -Parent
    if ($certificateDirectory) {
        New-Item -ItemType Directory -Path $certificateDirectory -Force | Out-Null
    }
    Export-Certificate -Cert $certificate -FilePath $CertificatePath -Force | Out-Null

    $kitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    $signTool = Get-ChildItem -Path $kitsRoot -Filter "signtool.exe" -Recurse |
        Where-Object { $_.DirectoryName -match "\\x64$" } |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if (-not $signTool) { throw "signtool.exe was not found in the Windows SDK." }

    & $signTool.FullName sign /sha1 $certificate.Thumbprint /fd SHA256 /tr http://timestamp.acs.microsoft.com /td SHA256 $resolvedMsix
    if ($LASTEXITCODE -ne 0) { throw "Development MSIX signing failed." }
}
finally {
    Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -Force
}

Write-Host "The development MSIX is signed. Trust '$CertificatePath' locally before installation."
