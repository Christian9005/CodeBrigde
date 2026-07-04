param(
    [Parameter(Mandatory = $true)]
    [string] $CertificatePath,

    [Parameter()]
    [string] $VsixPath,

    [Parameter()]
    [string] $TimestampUrl = "http://timestamp.digicert.com",

    [Parameter()]
    [string] $OutputPath
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
if ([string]::IsNullOrWhiteSpace($VsixPath)) {
    $VsixPath = Join-Path $repoRoot "src\CodeBridge.VisualStudio\bin\Release-signed\net472\CodeBridge.VisualStudio.vsix"
}

$CertificatePath = [System.IO.Path]::GetFullPath($CertificatePath)
$VsixPath = [System.IO.Path]::GetFullPath($VsixPath)
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = [System.IO.Path]::Combine(
        [System.IO.Path]::GetDirectoryName($VsixPath),
        [System.IO.Path]::GetFileNameWithoutExtension($VsixPath) + ".signed.vsix")
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)

$projectPath = Join-Path $PSScriptRoot "CodeBridge.VisualStudio.Signing.csproj"
dotnet restore $projectPath | Out-Host

$toolRoot = Join-Path $env:USERPROFILE ".nuget\packages\microsoft.vssdk.vsixsigntool\17.10.34916.79"
$tool = Get-ChildItem -Path $toolRoot -Recurse -Filter "VsixSignTool.exe" | Select-Object -First 1
if (-not $tool) {
    throw "VsixSignTool.exe was not found under $toolRoot"
}

if (-not (Test-Path -LiteralPath $CertificatePath)) {
    throw "Certificate file not found: $CertificatePath"
}

if (-not (Test-Path -LiteralPath $VsixPath)) {
    throw "VSIX file not found: $VsixPath"
}

$securePassword = Read-Host "P12 password" -AsSecureString
$passwordPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$signingCertificatePath = $CertificatePath
$temporaryPfxPath = $null
try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPtr)

    $certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(
        $CertificatePath,
        $password,
        [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)

    Write-Host "Certificate subject: $($certificate.Subject)"
    Write-Host "Certificate thumbprint: $($certificate.Thumbprint)"
    Write-Host "Certificate expires: $($certificate.NotAfter)"

    if (-not $certificate.HasPrivateKey) {
        throw "The P12/PFX certificate does not contain a private key."
    }

    $codeSigningOid = "1.3.6.1.5.5.7.3.3"
    $ekuExtension = $certificate.Extensions |
        Where-Object { $_ -is [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension] } |
        Select-Object -First 1

    if ($ekuExtension) {
        $ekuOids = @($ekuExtension.EnhancedKeyUsages | ForEach-Object { $_.Value })
        if ($ekuOids -notcontains $codeSigningOid) {
            $ekuList = ($ekuExtension.EnhancedKeyUsages | ForEach-Object { "$($_.FriendlyName) [$($_.Value)]" }) -join ", "
            throw "The certificate is not valid for Code Signing. EKU found: $ekuList"
        }
    }
    else {
        Write-Warning "The certificate has no Enhanced Key Usage extension. VsixSignTool may reject it if Code Signing usage is required."
    }

    if ([System.IO.Path]::GetExtension($CertificatePath).Equals(".p12", [System.StringComparison]::OrdinalIgnoreCase)) {
        $temporaryPfxPath = Join-Path ([System.IO.Path]::GetTempPath()) ("CodeBridge-" + [System.Guid]::NewGuid().ToString("N") + ".pfx")
        $pfxBytes = $certificate.Export(
            [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx,
            $password)
        [System.IO.File]::WriteAllBytes($temporaryPfxPath, $pfxBytes)
        $signingCertificatePath = $temporaryPfxPath
        Write-Host "Converted P12 certificate to temporary PFX for VsixSignTool."
    }

    Copy-Item -LiteralPath $VsixPath -Destination $OutputPath -Force

    & $tool.FullName sign `
        /v `
        /f $signingCertificatePath `
        /p $password `
        /fd sha256 `
        /t $TimestampUrl `
        $OutputPath

    if ($LASTEXITCODE -ne 0) {
        throw "VsixSignTool failed with exit code $LASTEXITCODE"
    }

    & $tool.FullName verify /v $OutputPath
    if ($LASTEXITCODE -ne 0) {
        throw "VsixSignTool verification failed with exit code $LASTEXITCODE"
    }
}
finally {
    if ($certificate) {
        $certificate.Dispose()
    }

    if ($temporaryPfxPath -and (Test-Path -LiteralPath $temporaryPfxPath)) {
        Remove-Item -LiteralPath $temporaryPfxPath -Force
    }

    if ($passwordPtr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPtr)
    }
}

Write-Host "Signed VSIX: $OutputPath"
